using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Lichess4545SlackNotifier
{
    [BroadcastReceiver(Enabled = true, Exported = true)]
    public class DismissNotificationReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            Log.Info("slackn", "Dimiss notification received");
            var prefs = new Prefs(context);
            prefs.LastDismissedTs = Math.Max(prefs.LastDismissedTs, intent.GetLongExtra("ts", -1));
        }
    }
}