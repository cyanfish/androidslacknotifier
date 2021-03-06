﻿using System;
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
            var response = await JsonUtils.ReadJsonFromUrlAsync<UserListResponse>(url);
            return response.Members?.Where(member => member.Profile?.DisplayName != null)
                                    .ToDictionary(member => member.Id, member => member.Profile?.DisplayName);
        }

        public static async Task<UsersCountsResponse> UsersCounts(string token)
        {
            string url = $"https://slack.com/api/users.counts?token={token}&mpim_aware=true";
            return await JsonUtils.ReadJsonFromUrlAsync<UsersCountsResponse>(url);
        }
        
        public static async Task<AuthResponse> TestAuth(string token)
        {
            string url = $"https://slack.com/api/auth.test?token={token}";
            return await JsonUtils.ReadJsonFromUrlAsync<AuthResponse>(url);
        }

        public static async Task<ChannelHistoryResponse> ChannelHistory(string token, Channel channel)
        {
            var type = channel.IsMpim ? "mpim" :
                       channel.IsIm ? "im" :
                       channel.IsGroup ? "groups" :
                       "channels";
            string url = $"https://slack.com/api/{type}.history?token={token}&channel={channel.Id}&unreads=true";
            return await JsonUtils.ReadJsonFromUrlAsync<ChannelHistoryResponse>(url);
        }

        public static async Task<List<UnreadChannel>> GetUnreadChannels(UsersCountsResponse response, string token, Dictionary<string, string> userMap, string currentUser, IEnumerable<SubscriptionType> subs)
        {
            var channelsWithUnreads = response.AllChannels().Where(x => !x.IsArchived && (x.UnreadCountDisplay > 0 || x.DmCount > 0)).ToList();
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

            return result.OrderByDescending(x => x.LatestTimestamp).ToList();
        }

        private static async Task<List<Message>> MessageHistory(this Channel channel, string token)
        {
            return (await ChannelHistory(token, channel)).Messages.Take(channel.UnreadCountDisplay + channel.DmCount).ToList();
        }

        public static string GetDisplayName(this Channel channel, Dictionary<string, string> userMap, string currentUserName)
        {
            if (channel.IsIm)
            {
                string userId = channel.User ?? channel.UserId;
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

        public static IEnumerable<Channel> AllChannels(this UsersCountsResponse r)
        {
            foreach (var im in r.Ims)
            {
                im.IsIm = true;
            }
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

        public static long LongTimestamp(this Message message)
        {
            string tsStr = message.Ts;
            double tsDouble = double.Parse(tsStr);
            long tsLong = (long)(tsDouble * 1000);
            return tsLong;
        }
    }
}