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
using Ninject;

namespace Lichess4545SlackNotifier
{
    [AttributeUsage(AttributeTargets.Property)]
    public class InjectViewAttribute : InjectAttribute
    {
        public InjectViewAttribute(int resId)
        {
            ResourceId = resId;
        }

        public int ResourceId { get; }
    }
}