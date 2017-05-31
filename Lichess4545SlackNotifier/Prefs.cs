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
using Lichess4545SlackNotifier.SlackApi;
using Newtonsoft.Json;
using Org.Json;

namespace Lichess4545SlackNotifier
{
    public class Prefs
    {
        public Prefs(Context context)
        {
            Source = context.GetSharedPreferences("prefs", FileCreationMode.Private);
        }

        public ISharedPreferences Source { get; }

        public long Interval
        {
            get => Source.GetLong("interval", TimeConstants.Hour);
            set => Source.Edit().PutLong("interval", value).Commit();
        }

        public AuthResponse Auth
        {
            get => JsonConvert.DeserializeObject<AuthResponse>(Source.GetString("auth", "{}")) ?? new AuthResponse();
            set => Source.Edit().PutString("auth", JsonConvert.SerializeObject(value)).Commit();
        }

        public string Token
        {
            get => Source.GetString("token", null);
            set => Source.Edit().PutString("token", value).Commit();
        }

        public long LastDismissedTs
        {
            get => Source.GetLong("LastDismissedTs", -1);
            set => Source.Edit().PutLong("LastDismissedTs", value).Commit();
        }

        public IEnumerable<SubscriptionType> Subscriptions
        {
            get => Source.GetStringSet("Subscriptions", SubscriptionType.All.Select(x => x.Id).ToList()).Select(SubscriptionType.FromId);
            set => Source.Edit().PutStringSet("Subscriptions", value.Select(x => x.Id).ToList()).Commit();
        }

        public List<UnreadChannel> LatestUnreads
        {
            get => JsonConvert.DeserializeObject<List<UnreadChannel>>(Source.GetString("LatestUnreads", "[]")) ?? new List<UnreadChannel>();
            set => Source.Edit().PutString("LatestUnreads", JsonConvert.SerializeObject(value)).Commit();
        }

        public Dictionary<string, string> LatestUserMap
        {
            get => JsonConvert.DeserializeObject<Dictionary<string, string>>(Source.GetString("LatestUserMap", "{}")) ?? new Dictionary<string, string>();
            set => Source.Edit().PutString("LatestUserMap", JsonConvert.SerializeObject(value)).Commit();
        }
    }
}