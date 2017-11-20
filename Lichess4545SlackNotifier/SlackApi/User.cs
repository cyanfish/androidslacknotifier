using Newtonsoft.Json;

namespace Lichess4545SlackNotifier.SlackApi
{
    public class User
    {
        public string Id { get; set; }
        
        public string Name { get; set; }

        public UserProfile Profile { get; set; }

        public class UserProfile
        {
            [JsonProperty(PropertyName = "display_name")]
            public string DisplayName { get; set; }
        }
    }
}