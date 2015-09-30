using System;
using Perspex.Controls;
using Perspex.Input;
using Perspex.Input.Raw;
using Perspex.Platform;

namespace Perspex.TinyWM
{
    abstract class TopLevelImpl : ITopLevelImpl, IPlatformHandle
    {
        public virtual void Dispose()
        {
        }

        public virtual Rect Bounds => new Rect(ClientSize);

        public abstract Size ClientSize { get; set; }

        IPlatformHandle ITopLevelImpl.Handle => this;
        IntPtr IPlatformHandle.Handle => IntPtr.Zero;
        public string HandleDescriptor => "TinyWMVirtualHandle";

        public Action Activated { get; set; }
        public Action Closed { get; set; }
        public Action Deactivated { get; set; }
        public Action<RawInputEventArgs> Input { get; set; }
        public Action<Rect> Paint { get; set; }
        public Action<Size> Resized { get; set; }

        public void Activate()
        {
            //TODO
        }

        public void Invalidate(Rect rect)
        {
            WindowManager.Scene.RenderRequestedBy(this);
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            InputRoot = inputRoot;
        }

        public IInputRoot InputRoot { get; set; }
        public TopLevel TopLevel { get; set; }

        public Point PointToScreen(Point point)
        {
            //TODO: implement it for popups
            return point;
        }

        public void SetCursor(IPlatformHandle cursor)
        {
        }
        

    }
}
