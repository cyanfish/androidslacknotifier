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
    [IntentFilter(new[] { Intent.ActionMyPackageReplaced })]
    public class PackageReplacedReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            Log.Info("slackn", "Package replaced received");
            new AlarmSetter(context).SetAlarm();
        }
    }
}