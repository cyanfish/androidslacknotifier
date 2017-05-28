using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Widget;
using Android.OS;
using Android.Views;
using Java.Lang;
using Org.Json;

namespace Lichess4545SlackNotifier
{
    [Activity(Label = "Lichess4545SlackNotifier", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private const int SECOND = 1000;
        private const int MINUTE = 60 * SECOND;
        private const int HOUR = 60 * MINUTE;

        private const int LOGIN_REQUEST = 1;

        private Button loginButton;
        private Button logoutButton;
        private TextView status;
        private Spinner intervalSpinner;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main2);
            
            loginButton = FindViewById<Button>(Resource.Id.LoginButton);
            loginButton.Click += (sender, args) => StartActivityForResult(new Intent(this, typeof(SlackLoginActivity)), LOGIN_REQUEST);

            logoutButton = FindViewById<Button>(Resource.Id.LogoutButton);
            logoutButton.Click += (sender, args) =>
            {
                GetSharedPreferences("prefs", FileCreationMode.Private).Edit().Remove("auth").Commit();
                RefreshDisplay(true);
            };

            status = FindViewById<TextView>(Resource.Id.status);

            string[] intervalChoices = { "Disabled", "Every minute", "Every 10 minutes", "Every 20 minutes", "Every 30 minutes", "Every hour", "Every 2 hours" };
            var intervalValues = new List<long> { 0L, MINUTE, 10 * MINUTE, 20 * MINUTE, 30 * MINUTE, HOUR, 2 * HOUR };
            intervalSpinner = FindViewById<Spinner>(Resource.Id.IntervalSpinner);
            intervalSpinner.Adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, intervalChoices);
            long interval = GetSharedPreferences("prefs", FileCreationMode.Private).GetLong("interval", HOUR);
            intervalSpinner.SetSelection(intervalValues.IndexOf(interval));
            intervalSpinner.ItemSelected += (sender, args) =>
            {
                long newValue = intervalValues[args.Position];
                long oldValue = Config.GetAlarmInterval(this);
                if (newValue != oldValue)
                {
                    GetSharedPreferences("prefs", FileCreationMode.Private).Edit().PutLong("interval", newValue).Commit();
                    Config.SetAlarm(this);
                }
            };

            RefreshDisplay(true);
            new TestAuthTask(this).Execute();
            Config.SetAlarm(this);
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
                loginButton.Visibility = ViewStates.Visible;
                logoutButton.Visibility = ViewStates.Gone;
                status.Visibility = ViewStates.Visible;
                status.Text = "Couldn't connect to slack";
                return;
            }

            string user = Config.GetLoggedInUser(this);
            if (user != null)
            {
                // Logged in
                loginButton.Visibility = ViewStates.Gone;
                logoutButton.Visibility = ViewStates.Visible;
                status.Visibility = ViewStates.Visible;
                status.Text = $"Logged in as {user}";
            }
            else
            {
                // Logged out
                loginButton.Visibility = ViewStates.Visible;
                logoutButton.Visibility = ViewStates.Gone;
                status.Visibility = ViewStates.Gone;
            }
        }

        private class TestAuthTask : AsyncTask<Void, Void, bool>
        {
            private readonly Activity context;

            public TestAuthTask(Activity context)
            {
                this.context = context;
            }

            protected override bool RunInBackground(params Void[] @params)
            {
                try
                {
                    string token = context.GetSharedPreferences("prefs", FileCreationMode.Private).GetString("token", null);
                    if (token == null)
                    {
                        return true;
                    }
                    string url = $"https://slack.com/api/auth.test?token={token}";
                    JSONObject result = JsonReader.ReadJsonFromUrl(url);
                    context.GetSharedPreferences("prefs", FileCreationMode.Private).Edit().PutString("auth", result.ToString()).Commit();
                    return true;
                }
                catch (Exception e)
                {
                }
                return false;
            }
        }
    }
}
