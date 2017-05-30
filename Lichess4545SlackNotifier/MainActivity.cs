using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Widget;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Java.Lang;
using Lichess4545SlackNotifier.SlackApi;
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

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main2);

            Cheeseknife.Inject(this);

            prefs = new Prefs(this);
            alarmSetter = new AlarmSetter(this);

            loginButton.Click += (sender, args) =>
            {
                StartActivityForResult(new Intent(this, typeof(SlackLoginActivity)), LOGIN_REQUEST);
            };

            logoutButton.Click += (sender, args) =>
            {
                prefs.Auth = null;
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
        }

        protected override void OnResume()
        {
            base.OnResume();

            RefreshDisplay(true);
            TestAuth();
            alarmSetter.SetAlarm();
        }

        public async void TestAuth()
        {
            string token = prefs.Token;
            if (token == null)
            {
                return;
            }
            try
            {
                string url = $"https://slack.com/api/auth.test?token={token}";
                var readAuth = JsonReader.ReadJsonFromUrlAsync<AuthResponse>(url);
                string rtmUrl = $"https://slack.com/api/rtm.start?token={token}&mpim_aware=true";
                var readRtm = JsonReader.ReadJsonFromUrlAsync<RtmStartResponse>(rtmUrl);
                var readUserMap = SlackUtils.BuildUserMap(token);
                prefs.Auth = await readAuth;
                if (prefs.Auth.Ok)
                {
                    var response = await readRtm;
                    var userMap = await readUserMap;
                    var unreadChannels = (await SlackUtils.GetUnreadChannels(response, token, userMap, prefs.Auth.User, prefs.Subscriptions)).ToList();
                    var currentUser = prefs.Auth.User;
                    if (messageList.Adapter == null)
                    {
                        messageList.Adapter = new MessageListAdapter(this, unreadChannels, userMap, currentUser);
                    }
                    else
                    {
                        ((MessageListAdapter) messageList.Adapter).UnreadChannels = unreadChannels;
                        ((MessageListAdapter) messageList.Adapter).NotifyDataSetChanged();
                    }
                    progressBar.Visibility = ViewStates.Gone;
                    Notifications.Update(userMap, this, unreadChannels, prefs.LastDismissedTs);
                }
            }
            catch (Exception e)
            {
                Log.Error("slackn", e.ToString());
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
                    if (messageList.Adapter != null)
                    {
                        ((MessageListAdapter)messageList.Adapter).UnreadChannels.Clear();
                        ((MessageListAdapter)messageList.Adapter).NotifyDataSetChanged();
                    }
                    progressBar.Visibility = ViewStates.Visible;
                    TestAuth();
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
            }
        }

        private class MessageListAdapter : BaseAdapter<UnreadChannel>
        {
            private readonly Context context;
            private readonly Dictionary<string, string> userMap;
            private readonly string currentUserName;

            public MessageListAdapter(Context context, IEnumerable<UnreadChannel> unreadChannels, Dictionary<string, string> userMap, string currentUserName)
            {
                this.context = context;
                this.UnreadChannels = unreadChannels.ToList();
                this.userMap = userMap;
                this.currentUserName = currentUserName;
            }

            public List<UnreadChannel> UnreadChannels { get; set; }

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
                    LayoutInflater vi = (LayoutInflater)context.GetSystemService(LayoutInflaterService);
                    v = vi.Inflate(Android.Resource.Layout.TwoLineListItem, null);
                }

                var item = this[position];
                TextView text1 = (TextView)v.FindViewById(Android.Resource.Id.Text1);
                text1.SetText(item.ChannelName, TextView.BufferType.Normal);
                text1.SetTextColor(new Color(64, 64, 64));
                TextView text2 = (TextView)v.FindViewById(Android.Resource.Id.Text2);
                text2.SetText(item.Messages.First().DisplayText(userMap) ?? "", TextView.BufferType.Normal);
                text2.SetTextColor(new Color(64, 64, 64));

                return v;
            }

            public override int Count => UnreadChannels.Count;
        }
    }
}
