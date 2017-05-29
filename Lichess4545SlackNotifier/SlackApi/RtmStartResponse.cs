using System.Collections.Generic;
using System.Linq;

namespace Lichess4545SlackNotifier.SlackApi
{
    public class RtmStartResponse
    {
        public List<Channel> Mpims { get; set; }
        public List<Channel> Ims { get; set; }
        public List<Channel> Channels { get; set; }
        public List<Channel> Groups { get; set; }
    }
}