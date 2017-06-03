using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Firebase.Iid;
using Firebase.Messaging;
using Exception = Java.Lang.Exception;

namespace Lichess4545SlackNotifier
{
    public class CloudMessaging
    {
        private readonly Prefs prefs;

        public CloudMessaging(Context context)
        {
            prefs = new Prefs(context);
        }

        public async void Register()
        {
            try
            {
                var url = string.Format(Constants.ServerUrlFormat, "register");
                if (prefs.Token != null)
                {
                    var args = new { slack_token = prefs.Token, reg_id = FirebaseInstanceId.Instance.Token };
                    string response = await JsonUtils.WriteJsonToUrlAsync(url, args);
                    Log.Debug("slackn", response);
                }
            }
            catch (Exception e)
            {
                Log.Error("slackn", e, "Error registering");
            }
            catch (System.Exception e)
            {
                Log.Error("slackn", "Error registering: " + e);
            }
        }

        public async void Unregister()
        {
            try
            {
                var url = string.Format(Constants.ServerUrlFormat, "unregister");
                var args = new { reg_id = FirebaseInstanceId.Instance.Token };
                string response = await JsonUtils.WriteJsonToUrlAsync(url, args);
                Log.Debug("slackn", response);
            }
            catch (Exception e)
            {
                Log.Error("slackn", e, "Error unregistering");
            }
            catch (System.Exception e)
            {
                Log.Error("slackn", "Error unregistering: " + e);
            }
        }

        public void UpdateTopicSubscriptions()
        {
            foreach (var sub in prefs.Subscriptions.Intersect(SubscriptionType.AllAnnounce))
            {
                FirebaseMessaging.Instance.SubscribeToTopic(sub.Id);
            }
            foreach (var sub in SubscriptionType.AllAnnounce.Except(prefs.Subscriptions))
            {
                FirebaseMessaging.Instance.UnsubscribeFromTopic(sub.Id);
            }
        }
    }
}