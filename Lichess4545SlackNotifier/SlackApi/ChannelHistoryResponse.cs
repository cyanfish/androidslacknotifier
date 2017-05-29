using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Lichess4545SlackNotifier.SlackApi
{
    public class ChannelHistoryResponse
    {
        public bool Ok { get; set; }

        public string Latest { get; set; }

        public List<Message> Messages { get; set; }

        [JsonProperty(PropertyName = "has_more")]
        public bool HasMore { get; set; }
    }
}