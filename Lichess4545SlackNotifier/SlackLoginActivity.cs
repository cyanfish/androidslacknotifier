using System;
using Android.App;
using Android.Content;
using Android.Net;
using Android.Widget;
using Android.OS;
using Android.Webkit;
using Java.Math;
using Java.Security;
using Org.Json;
using Exception = Java.Lang.Exception;
using Uri = Android.Net.Uri;
using Void = Java.Lang.Void;

namespace Lichess4545SlackNotifier
{
    [Activity(Label = "Lichess4545SlackNotifier", Icon = "@drawable/icon")]
    public class SlackLoginActivity : Activity
    {
        private SecureRandom random = new SecureRandom();

        private string state;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.SlackLogin);

            state = new BigInteger(130, random).ToString(32);

            WebView webView = FindViewById<WebView>(Resource.Id.login_webview);
            webView.Settings.JavaScriptEnabled = true;
            webView.SetWebViewClient(new Client(this, state));

            string url = $"https://slack.com/oauth/authorize?client_id={Creds.client_id}&scope={Constants.scope}&redirect_uri={Constants.redirect_uri}&state={state}&team={Constants.team}";
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

        private class GetAccessTokenTask : AsyncTask<Void, Void, bool>
        {
            private readonly Activity context;
            private readonly string code;

            public GetAccessTokenTask(Activity context, string code)
            {
                this.context = context;
                this.code = code;
            }

            protected override bool RunInBackground(params Void[] @params)
            {
                try
                {
                    string url = $"https://slack.com/api/oauth.access?client_id={Creds.client_id}&client_secret={Creds.client_secret}&code={code}&redirect_uri={Constants.redirect_uri}";
                    JSONObject tokenResult = JsonReader.ReadJsonFromUrl(url);
                    string token = tokenResult.GetString("access_token");
                    context.GetSharedPreferences("prefs", FileCreationMode.Private).Edit().PutString("token", token).Commit();

                    string authUrl = $"https://slack.com/api/auth.test?token={token}";
                    JSONObject authResult = JsonReader.ReadJsonFromUrl(authUrl);
                    context.GetSharedPreferences("prefs", FileCreationMode.Private).Edit().PutString("auth", authResult.ToString()).Commit();
                    return true;
                }
                catch (Exception e)
                {
                }
                return false;
            }

            protected override void OnPostExecute(bool ok)
            {
                if (ok)
                {
                    Toast.MakeText(context, "Login succeeded", ToastLength.Short).Show();
                    context.SetResult(Result.Ok);
                }
                else
                {
                    Toast.MakeText(context, "Login failed", ToastLength.Short).Show();
                    context.SetResult(Result.Ok);
                }
                context.Finish();
            }
        }

        private class Client : WebViewClient
        {
            private readonly Activity context;
            private readonly string state;

            public Client(Activity context, string state)
            {
                this.context = context;
                this.state = state;
            }

            [Obsolete("deprecated")]
            public override bool ShouldOverrideUrlLoading(WebView view, string url)
            {
                if (url.ToString().StartsWith(Constants.redirect_uri))
                {
                    var uri = Uri.Parse(url);
                    string error = uri.GetQueryParameter("error");
                    if (error == null)
                    {
                        string code = uri.GetQueryParameter("code");
                        string returnedState = uri.GetQueryParameter("state");
                        if (state.Equals(returnedState))
                        {
                            new GetAccessTokenTask(context, code).Execute();
                            return true;
                        }
                    }
                    Toast.MakeText(context, "Login failed", ToastLength.Short).Show();
                    context.SetResult(Result.Canceled);
                    context.Finish();
                }
                return false;
            }
        }
    }
}
