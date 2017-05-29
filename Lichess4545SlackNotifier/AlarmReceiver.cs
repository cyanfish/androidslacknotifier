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
using Lichess4545SlackNotifier.SlackApi;
using Newtonsoft.Json;
using Org.Json;

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
            // unread_count_display

            try
            {
                var prefs = new Prefs(context);
                string token = prefs.Token;

                var userMap = await SlackUtils.BuildUserMap(token);
                
                var response = await SlackUtils.RtmStart(token);

                var unreads = await SlackUtils.GetUnreadChannels(response, token, userMap, prefs.Auth.User, prefs.Subscriptions);
                
                Notifications.Update(userMap, context, unreads, prefs.LastDismissedTs);
            }
            catch (IOException e)
            {
                Log.Error("slackn", e.ToString());
            }
            catch (JsonSerializationException e)
            {
                Log.Error("slackn", e.ToString());
            }
        }
    }
}
