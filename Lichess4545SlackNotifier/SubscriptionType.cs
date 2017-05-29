using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public class SubscriptionType
    {
        public static readonly SubscriptionType DirectMessages = new SubscriptionType("dm", null, "Direct messages");

        public static readonly SubscriptionType TeamAnnounce = new SubscriptionType("team_a", "[Team]", "Team announcements");

        public static readonly SubscriptionType LonewolfAnnounce = new SubscriptionType("lonewolf_a", "[Lonewolf]", "Lonewolf announcements");

        public static readonly SubscriptionType LadderAnnounce = new SubscriptionType("ladder_a", "[Ladder]", "Ladder announcements");

        public static readonly SubscriptionType BlitzAnnounce = new SubscriptionType("blitz_a", "[Blitz]", "Blitz announcements");

        public static readonly SubscriptionType LedgerAnnounce = new SubscriptionType("ledger_a", "[Ledger]", "Ledger announcements");

        public static readonly IReadOnlyList<SubscriptionType> AllAnnounce = new ReadOnlyCollection<SubscriptionType>(new[] { TeamAnnounce, LonewolfAnnounce, LadderAnnounce, BlitzAnnounce, LedgerAnnounce });

        public static readonly IReadOnlyList<SubscriptionType> All = new ReadOnlyCollection<SubscriptionType>(AllAnnounce.Concat(new[] { DirectMessages }).ToArray());

        private static readonly Dictionary<string, SubscriptionType> Map = All.ToDictionary(x => x.Id);

        public static SubscriptionType FromId(string id) => Map[id];

        private SubscriptionType(string id, string tag, string name)
        {
            Id = id;
            Tag = tag;
            Name = name;
        }

        public string Name { get; }

        public string Tag { get; }

        public string Id { get; }
    }
}