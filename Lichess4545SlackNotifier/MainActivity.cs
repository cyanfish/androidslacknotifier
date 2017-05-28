using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Widget;
using Android.OS;
using Android.Views;
using Java.Lang;
using Ninject;
using Org.Json;

namespace Lichess4545SlackNotifier
{
    [Activity(Label = "Lichess4545SlackNotifier", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private const int LOGIN_REQUEST = 1;

        [InjectView(Resource.Id.LoginButton)]
        private Button LoginButton { get; set; }

        [InjectView(Resource.Id.LogoutButton)]
        private Button LogoutButton { get; set; }

        [InjectView(Resource.Id.status)]
        private TextView Status { get; set; }

        [InjectView(Resource.Id.IntervalSpinner)]
        private Spinner IntervalSpinner { get; set; }

        [Inject]
        public Prefs Prefs { get; set; }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main2);

            KernelManager.Inject(this);
            
            LoginButton = FindViewById<Button>(Resource.Id.LoginButton);
            LoginButton.Click += (sender, args) => StartActivityForResult(new Intent(this, typeof(SlackLoginActivity)), LOGIN_REQUEST);

            LogoutButton = FindViewById<Button>(Resource.Id.LogoutButton);
            LogoutButton.Click += (sender, args) =>
            {
                Prefs.Auth = null;
                RefreshDisplay(true);
            };

            Status = FindViewById<TextView>(Resource.Id.status);

            string[] intervalChoices = { "Disabled", "Every minute", "Every 10 minutes", "Every 20 minutes", "Every 30 minutes", "Every hour", "Every 2 hours" };
            var intervalValues = new List<long> { 0L, TimeConstants.Minute, 10 * TimeConstants.Minute, 20 * TimeConstants.Minute, 30 * TimeConstants.Minute, TimeConstants.Hour, 2 * TimeConstants.Hour };
            IntervalSpinner = FindViewById<Spinner>(Resource.Id.IntervalSpinner);
            IntervalSpinner.Adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, intervalChoices);
            long interval = Prefs.Interval;
            IntervalSpinner.SetSelection(intervalValues.IndexOf(interval));
            IntervalSpinner.ItemSelected += (sender, args) =>
            {
                long newValue = intervalValues[args.Position];
                long oldValue = Config.GetAlarmInterval(this);
                if (newValue != oldValue)
                {
                    Prefs.Interval = newValue;
                    Config.SetAlarm(this);
                }
            };

            RefreshDisplay(true);
            TestAuth();
            Config.SetAlarm(this);
        }

        private async void TestAuth()
        {
            string token = Prefs.Token;
            if (token == null)
            {
                return;
            }
            string url = $"https://slack.com/api/auth.test?token={token}";
            JSONObject result = await JsonReader.ReadJsonFromUrlAsync(url);
            Prefs.Token = result.ToString();
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode == LOGIN_REQUEST)
            {
                if (resultCode == Result.Ok)
                {
                    RefreshDisplay(true);
                    Config.SetAlarm(this);
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

            string user = Config.GetLoggedInUser(this);
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
