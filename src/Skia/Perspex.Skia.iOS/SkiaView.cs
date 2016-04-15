using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using GLKit;
using ObjCRuntime;
using OpenGLES;
using Perspex.Platform;
using UIKit;

namespace Perspex.Skia.iOS
{
    public abstract class SkiaView : UIView //GLKView
    {
        //[DllImport("__Internal")]
        //static extern IntPtr GetPerspexEAGLContext();

        bool _drawQueued;
        //CADisplayLink _link;
        static EAGLContext GetContext()
        {
            /* No longer needed with SkiaSharp, but require a variant for HW accel
                //Ensure initialization
                MethodTable.Instance.SetOption((MethodTable.Option)0x10009999, IntPtr.Zero);
                var ctx = GetPerspexEAGLContext();
                var rv = Runtime.GetNSObject<EAGLContext>(ctx);
                rv.DangerousRetain();
                return rv;
            */
            return null;
        }


        protected SkiaView(Action<Action> registerFrame) : base(UIScreen.MainScreen.ApplicationFrame)	//, GetContext())
        {
            registerFrame(OnFrame);
        }

        protected SkiaView() : base(UIScreen.MainScreen.ApplicationFrame)	//, GetContext())
        {
            //(_link = CADisplayLink.Create(() => OnFrame())).AddToRunLoop(NSRunLoop.Main, NSRunLoop.NSDefaultRunLoopMode);
        }

        protected void OnFrame()
        {
            if (_drawQueued)
            {
                _drawQueued = false;

                // GLKView
                //Display();

                this.SetNeedsDisplay();
            }
        }

        protected void DrawOnNextFrame()
        {
            _drawQueued = true;
        }

        protected IPlatformHandle PerspexPlatformHandle { get; }
            = new PlatformHandle(IntPtr.Zero, "Null (iOS-specific)");


        protected abstract void Draw();

        public override void Draw(CGRect rect)
        {
            Draw();
        }
    }
}
