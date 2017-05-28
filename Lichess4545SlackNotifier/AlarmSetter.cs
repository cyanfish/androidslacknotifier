using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Ninject;
using Org.Json;

namespace Lichess4545SlackNotifier
{
    public class AlarmSetter
    {
        private readonly Context context;

        public AlarmSetter(Context context)
        {
            this.context = context;
            Prefs = new Prefs(context);
        }
        
        private Prefs Prefs { get; }

        public void SetAlarm()
        {
            AlarmManager alarmManager = (AlarmManager)context.GetSystemService(Context.AlarmService);
            Intent intent = new Intent(context, typeof(AlarmReceiver));
            PendingIntent alarmIntent = PendingIntent.GetBroadcast(context, 0, intent, 0);

            ComponentName receiver = new ComponentName(context, Java.Lang.Class.FromType(typeof(BootReceiver)));
            PackageManager pm = context.PackageManager;

            long interval = new Prefs(context).Interval;
            if (Prefs.Auth.User == null || interval == 0) {
                Log.Debug("slackn", "Cancelling alarm");
                alarmManager.Cancel(alarmIntent);
                pm.SetComponentEnabledSetting(receiver,
                    ComponentEnabledState.Disabled, 
                    ComponentEnableOption.DontKillApp);
            } else {
                Log.Debug("slackn", "Setting alarm for interval " + interval);
                alarmManager.SetInexactRepeating(AlarmType.ElapsedRealtime, SystemClock.ElapsedRealtime(), interval, alarmIntent);
                pm.SetComponentEnabledSetting(receiver,
                    ComponentEnabledState.Enabled, 
                    ComponentEnableOption.DontKillApp);
            }
        }
    }
}