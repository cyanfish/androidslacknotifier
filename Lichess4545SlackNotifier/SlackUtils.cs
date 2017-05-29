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

        public static async Task<RtmStartResponse> RtmStart(string token)
        {
            string url = $"https://slack.com/api/rtm.start?token={token}&mpim_aware=true";
            return await JsonReader.ReadJsonFromUrlAsync<RtmStartResponse>(url);
        }

        public static async Task<ChannelHistoryResponse> ChannelHistory(string token, string channelId)
        {
            string url = $"https://slack.com/api/channels.history?token={token}&channel={channelId}&unreads=true";
            return await JsonReader.ReadJsonFromUrlAsync<ChannelHistoryResponse>(url);
        }

        public static async Task<ChannelHistoryResponse> AnnounceHistory(string token)
        {
            return await ChannelHistory(token, Constants.AnnounceChannelId);
        }

        public static async Task<IEnumerable<UnreadChannel>> GetUnreadChannels(RtmStartResponse response, string token, Dictionary<string, string> userMap, string currentUser, IEnumerable<SubscriptionType> subs)
        {
            var channelsWithUnreads = response.AllChannels().Where(x => !x.IsArchived && x.UnreadCountDisplay > 0).ToList();
            var result = new List<UnreadChannel>();
            if (subs.Contains(SubscriptionType.DirectMessages))
            {
                result.AddRange(await Task.WhenAll(channelsWithUnreads.Where(x => x.IsIm || x.IsMpim).Select(async x => new UnreadChannel
                {
                    ChannelId = x.Id,
                    ChannelName = x.GetDisplayName(userMap, currentUser),
                    Messages = await x.MessageHistory(token)
                })));
            }
            var announceChannel = channelsWithUnreads.FirstOrDefault(x => x.Id == Constants.AnnounceChannelId);
            var announceSubs = subs.Intersect(SubscriptionType.AllAnnounce).ToList();
            if (announceChannel != null && announceSubs.Any())
            {
                var messages = await announceChannel.MessageHistory(token);
                var subscribedMessages = messages.Where(x =>
                    announceSubs.Any(y =>
                        x.Text.IndexOf(y.Tag, StringComparison.InvariantCultureIgnoreCase) != -1)).ToList();
                if (subscribedMessages.Any())
                {
                    result.Add(new UnreadChannel
                    {
                        ChannelId = announceChannel.Id,
                        ChannelName = announceChannel.GetDisplayName(userMap, currentUser),
                        Messages = subscribedMessages
                    });
                }
            }

            return result;
        }

        private static async Task<List<Message>> MessageHistory(this Channel channel, string token)
        {
            return channel.UnreadCountDisplay > 1
                ? (await ChannelHistory(token, channel.Id)).Messages.Take(channel.UnreadCountDisplay + 5).ToList()
                : new List<Message> { channel.Latest };
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

        public static Intent GetIntent(this UnreadChannel channel)
        {
            var uri = Android.Net.Uri.Parse($"slack://channel?team={Constants.Team}&id={channel.ChannelId}"); // G0DFRURGQ
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