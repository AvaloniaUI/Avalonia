using System;
using System.IO;
using Android.AccessibilityServices;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using Java.Lang;
using Javax.Security.Auth;
using Perspex.Input;
using Perspex.Input.Raw;
using Perspex.Platform;
using ARect = Android.Graphics.Rect;
using Exception = System.Exception;

namespace Perspex.Android.Rendering
{
    public class PerspexView : View
    {
        public PerspexView(Context context) : base(context)
        {
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);
        }

        public new IPlatformHandle Handle
        {
            get { return new PlatformHandle(IntPtr.Zero, "View"); }
            private set { }
        }
    }
}