using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using Org.Json;

namespace Lichess4545SlackNotifier
{
    public class JsonReader
    {
        public static JSONObject ReadJsonFromUrl(string url)
        {
            using (var client = new WebClient())
            {
                return new JSONObject(client.DownloadString(url));
            }
        }

        public static async Task<JSONObject> ReadJsonFromUrlAsync(string url)
        {
            using (var client = new WebClient())
            {
                return new JSONObject(await client.DownloadStringTaskAsync(url));
            }
        }

        public static async Task<T> ReadJsonFromUrlAsync<T>(string url)
        {
            using (var client = new WebClient())
            {
                return JsonConvert.DeserializeObject<T>(await client.DownloadStringTaskAsync(url));
            }
        }
    }
}