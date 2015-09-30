using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using Perspex.Controls;
using Perspex.Input;
using Perspex.Input.Raw;
using Perspex.MobilePlatform.Fakes;
using Perspex.Platform;
using Perspex.Threading;

namespace Perspex.MobilePlatform
{
    abstract class MobileTopLevel : ITopLevelImpl
    {
        public IPlatformHandle Handle { get; }
        public VisualWrapper Visual { get; set; }
        public MobileTopLevel()
        {
            Handle = new FakePlatformHandle(this);
            Visual = new VisualWrapper(this);
        }
        
        public virtual void Dispose()
        {
        }

        public abstract Size ClientSize { get; set; }

        public string HandleDescriptor => "MobilePlatformVirtualHandle";
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
            Platform.Scene.RenderRequestedBy(this);
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
