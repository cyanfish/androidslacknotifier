using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
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
    }
}