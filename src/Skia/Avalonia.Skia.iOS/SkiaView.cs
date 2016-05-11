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
using Avalonia.Platform;
using UIKit;

namespace Avalonia.Skia.iOS
{
    // TODO: This implementation will be revised as part of HW acceleration work
    // and we may use the GLKView as a base for the implementation.
    //
    public abstract class SkiaView : UIView
    {
        bool _drawQueued;
        static EAGLContext GetContext()
        {
            return null;
        }


        protected SkiaView(Action<Action> registerFrame) : base(UIScreen.MainScreen.ApplicationFrame)
        {
            registerFrame(OnFrame);
        }

        protected SkiaView() : base(UIScreen.MainScreen.ApplicationFrame)
        {
        }

        protected void OnFrame()
        {
            if (_drawQueued)
            {
                _drawQueued = false;
                this.SetNeedsDisplay();
            }
        }

        protected void DrawOnNextFrame()
        {
            _drawQueued = true;
        }

        protected IPlatformHandle AvaloniaPlatformHandle { get; }
            = new PlatformHandle(IntPtr.Zero, "Null (iOS-specific)");


        protected abstract void Draw();

        public override void Draw(CGRect rect)
        {
            Draw();
        }
    }
}
