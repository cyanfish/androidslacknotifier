using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Android.Content;
using Lichess4545SlackNotifier.SlackApi;

namespace Lichess4545SlackNotifier
{
    public static class SlackUtils
    {
        public static async Task<Dictionary<string, string>> BuildUserMap(string token)
        {
            string url = $"https://slack.com/api/users.list?token={token}";
            var response = await JsonReader.ReadJsonFromUrlAsync<UserListResponse>(url);
            return response.Members.ToDictionary(member => member.Id, member => member.Name);
        }

        public static string GetDisplayName(this Channel channel, Dictionary<string, string> userMap, string currentUserName)
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

        public static IEnumerable<Channel> AllChannels(this RtmStartResponse r)
        {
            return r.Mpims.Concat(r.Ims).Concat(r.Channels).Concat(r.Groups);
        }

        public static Intent GetIntent(this Channel channel)
        {
            var uri = Android.Net.Uri.Parse($"slack://channel?team={Constants.Team}&id={channel.Id}"); // G0DFRURGQ
            return new Intent(Intent.ActionView, uri);
        }

        public static string DisplayText(this Message message, Dictionary<string, string> userMap)
        {
            var text = Regex.Replace(message.Text, "<([@#])([\\w-]+)\\|([\\w-]+)?>", m => m.Groups[1].Value + m.Groups[3].Value);
            text = Regex.Replace(text, "<@(U[\\w-]+)>", m => userMap.TryGetValue(m.Groups[1].Value, out string userName) ? "@" + userName : m.Groups[1].Value);
            return text;
        }
    }
}