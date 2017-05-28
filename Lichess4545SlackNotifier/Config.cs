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
using Org.Json;

namespace Lichess4545SlackNotifier
{
    public class Config
    {
        private const int SECOND = 1000;
        private const int MINUTE = 60 * SECOND;
        private const int HOUR = 60 * MINUTE;

        public static void SetAlarm(Context context)
        {
            AlarmManager alarmManager = (AlarmManager)context.GetSystemService(Context.AlarmService);
            Intent intent = new Intent(context, typeof(AlarmReceiver));
            PendingIntent alarmIntent = PendingIntent.GetBroadcast(context, 0, intent, 0);

            ComponentName receiver = new ComponentName(context, Java.Lang.Class.FromType(typeof(BootReceiver)));
            PackageManager pm = context.PackageManager;

            long interval = GetAlarmInterval(context);
            if (GetLoggedInUser(context) == null || interval == 0) {
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

        public static String GetLoggedInUser(Context context)
        {
            var auth = new Prefs(context).Auth;
            if (auth.GetBoolean("ok"))
            {
                return auth.GetString("user");
            }
            return null;
        }

        public static long GetAlarmInterval(Context context)
        {
            return new Prefs(context).Interval;
        }
    }
}