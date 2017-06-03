using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Lichess4545SlackNotifier
{
    public class SubscriptionsDialogFragment : DialogFragment
    {
        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            var prefs = new Prefs(Context);

            var selectedItems = prefs.Subscriptions.ToList();
            AlertDialog.Builder builder = new AlertDialog.Builder(Activity);
            builder.SetTitle("Subscriptions")
                .SetMultiChoiceItems(SubscriptionType.All.Select(x => x.Name).ToArray(), SubscriptionType.All.Select(x => selectedItems.Any(y => y.Id == x.Id)).ToArray(),
                (sender, args) =>
                    {
                        if (args.IsChecked)
                        {
                            selectedItems.Add(SubscriptionType.All[args.Which]);
                        }
                        else
                        {
                            selectedItems.Remove(SubscriptionType.All[args.Which]);
                        }
                    }
            )
            .SetPositiveButton(Android.Resource.String.Ok, (sender, args) =>
                {
                    prefs.Subscriptions = selectedItems;
                    new CloudMessaging(Context).UpdateTopicSubscriptions();
                    ((MainActivity) Activity).PollForMessages();
                })
            .SetNegativeButton(Android.Resource.String.Cancel, (sender, args) => { });

            return builder.Create();
        }
    }
}