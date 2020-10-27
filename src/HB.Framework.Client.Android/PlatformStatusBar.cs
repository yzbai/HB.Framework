﻿using System;
using Android.Views;
using HB.Framework.Client.Android;
using HB.Framework.Client.UI.Platform;
using Xamarin.Essentials;
using Xamarin.Forms;

[assembly: Dependency(typeof(PlatformStatusBar))]
namespace HB.Framework.Client.Android
{
    public class PlatformStatusBar : IStatusBar
    {
        WindowManagerFlags _orginalFlags;

        public void Show()
        {
            var attrs = Platform.CurrentActivity.Window.Attributes;

            attrs.Flags = _orginalFlags;

            Platform.CurrentActivity.Window.Attributes = attrs;
        }

        public void Hide()
        {
            WindowManagerLayoutParams attrs = Platform.CurrentActivity.Window.Attributes;

            _orginalFlags = attrs.Flags;

            attrs.Flags |= WindowManagerFlags.Fullscreen;

            Platform.CurrentActivity.Window.Attributes = attrs;
        }
    }
}