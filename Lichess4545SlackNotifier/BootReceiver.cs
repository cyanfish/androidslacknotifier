﻿using System;
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
    [BroadcastReceiver(Enabled = false, Exported = false)]
    public class BootReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            Log.Info("slackn", "Boot received");
            new AlarmSetter(context).SetAlarm();
        }
    }
}