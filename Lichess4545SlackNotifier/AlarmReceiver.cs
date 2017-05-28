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
                string token = new Prefs(context).Token;

                var userMap = await buildUserMap(token);

                string url = $"https://slack.com/api/rtm.start?token={token}&mpim_aware=true";
                var response = await JsonReader.ReadJsonFromUrlAsync<RtmStartResponse>(url);
                
                var unreadChannels = new List<Channel>();

                extract_unread_channels(response.Channels, unreadChannels);
                extract_unread_channels(response.Groups, unreadChannels);
                extract_unread_channels(response.Mpims, unreadChannels);
                extract_unread_channels(response.Ims, unreadChannels);

                foreach (var channel in unreadChannels)
                {
                    Log.Debug("slackn", channel.ToString());
                    if (channel.IsIm)
                    {
                        CreateNotification(userMap, context, channel);
                    }
                    if (channel.IsMpim)
                    {
                        CreateNotification(userMap, context, channel);
                    }
                }
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

        private async Task<Dictionary<string, string>> buildUserMap(string token)
        {
            string url = $"https://slack.com/api/users.list?token={token}";
            var response = await JsonReader.ReadJsonFromUrlAsync<UserListResponse>(url);
            return response.Members.ToDictionary(member => member.Id, member => member.Name);
        }

        private void extract_unread_channels(List<Channel> channels, List<Channel> unreadChannels)
        {
            foreach (var channel in channels)
            {
                if (channel.IsArchived)
                {
                    continue;
                }
                if (channel.UnreadCountDisplay > 0)
                {
                    unreadChannels.Add(channel);
                }
            }
        }

        private void CreateNotification(Dictionary<string, string> userMap, Context context, Channel channel)
        {
            string text = GetLatestText(channel);
            long ts = GetLatestTimestamp(channel);
            string currentUserName = new Prefs(context).Auth.User;
            Notification.Builder mBuilder =
                new Notification.Builder(context)
                    .SetSmallIcon(Resource.Drawable.slack_icon_full)
                    .SetContentTitle(GetChannelName(userMap, currentUserName, channel))
                    .SetContentText(text)
                    .SetWhen(ts)
                    .SetShowWhen(true)
                    .SetAutoCancel(true);
            mBuilder.SetStyle(new Notification.BigTextStyle().BigText(text));
            // Creates an explicit intent for an Activity in your app
            var uri = Android.Net.Uri.Parse($"slack://channel?team={Constants.Team}&id={channel.Id}"); // G0DFRURGQ
            Intent resultIntent = new Intent(Intent.ActionView, uri);

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
            int id = channel.Id.GetHashCode(); // hashCode is not guaranteed unique, but might be good enough
            mNotificationManager.Notify(id, mBuilder.Build());
        }

        private string GetLatestText(Channel channel)
        {
            string text = channel.Latest.Text;
            text = ReplaceLinks(text);
            return text;
        }

        private long GetLatestTimestamp(Channel channel)
        {
            string tsStr = channel.Latest.Ts;
            double tsDouble = double.Parse(tsStr);
            long tsLong = (long)(tsDouble * 1000);
            return tsLong;
        }

        private string ReplaceLinks(string text)
        {
            return Regex.Replace(text, "<([@#])([\\w-]+)\\|([\\w-]+)?>", m => m.Groups[1].Value + m.Groups[3].Value);
        }

        private string GetChannelName(Dictionary<string, string> userMap, string currentUserName, Channel channel)
        {
            if (channel.IsIm)
            {
                string userId = channel.User;
                return userMap.TryGetValue(userId, out string userName) ? userName : userId;
            }
            if (channel.IsMpim)
            {
                try
                {
                    return string.Join(", ", channel.Members.Select(x => userMap[x]).Where(x => x != currentUserName));
                }
                catch (KeyNotFoundException)
                {
                    return channel.Name;
                }
            }
            if (channel.IsChannel || channel.IsGroup)
            {
                return "#" + channel.Name;
            }
            return "";
        }
    }
}
