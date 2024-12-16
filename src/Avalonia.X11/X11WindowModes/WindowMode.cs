using System;
using System.Collections.Generic;

namespace Avalonia.X11;

partial class X11Window
{
    public abstract class X11WindowMode
    {
        public X11Window Window { get; private set; }
        protected IntPtr Display;
        protected X11Info X11;
        protected AvaloniaX11Platform Platform;
        protected IntPtr Handle => Window._handle;
        protected IntPtr RenderHandle => Window._renderHandle;
        public virtual bool BlockInput => false;

        public void Init(X11Window window)
        {
            Platform = window._platform;
            Display = window._platform.Display;
            X11 = window._platform.Info;
            Window = window;
        }

        public virtual bool OnEvent(ref XEvent ev)
        {
            return false;
        }
        
        public virtual void Activate()
        {
            
        }

        public virtual void OnHandleCreated(IntPtr handle)
        {
        }

        public virtual void OnDestroyNotify()
        {
        }

        public virtual void AppendWmProtocols(List<IntPtr> data)
        {
        }

        public virtual void Show(bool activate, bool isDialog)
        {

        }

        public abstract PixelPoint PointToScreen(Point pt);
        public abstract Point PointToClient(PixelPoint pt);

        public virtual void Hide()
        {
        }
    }
}