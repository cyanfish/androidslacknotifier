using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Gms.Common;
using Android.Graphics;
using Android.Widget;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Java.Lang;
using Com.Lilarcor.Cheeseknife;
#pragma warning disable 649

namespace Lichess4545SlackNotifier
{
    [Activity(Label = "Lichess4545 Slack Notifier", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private const int LOGIN_REQUEST = 1;

        [InjectView(Resource.Id.LoginButton)]
        private Button loginButton;

        [InjectView(Resource.Id.LogoutButton)]
        private Button logoutButton;

        [InjectView(Resource.Id.status)]
        private TextView status;

        [InjectView(Resource.Id.IntervalSpinner)]
        private Spinner intervalSpinner;

        [InjectView(Resource.Id.PollContainer)]
        private LinearLayout pollContainer;

        [InjectView(Resource.Id.listView1)]
        private ListView messageList;

        [InjectView(Resource.Id.progressBar1)]
        private ProgressBar progressBar;
        
        private Prefs prefs;
        private AlarmSetter alarmSetter;
        private CloudMessaging cloudMessaging;
        private MessageListAdapter adapter;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main2);

            Cheeseknife.Inject(this);

            prefs = new Prefs(this);
            alarmSetter = new AlarmSetter(this);
            cloudMessaging = new CloudMessaging(this);

            if (Intent.HasExtra("ts"))
            {
                prefs.LastDismissedTs = Math.Max(prefs.LastDismissedTs, Intent.GetLongExtra("ts", -1));
            }

            loginButton.Click += (sender, args) =>
            {
                StartActivityForResult(new Intent(this, typeof(SlackLoginActivity)), LOGIN_REQUEST);
            };

            logoutButton.Click += (sender, args) =>
            {
                cloudMessaging.Unregister();
                prefs.Auth = null;
                prefs.LatestUnreads = null;
                prefs.LatestUserMap = null;
                RefreshDisplay(true);
            };

            string[] intervalChoices = { "Disabled", "Every 10 minutes", "Every 20 minutes", "Every 30 minutes", "Every hour", "Every 2 hours" };
            var intervalValues = new List<long> { 0L, 10 * TimeConstants.Minute, 20 * TimeConstants.Minute, 30 * TimeConstants.Minute, TimeConstants.Hour, 2 * TimeConstants.Hour };
            intervalSpinner.Adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, intervalChoices);
            long interval = prefs.Interval;
            intervalSpinner.SetSelection(intervalValues.IndexOf(interval));
            intervalSpinner.ItemSelected += (sender, args) =>
            {
                long newValue = intervalValues[args.Position];
                if (newValue != prefs.Interval)
                {
                    prefs.Interval = newValue;
                    alarmSetter.SetAlarm();
                }
            };

            messageList.ItemClick += (sender, args) => StartActivity(((MessageListAdapter)messageList.Adapter).UnreadChannels[args.Position].GetIntent());
            messageList.Adapter = adapter = new MessageListAdapter(this, prefs.LatestUnreads, prefs.LatestUserMap);

            IsPlayServicesAvailable();
        }
        
        public bool IsPlayServicesAvailable()
        {
            int resultCode = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this);
            if (resultCode != ConnectionResult.Success)
            {
                if (GoogleApiAvailability.Instance.IsUserResolvableError(resultCode))
                    Toast.MakeText(this, GoogleApiAvailability.Instance.GetErrorString(resultCode), ToastLength.Long).Show();
                return false;
            }
            return true;
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            adapter.UnreadChannels = prefs.LatestUnreads;
            adapter.UserMap = prefs.LatestUserMap;
            adapter.NotifyDataSetChanged();

            if (intent.HasExtra("ts"))
            {
                prefs.LastDismissedTs = Math.Max(prefs.LastDismissedTs, intent.GetLongExtra("ts", -1));
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            RefreshDisplay(true);
            PollForMessages();
            alarmSetter.SetAlarm();
        }

        public async void PollForMessages()
        {
            string token = prefs.Token;
            if (token == null)
            {
                return;
            }
            try
            {
                var readAuth = SlackUtils.TestAuth(token);
                var readRtm = SlackUtils.RtmStart(token);
                var readUserMap = SlackUtils.BuildUserMap(token);
                prefs.Auth = await readAuth;
                if (prefs.Auth.Ok)
                {
                    var response = await readRtm;
                    var userMap = await readUserMap;
                    prefs.LatestUserMap = userMap;
                    var unreadChannels = await SlackUtils.GetUnreadChannels(response, token, userMap, prefs.Auth.User, prefs.Subscriptions);
                    prefs.LatestUnreads = unreadChannels;
                    adapter.UnreadChannels = unreadChannels;
                    adapter.UserMap = userMap;
                    adapter.NotifyDataSetChanged();
                    progressBar.Visibility = ViewStates.Gone;
                    Notifications.Update(userMap, this, unreadChannels, prefs.LastDismissedTs);
                }
            }
            catch (Exception e)
            {
                Log.Error("slackn", e, "Error polling");
            }
            catch (System.Exception e)
            {
                Log.Error("slackn", "Error polling: " + e);
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode == LOGIN_REQUEST)
            {
                if (resultCode == Result.Ok)
                {
                    RefreshDisplay(true);
                    alarmSetter.SetAlarm();
                    cloudMessaging.Register();
                }
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu1, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.ActionRefresh:
                    if (adapter != null)
                    {
                        adapter.UnreadChannels.Clear();
                        adapter.NotifyDataSetChanged();
                    }
                    progressBar.Visibility = ViewStates.Visible;
                    PollForMessages();
                    return true;
                case Resource.Id.ActionSubscriptions:
                    new SubscriptionsDialogFragment().Show(FragmentManager, null);
                    return true;
                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        private void RefreshDisplay(bool connected)
        {
            if (!connected)
            {
                loginButton.Visibility = ViewStates.Visible;
                logoutButton.Visibility = ViewStates.Gone;
                status.Visibility = ViewStates.Visible;
                pollContainer.Visibility = ViewStates.Gone;
                progressBar.Visibility = ViewStates.Gone;
                messageList.Visibility = ViewStates.Gone;
                status.Text = "Couldn't connect to slack";
                return;
            }

            string user = prefs.Auth.User;
            if (user != null)
            {
                // Logged in
                loginButton.Visibility = ViewStates.Gone;
                logoutButton.Visibility = ViewStates.Visible;
                status.Visibility = ViewStates.Visible;
                pollContainer.Visibility = ViewStates.Visible;
                progressBar.Visibility = messageList.Adapter?.Count > 0 ? ViewStates.Gone : ViewStates.Visible;
                messageList.Visibility = ViewStates.Visible;
                status.Text = $"Logged in as {user}";
            }
            else
            {
                // Logged out
                loginButton.Visibility = ViewStates.Visible;
                logoutButton.Visibility = ViewStates.Gone;
                status.Visibility = ViewStates.Gone;
                pollContainer.Visibility = ViewStates.Gone;
                progressBar.Visibility = ViewStates.Gone;
                messageList.Visibility = ViewStates.Gone;
            }
        }

        private class MessageListAdapter : BaseAdapter<UnreadChannel>
        {
            private readonly Color textColor = new Color(64, 64, 64);

            private readonly Context context;

            public MessageListAdapter(Context context, IEnumerable<UnreadChannel> unreadChannels, Dictionary<string, string> userMap)
            {
                this.context = context;
                UnreadChannels = unreadChannels.ToList();
                UserMap = userMap;
            }

            public List<UnreadChannel> UnreadChannels { get; set; }

            public Dictionary<string, string> UserMap { get; set; }

            public override UnreadChannel this[int position] => UnreadChannels[position];

            public override long GetItemId(int position)
            {
                return UnreadChannels[position].ChannelName.GetHashCode();
            }

            public override View GetView(int position, View convertView, ViewGroup parent)
            {
                var v = convertView;
                if (v == null)
                {
                    var vi = (LayoutInflater)context.GetSystemService(LayoutInflaterService);
                    v = vi.Inflate(Android.Resource.Layout.TwoLineListItem, null);
                }

                var item = this[position];
                TextView text1 = (TextView)v.FindViewById(Android.Resource.Id.Text1);
                text1.SetText(item.ChannelName, TextView.BufferType.Normal);
                text1.SetTextColor(textColor);
                TextView text2 = (TextView)v.FindViewById(Android.Resource.Id.Text2);
                text2.SetText(item.Messages.First().DisplayText(UserMap) ?? "", TextView.BufferType.Normal);
                text2.SetTextColor(textColor);

                return v;
            }

            public override int Count => UnreadChannels.Count;
        }
    }
}
