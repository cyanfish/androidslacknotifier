using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Java.Util.Regex;
using Org.Json;
using Boolean = System.Boolean;
using Double = System.Double;
using Pattern = Android.OS.Pattern;
using String = System.String;

namespace Lichess4545SlackNotifier
{
    [BroadcastReceiver(Enabled = true, Exported = true)]
    public class AlarmReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            Log.Info("slackn", "Alarm received");
            new PollTask().Execute(context);
        }

        private class PollTask : AsyncTask<Context, Java.Lang.Void, Boolean>
        {
            private Dictionary<String, String> userMap;

            protected override bool RunInBackground(params Context[] context)
            {
                // unread_count_display

                try
                {
                    String token = context[0].GetSharedPreferences("prefs", FileCreationMode.Private).GetString("token", null);

                    userMap = buildUserMap(token);

                    String url = String.Format("https://slack.com/api/rtm.start?token=%s&mpim_aware=true", token);
                    JSONObject result = JsonReader.ReadJsonFromUrl(url);

                    JSONArray channels = result.GetJSONArray("channels");
                    JSONArray groups = result.GetJSONArray("groups");
                    JSONArray mpims = result.GetJSONArray("mpims");
                    JSONArray ims = result.GetJSONArray("ims");

                    var unread_channels = new List<JSONObject>();

                    extract_unread_channels(channels, unread_channels);
                    extract_unread_channels(groups, unread_channels);
                    extract_unread_channels(mpims, unread_channels);
                    extract_unread_channels(ims, unread_channels);

                    foreach (JSONObject channel in unread_channels)
                    {
                        Log.Debug("slackn", channel.ToString());
                        if (channel.Has("is_im") && channel.GetBoolean("is_im"))
                        {
                            createNotification(context[0], channel);
                        }
                        if (channel.Has("is_mpim") && channel.GetBoolean("is_mpim"))
                        {
                            createNotification(context[0], channel);
                        }
                    }

                    return true;
                }
                catch (IOException e)
                {
                }
                catch (JSONException e)
                {
                }
                return false;
            }

            private Dictionary<String, String> buildUserMap(String token)
            {
                String url = String.Format("https://slack.com/api/users.list?token=%s", token);
                JSONObject response = JsonReader.ReadJsonFromUrl(url);
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

            private void createNotification(Context context, JSONObject channel)
            {
                String text = getLatestText(channel);
                long ts = getLatestTimestamp(channel);
                String currentUserName = Config.GetLoggedInUser(context);
                Notification.Builder mBuilder =
                    new Notification.Builder(context)
                        .SetSmallIcon(Resource.Drawable.slack_icon_full)
                        .SetContentTitle(GetChannelName(currentUserName, channel))
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

            private string getLatestText(JSONObject channel)
            {
                JSONObject latest = channel.GetJSONObject("latest");
                string text = latest.GetString("text");
                text = ReplaceLinks(text);
                return text;
            }

            private long getLatestTimestamp(JSONObject channel)
            {
                JSONObject latest = channel.GetJSONObject("latest");
                string tsStr = latest.GetString("ts");
                double tsDouble = Double.Parse(tsStr);
                long tsLong = (long)(tsDouble * 1000);
                return tsLong;
            }

            private string ReplaceLinks(string text)
            {
                return Regex.Replace(text, "<([@#])([\\w-]+)\\|([\\w-]+)?>", m => m.Groups[1].Value + m.Groups[3].Value);
            }

            private string GetChannelName(string currentUserName, JSONObject channel)
            {
                if (channel.Has("is_im") && channel.GetBoolean("is_im"))
                {
                    String userId = channel.GetString("user");
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
                        String userId = members.GetString(i);
                        if (!userMap.ContainsKey(userId))
                        {
                            return channel.GetString("name");
                        }
                        String userName = userMap[userId];
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
}