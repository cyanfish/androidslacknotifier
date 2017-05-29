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
    public class UnreadChannel
    {
        public string ChannelName { get; set; }

        public List<SlackApi.Message> Messages { get; set; }
    }
}