using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Widget;
using Android.OS;
using Android.Views;
using Lichess4545SlackNotifier.SlackApi;
using Ninject;

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

        [Inject]
        public Prefs Prefs { get; set; }

        [Inject]
        public AlarmSetter AlarmSetter { get; set; }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main2);

            KernelManager.Inject(this);
            
            LoginButton.Click += (sender, args) => StartActivityForResult(new Intent(this, typeof(SlackLoginActivity)), LOGIN_REQUEST);
            
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
            Prefs.Auth = await JsonReader.ReadJsonFromUrlAsync<AuthResponse>(url);
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
                Status.Text = $"Logged in as {user}";
            }
            else
            {
                // Logged out
                LoginButton.Visibility = ViewStates.Visible;
                LogoutButton.Visibility = ViewStates.Gone;
                Status.Visibility = ViewStates.Gone;
            }
        }
    }
}
