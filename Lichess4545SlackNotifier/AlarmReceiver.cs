using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Newtonsoft.Json;
using Exception = Java.Lang.Exception;

namespace Lichess4545SlackNotifier
{
    [BroadcastReceiver(Enabled = true, Exported = true)]
    public class AlarmReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            Log.Info("slackn", "Alarm received");
            Poll(context);
        }

        private async void Poll(Context context)
        {
            try
            {
                var prefs = new Prefs(context);
                string token = prefs.Token;

                var userMap = await SlackUtils.BuildUserMap(token);
                prefs.LatestUserMap = userMap;

                var response = await SlackUtils.RtmStart(token);

                var unreads = await SlackUtils.GetUnreadChannels(response, token, userMap, prefs.Auth.User, prefs.Subscriptions);
                prefs.LatestUnreads = unreads;

                Notifications.Update(userMap, context, unreads, prefs.LastDismissedTs);
            }
            catch (Exception e)
            {
                Log.Error("slackn", e.ToString());
            }
        }
    }
}
