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
                JSONObject result = await JsonReader.ReadJsonFromUrlAsync(url);

                JSONArray channels = result.GetJSONArray("channels");
                JSONArray groups = result.GetJSONArray("groups");
                JSONArray mpims = result.GetJSONArray("mpims");
                JSONArray ims = result.GetJSONArray("ims");

                var unreadChannels = new List<JSONObject>();

                extract_unread_channels(channels, unreadChannels);
                extract_unread_channels(groups, unreadChannels);
                extract_unread_channels(mpims, unreadChannels);
                extract_unread_channels(ims, unreadChannels);

                foreach (JSONObject channel in unreadChannels)
                {
                    Log.Debug("slackn", channel.ToString());
                    if (channel.Has("is_im") && channel.GetBoolean("is_im"))
                    {
                        CreateNotification(userMap, context, channel);
                    }
                    if (channel.Has("is_mpim") && channel.GetBoolean("is_mpim"))
                    {
                        CreateNotification(userMap, context, channel);
                    }
                }
            }
            catch (IOException e)
            {
            }
            catch (JSONException e)
            {
            }
        }

        private async Task<Dictionary<string, string>> buildUserMap(string token)
        {
            string url = $"https://slack.com/api/users.list?token={token}";
            JSONObject response = await JsonReader.ReadJsonFromUrlAsync(url);
            JSONArray members = response.GetJSONArray("members");
            var result = new Dictionary<string, string>();
            for (int i = 0; i < members.Length(); i++)
            {
                JSONObject member = members.GetJSONObject(i);
                result.Add(member.GetString("id"), member.GetString("name"));
            }
            return result;
        }

        private void extract_unread_channels(JSONArray channels, List<JSONObject> unreadChannels)
        {
            for (int i = 0; i < channels.Length(); i++)
            {
                JSONObject obj = channels.GetJSONObject(i);
                if (obj.Has("is_archived") && obj.GetBoolean("is_archived"))
                {
                    continue;
                }
                if (obj.Has("unread_count_display") && obj.GetInt("unread_count_display") > 0)
                {
                    unreadChannels.Add(obj);
                }
            }
        }

        private void CreateNotification(Dictionary<string, string> userMap, Context context, JSONObject channel)
        {
            string text = GetLatestText(channel);
            long ts = GetLatestTimestamp(channel);
            string currentUserName = Config.GetLoggedInUser(context);
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
            var uri = Android.Net.Uri.Parse($"slack://channel?team={Constants.team}&id={channel.GetString("id")}"); // G0DFRURGQ
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
            int id = channel.GetString("id").GetHashCode(); // hashCode is not guaranteed unique, but might be good enough
            mNotificationManager.Notify(id, mBuilder.Build());
        }

        private string GetLatestText(JSONObject channel)
        {
            JSONObject latest = channel.GetJSONObject("latest");
            string text = latest.GetString("text");
            text = ReplaceLinks(text);
            return text;
        }

        private long GetLatestTimestamp(JSONObject channel)
        {
            JSONObject latest = channel.GetJSONObject("latest");
            string tsStr = latest.GetString("ts");
            double tsDouble = double.Parse(tsStr);
            long tsLong = (long)(tsDouble * 1000);
            return tsLong;
        }

        private string ReplaceLinks(string text)
        {
            return Regex.Replace(text, "<([@#])([\\w-]+)\\|([\\w-]+)?>", m => m.Groups[1].Value + m.Groups[3].Value);
        }

        private string GetChannelName(Dictionary<string, string> userMap, string currentUserName, JSONObject channel)
        {
            if (channel.Has("is_im") && channel.GetBoolean("is_im"))
            {
                string userId = channel.GetString("user");
                if (!userMap.ContainsKey(userId))
                {
                    return userId;
                }
                return userMap[userId];
            }
            if (channel.Has("is_mpim") && channel.GetBoolean("is_mpim"))
            {
                JSONArray members = channel.GetJSONArray("members");
                var userNames = new List<string>();
                for (int i = 0; i < members.Length(); i++)
                {
                    string userId = members.GetString(i);
                    if (!userMap.ContainsKey(userId))
                    {
                        return channel.GetString("name");
                    }
                    string userName = userMap[userId];
                    if (!userName.Equals(currentUserName))
                    {
                        userNames.Add(userName);
                    }
                }
                return string.Join(", ", userNames);
            }
            if (channel.Has("is_channel") && channel.GetBoolean("is_channel"))
            {
                return "#" + channel.GetString("name");
            }
            if (channel.Has("is_group") && channel.GetBoolean("is_group"))
            {
                return "#" + channel.GetString("name");
            }
            return "";
        }
    }
}