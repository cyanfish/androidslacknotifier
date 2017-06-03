using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Util;
using Firebase.Messaging;

namespace Lichess4545SlackNotifier
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class MyFirebaseMessagingService : FirebaseMessagingService
    {
        const string TAG = "MyFirebaseMsgService";
        public override void OnMessageReceived(RemoteMessage message)
        {
            Log.Debug(TAG, "From: " + message.From);

            Notifications.Update(ApplicationContext);
        }
    }
}