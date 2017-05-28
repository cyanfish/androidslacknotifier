using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Ninject;
using Ninject.Activation;
using Ninject.Modules;
using Ninject.Parameters;
using Context = Android.Content.Context;

namespace Lichess4545SlackNotifier
{
    public class KernelManager
    {
        static KernelManager()
        {
            Kernel = new StandardKernel(new NinjectSettings { InjectNonPublic = true }, new Module());
        }

        public static IKernel Kernel { get; }

        public static void Inject(object target)
        {
            Inject(target, target as Context);
        }

        public static void Inject(object target, Context androidContext)
        {
            Kernel.Inject(target, new Parameter("androidContext", androidContext, true));
        }

        private class Module : NinjectModule
        {
            public override void Load()
            {
                Bind<Context>().ToMethod(GetAndroidContext);
                var viewTypes = typeof(View).Assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(View)));
                foreach (var t in viewTypes)
                {
                    Bind(t).ToMethod(GetAndroidView);
                }
            }

            private Context GetAndroidContext(IContext ctx)
            {
                if (ctx.Request.Target != null)
                {
                    return (Context)ctx.Request.Parameters.Where(x => x.Name == "androidContext").Select(x => x.GetValue(ctx, ctx.Request.Target)).FirstOrDefault();
                }
                return null;
            }

            private View GetAndroidView(IContext ctx)
            {
                var activity = GetAndroidContext(ctx) as Activity;
                if (ctx.Request.Target != null && activity != null)
                {
                    var attr = ctx.Request.Target.Member.GetCustomAttributes<InjectViewAttribute>().FirstOrDefault();
                    if (attr != null)
                    {
                        return activity.FindViewById(attr.ResourceId);
                    }
                }
                return null;
            }
        }
    }
}