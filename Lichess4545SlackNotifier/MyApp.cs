﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Firebase;

namespace Lichess4545SlackNotifier
{
#if DEBUG
    [Application(Debuggable = true)]
#else
    [Application(Debuggable = false)]
#endif
    public class MyApp : Application
    {
        public MyApp(IntPtr handle, JniHandleOwnership transfer)
            : base(handle,transfer)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();
            FirebaseApp.InitializeApp(this);
        }
    }
}