using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Lichess4545SlackNotifier.SlackApi
{
    public class Channel
    {
        [JsonProperty(PropertyName = "is_im")]
        public bool IsIm { get; set; }

        [JsonProperty(PropertyName = "is_mpim")]
        public bool IsMpim { get; set; }

        [JsonProperty(PropertyName = "is_channel")]
        public bool IsChannel { get; set; }

        [JsonProperty(PropertyName = "is_group")]
        public bool IsGroup { get; set; }

        [JsonProperty(PropertyName = "is_archived")]
        public bool IsArchived { get; set; }
        
        [JsonProperty(PropertyName = "unread_count_display")]
        public int UnreadCountDisplay { get; set; }

        public string Id { get; set; }

        public Message Latest { get; set; }

        public string User { get; set; }

        public List<string> Members { get; set; }

        public string Name { get; set; }
    }
}