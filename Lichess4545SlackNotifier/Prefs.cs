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
using Org.Json;

namespace Lichess4545SlackNotifier
{
    public class Prefs
    {
        private readonly Context context;

        public Prefs(Context context)
        {
            this.context = context;

            Source = context.GetSharedPreferences("prefs", FileCreationMode.Private);
        }

        public ISharedPreferences Source { get; }

        public long Interval
        {
            get => Source.GetLong("interval", TimeConstants.Hour);
            set => Source.Edit().PutLong("interval", value).Commit();
        }

        public JSONObject Auth
        {
            get => new JSONObject(Source.GetString("auth", "{\"ok\": false}"));
            set => Source.Edit().PutString("auth", value.ToString()).Commit();
        }

        public string Token
        {
            get => Source.GetString("token", null);
            set => Source.Edit().PutString("token", value).Commit();
        }
    }
}