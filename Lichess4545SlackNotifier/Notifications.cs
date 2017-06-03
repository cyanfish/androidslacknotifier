using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Lichess4545SlackNotifier
{
    public class Notifications
    {
        public static async void Update(Context context)
        {
            var prefs = new Prefs(context);
            string token = prefs.Token;

            var readAuth = SlackUtils.TestAuth(token);
            var readRtm = SlackUtils.RtmStart(token);
            var readUserMap = SlackUtils.BuildUserMap(token);

            prefs.Auth = await readAuth;
            if (prefs.Auth.Ok)
            {
                var userMap = await readUserMap;
                prefs.LatestUserMap = userMap;

                var response = await readRtm;

                var unreads = await SlackUtils.GetUnreadChannels(response, token, userMap, prefs.Auth.User, prefs.Subscriptions);
                prefs.LatestUnreads = unreads;

                Update(userMap, context, unreads, prefs.LastDismissedTs);
            }
        }

        public static void Update(Dictionary<string, string> userMap, Context context, IEnumerable<UnreadChannel> unreadChannels, long lastDismissedTs)
        {
            var channels = unreadChannels.Where(x => x.LatestTimestamp > lastDismissedTs).ToList();
            NotificationManager mNotificationManager =
                (NotificationManager)context.GetSystemService(Context.NotificationService);
            if (!channels.Any())
            {
                mNotificationManager.CancelAll();
                return;
            }
            string title = channels.Count > 1
                ? $"{channels.Select(x => x.Messages.Count).Sum()} unread messages"
                : channels.Single().ChannelName;
            var mostRecentChannel = channels.OrderByDescending(x => x.LatestTimestamp).First();
            var messagesNotSeen = mostRecentChannel.Messages.Where(x => x.LongTimestamp() > lastDismissedTs);
            string text = string.Join("\n",
                messagesNotSeen.OrderBy(x => x.Ts).Select(x => x.DisplayText(userMap)));
            long ts = channels.Select(x => x.LatestTimestamp).Max();
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
            mBuilder.SetDeleteIntent(PendingIntent.GetBroadcast(context.ApplicationContext, 0, intent, PendingIntentFlags.UpdateCurrent));

            Intent resultIntent = new Intent(context, typeof(MainActivity));
            resultIntent.AddFlags(ActivityFlags.SingleTop);
            resultIntent.PutExtra("ts", ts);

            // The stack builder object will contain an artificial back stack for the
            // started Activity.
            // This ensures that navigating backward from the Activity leads out of
            // your application to the Home screen.
            // TaskStackBuilder stackBuilder = TaskStackBuilder.create(context);
            // Adds the back stack for the Intent (but not the Intent itself)
            // stackBuilder.addParentStack(ResultActivity.class);
            // Adds the Intent that starts the Activity to the top of the stack
            PendingIntent resultPendingIntent = PendingIntent.GetActivity(context, 0, resultIntent, PendingIntentFlags.UpdateCurrent);
            mBuilder.SetContentIntent(resultPendingIntent);
            // mId allows you to update the notification later on.
            int id = 0;
            mNotificationManager.Notify(id, mBuilder.Build());
        }
    }
}