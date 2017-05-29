using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Widget;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Java.Lang;
using Lichess4545SlackNotifier.SlackApi;
using Ninject;
using Message = Lichess4545SlackNotifier.SlackApi.Message;

namespace Lichess4545SlackNotifier
{
    [Activity(Label = "Lichess4545SlackNotifier", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private const int LOGIN_REQUEST = 1;

        [InjectView(Resource.Id.LoginButton)]
        public Button LoginButton { get; set; }

        [InjectView(Resource.Id.LogoutButton)]
        public Button LogoutButton { get; set; }

        [InjectView(Resource.Id.status)]
        public TextView Status { get; set; }

        [InjectView(Resource.Id.IntervalSpinner)]
        public Spinner IntervalSpinner { get; set; }

        [InjectView(Resource.Id.PollContainer)]
        public LinearLayout PollContainer { get; set; }

        [InjectView(Resource.Id.listView1)]
        public ListView MessageList { get; set; }

        [InjectView(Resource.Id.progressBar1)]
        public ProgressBar ProgressBar { get; set; }

        [Inject]
        public Prefs Prefs { get; set; }

        [Inject]
        public AlarmSetter AlarmSetter { get; set; }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main2);

            KernelManager.Inject(this);

            LoginButton.Click += (sender, args) =>
            {
                StartActivityForResult(new Intent(this, typeof(SlackLoginActivity)), LOGIN_REQUEST);
            };

            LogoutButton.Click += (sender, args) =>
            {
                Prefs.Auth = null;
                RefreshDisplay(true);
            };

            string[] intervalChoices = { "Disabled", "Every minute", "Every 10 minutes", "Every 20 minutes", "Every 30 minutes", "Every hour", "Every 2 hours" };
            var intervalValues = new List<long> { 0L, TimeConstants.Minute, 10 * TimeConstants.Minute, 20 * TimeConstants.Minute, 30 * TimeConstants.Minute, TimeConstants.Hour, 2 * TimeConstants.Hour };
            IntervalSpinner.Adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, intervalChoices);
            long interval = Prefs.Interval;
            IntervalSpinner.SetSelection(intervalValues.IndexOf(interval));
            IntervalSpinner.ItemSelected += (sender, args) =>
            {
                long newValue = intervalValues[args.Position];
                if (newValue != Prefs.Interval)
                {
                    Prefs.Interval = newValue;
                    AlarmSetter.SetAlarm();
                }
            };

            MessageList.ItemClick += (sender, args) => StartActivity(((MessageListAdapter)MessageList.Adapter).UnreadChannels[args.Position].GetIntent());
        }

        protected override void OnResume()
        {
            base.OnResume();

            RefreshDisplay(true);
            TestAuth();
            AlarmSetter.SetAlarm();
        }

        private async void TestAuth()
        {
            string token = Prefs.Token;
            if (token == null)
            {
                return;
            }
            string url = $"https://slack.com/api/auth.test?token={token}";
            var readAuth = JsonReader.ReadJsonFromUrlAsync<AuthResponse>(url);
            string rtmUrl = $"https://slack.com/api/rtm.start?token={token}&mpim_aware=true";
            var readRtm = JsonReader.ReadJsonFromUrlAsync<RtmStartResponse>(rtmUrl);
            var readUserMap = SlackUtils.BuildUserMap(token);
            Prefs.Auth = await readAuth;
            if (Prefs.Auth.Ok)
            {
                var response = await readRtm;
                var userMap = await readUserMap;
                var unreadChannels = response.AllChannels().Where(x => !x.IsArchived && x.UnreadCountDisplay > -1 && (x.IsMpim || x.IsIm || x.Name == "general")).ToList();
                var currentUser = Prefs.Auth.User;
                if (MessageList.Adapter == null)
                {
                    MessageList.Adapter = new MessageListAdapter(this, unreadChannels, userMap, currentUser);
                }
                else
                {
                    ((MessageListAdapter)MessageList.Adapter).UnreadChannels = unreadChannels;
                    ((MessageListAdapter)MessageList.Adapter).NotifyDataSetChanged();
                }
                ProgressBar.Visibility = ViewStates.Gone;
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode == LOGIN_REQUEST)
            {
                if (resultCode == Result.Ok)
                {
                    RefreshDisplay(true);
                    AlarmSetter.SetAlarm();
                }
            }
        }

        private void RefreshDisplay(bool connected)
        {
            if (!connected)
            {
                LoginButton.Visibility = ViewStates.Visible;
                LogoutButton.Visibility = ViewStates.Gone;
                Status.Visibility = ViewStates.Visible;
                PollContainer.Visibility = ViewStates.Gone;
                ProgressBar.Visibility = ViewStates.Gone;
                Status.Text = "Couldn't connect to slack";
                return;
            }

            string user = Prefs.Auth.User;
            if (user != null)
            {
                // Logged in
                LoginButton.Visibility = ViewStates.Gone;
                LogoutButton.Visibility = ViewStates.Visible;
                Status.Visibility = ViewStates.Visible;
                PollContainer.Visibility = ViewStates.Visible;
                ProgressBar.Visibility = MessageList.Adapter?.Count > 0 ? ViewStates.Gone : ViewStates.Visible;
                Status.Text = $"Logged in as {user}";
            }
            else
            {
                // Logged out
                LoginButton.Visibility = ViewStates.Visible;
                LogoutButton.Visibility = ViewStates.Gone;
                Status.Visibility = ViewStates.Gone;
                PollContainer.Visibility = ViewStates.Gone;
                ProgressBar.Visibility = ViewStates.Gone;
            }
        }

        private class MessageListAdapter : BaseAdapter<Channel>
        {
            private readonly Context context;
            private readonly Dictionary<string, string> userMap;
            private readonly string currentUserName;

            public MessageListAdapter(Context context, IEnumerable<Channel> unreadChannels, Dictionary<string, string> userMap, string currentUserName)
            {
                this.context = context;
                this.UnreadChannels = unreadChannels.ToList();
                this.userMap = userMap;
                this.currentUserName = currentUserName;
            }

            public List<Channel> UnreadChannels { get; set; }

            public override Channel this[int position] => UnreadChannels[position];

            public override long GetItemId(int position)
            {
                return UnreadChannels[position].Name?.GetHashCode() ?? position;
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
                text1.SetText(item.GetDisplayName(userMap, currentUserName), TextView.BufferType.Normal);
                TextView text2 = (TextView)v.FindViewById(Android.Resource.Id.Text2);
                text2.SetText(item.Latest?.Text ?? "", TextView.BufferType.Normal);

                return v;
            }

            public override int Count => UnreadChannels.Count;
        }
    }
}
