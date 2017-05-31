using System;
using Android.App;
using Android.Content;
using Android.Net;
using Android.Widget;
using Android.OS;
using Android.Webkit;
using Java.Math;
using Java.Security;
using Lichess4545SlackNotifier.SlackApi;
using Exception = Java.Lang.Exception;
using Uri = Android.Net.Uri;

namespace Lichess4545SlackNotifier
{
    [Activity(Label = "Login to Slack", Icon = "@drawable/icon")]
    public class SlackLoginActivity : Activity
    {
        private readonly SecureRandom random = new SecureRandom();

        private string state;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

#if DEBUG
            // Log in automatically with test credentials
            var Prefs = new Prefs(this);
            Prefs.Token = Creds.TestToken;
            Prefs.Auth = new AuthResponse { Ok = true, User = Creds.TestUser };
            Finish();
            return;
#endif

            SetContentView(Resource.Layout.SlackLogin);

            state = new BigInteger(130, random).ToString(32);

            WebView webView = FindViewById<WebView>(Resource.Id.login_webview);
            webView.Settings.JavaScriptEnabled = true;
            webView.SetWebViewClient(new Client(this, state));

            string url = $"https://slack.com/oauth/authorize?client_id={Creds.ClientId}&scope={Constants.Scope}&redirect_uri={Constants.RedirectUri}&state={state}&team={Constants.Team}";
            webView.LoadUrl(url);
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            outState.PutString("state", state);
        }

        protected override void OnRestoreInstanceState(Bundle savedInstanceState)
        {
            base.OnRestoreInstanceState(savedInstanceState);
            state = savedInstanceState.GetString("state");
        }

        private class Client : WebViewClient
        {
            private readonly Activity context;
            private readonly string state;

            public Client(Activity context, string state)
            {
                this.context = context;
                this.state = state;
                Prefs = new Prefs(context);
            }

            private Prefs Prefs { get; }

            [Obsolete("deprecated")]
            public override bool ShouldOverrideUrlLoading(WebView view, string url)
            {
                if (url.StartsWith(Constants.RedirectUri))
                {
                    var uri = Uri.Parse(url);
                    string error = uri.GetQueryParameter("error");
                    if (error == null)
                    {
                        string code = uri.GetQueryParameter("code");
                        string returnedState = uri.GetQueryParameter("state");
                        if (state.Equals(returnedState))
                        {
                            GetAccessToken(code);
                            return true;
                        }
                    }
                    Toast.MakeText(context, "Login failed", ToastLength.Short).Show();
                    context.SetResult(Result.Canceled);
                    context.Finish();
                }
                return false;
            }

            private async void GetAccessToken(string code)
            {
                try
                {
                    string url =
                        $"https://slack.com/api/oauth.access?client_id={Creds.ClientId}&client_secret={Creds.ClientSecret}&code={code}&redirect_uri={Constants.RedirectUri}";
                    var tokenResponse = await JsonReader.ReadJsonFromUrlAsync<TokenResponse>(url);
                    Prefs.Token = tokenResponse.AccessToken;
                    
                    Prefs.Auth = await SlackUtils.TestAuth(Prefs.Token);

                    Toast.MakeText(context, "Login succeeded", ToastLength.Short).Show();
                    context.SetResult(Result.Ok);
                }
                catch (Exception)
                {
                    Toast.MakeText(context, "Login failed", ToastLength.Short).Show();
                    context.SetResult(Result.Ok);
                }
                context.Finish();
            }
        }
    }
}
