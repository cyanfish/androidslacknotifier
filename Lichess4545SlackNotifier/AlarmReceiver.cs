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

                string url = $"https://slack.com/api/rtm.start?token={token}&mpim_aware=true";
                var response = await JsonReader.ReadJsonFromUrlAsync<RtmStartResponse>(url);
                
                var unreadChannels = response.AllChannels().Where(x => !x.IsArchived && x.UnreadCountDisplay > 0);

                var notifyChannels = unreadChannels.Where(x => (x.IsIm || x.IsMpim) && GetLatestTimestamp(x) > prefs.LastDismissedTs);
                CreateNotification(userMap, context, notifyChannels.ToList());
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

        private void CreateNotification(Dictionary<string, string> userMap, Context context, List<Channel> channels)
        {
            if (!channels.Any())
            {
                return;
            }
            string currentUserName = new Prefs(context).Auth.User;
            string title = channels.Count > 1
                ? $"{channels.Select(x => x.UnreadCountDisplay).Sum()} unread messages"
                : channels.Single().GetDisplayName(userMap, currentUserName);
            string text = channels.OrderByDescending(GetLatestTimestamp).First().Latest.DisplayText(userMap);
            long ts = channels.Select(GetLatestTimestamp).Max();
            Notification.Builder mBuilder =
                new Notification.Builder(context)
                    .SetSmallIcon(Resource.Drawable.slack_icon_full)
                    .SetContentTitle(title)
                    .SetContentText(text)
                    .SetWhen(ts)
                    .SetShowWhen(true)
                    .SetAutoCancel(true);
            mBuilder.SetStyle(new Notification.BigTextStyle().BigText(text));
            // Creates an explicit intent for an Activity in your app
            Intent intent = new Intent(context, typeof(DismissNotificationReceiver));
            intent.PutExtra("ts", ts);
            mBuilder.SetDeleteIntent(PendingIntent.GetBroadcast(context.ApplicationContext, 0, intent, 0));

            Intent resultIntent = new Intent(context, typeof(MainActivity));

            // The stack builder object will contain an artificial back stack for the
            // started Activity.
            // This ensures that navigating backward from the Activity leads out of
            // your application to the Home screen.
            // TaskStackBuilder stackBuilder = TaskStackBuilder.create(context);
            // Adds the back stack for the Intent (but not the Intent itself)
            // stackBuilder.addParentStack(ResultActivity.class);
            // Adds the Intent that starts the Activity to the top of the stack
            PendingIntent resultPendingIntent = PendingIntent.GetActivity(context, 0, resultIntent, 0);
            mBuilder.SetContentIntent(resultPendingIntent);
            NotificationManager mNotificationManager =
                (NotificationManager)context.GetSystemService(Context.NotificationService);
            // mId allows you to update the notification later on.
            int id = 0;
            mNotificationManager.Notify(id, mBuilder.Build());
        }

        private long GetLatestTimestamp(Channel channel)
        {
            string tsStr = channel.Latest.Ts;
            double tsDouble = double.Parse(tsStr);
            long tsLong = (long)(tsDouble * 1000);
            return tsLong;
        }
    }
}
