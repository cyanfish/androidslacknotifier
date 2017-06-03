using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Util;
using Firebase.Iid;

namespace Lichess4545SlackNotifier
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.INSTANCE_ID_EVENT" })]
    public class MyFirebaseIIDService : FirebaseInstanceIdService
    {
        private const string TAG = "MyFirebaseIIDService";

        public override void OnTokenRefresh()
        {
            var refreshedToken = FirebaseInstanceId.Instance.Token;
            Log.Debug(TAG, "Refreshed token: " + refreshedToken);
            new CloudMessaging(ApplicationContext).Register();
        }
    }
}