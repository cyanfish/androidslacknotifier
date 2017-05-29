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
    public class Constants
    {
        // TODO: If we switch to the events API and a push messaging server, change scopes to these:
        // channels:read channels:history groups:read groups:history im:read im:history mpim:read mpim:history
        public const string Scope = "client";

        public const string RedirectUri = "https://lichess4545.com/redirect";

        public const string Team = "T0CSGMP0R";

        public const string AnnounceChannelName = "announcements";

        public const string AnnounceChannelId = "C2UP34BCZ";
    }
}