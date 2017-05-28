using Newtonsoft.Json;

namespace Lichess4545SlackNotifier.SlackApi
{
    public class TokenResponse
    {
        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { get; set; }
        
        public bool Ok { get; set; }
    }
}