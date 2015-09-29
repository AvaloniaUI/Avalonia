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
    class MobileTopLevel : ITopLevelImpl
    {
        public IPlatformHandle Handle { get; }
        public FakeRenderer Renderer { get; }
        public VisualWrapper Visual { get; }
        private InvalidationHelper _invalidator = new InvalidationHelper();
        public MobileTopLevel()
        {
            Handle = new FakePlatformHandle(this);
            Renderer = new FakeRenderer();
            Visual = new VisualWrapper(this);
            _invalidator.Invalidated += () => Paint?.Invoke(new Rect(ClientSize), Handle);
        }
        
        public virtual void Dispose()
        {
        }

        public Size ClientSize
        {
            get { return Platform.NativeWindowImpl.ClientSize; }
            set
            {
                Resized?.Invoke(ClientSize);
                Dispatcher.UIThread.InvokeAsync(() => Resized?.Invoke(ClientSize));
            }
        }

        public string HandleDescriptor => "MobilePlatformVirtualHandle";
        public Action Activated { get; set; }
        public Action Closed { get; set; }
        public Action Deactivated { get; set; }
        public Action<RawInputEventArgs> Input { get; set; }
        public Action<Rect, IPlatformHandle> Paint { get; set; }
        public Action<Size> Resized { get; set; }

        public void Activate()
        {
            //TODO
        }

        public void Invalidate(Rect rect)
        {
            _invalidator.Invalidate();
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            InputRoot = inputRoot;
        }

        public IInputRoot InputRoot { get; set; }

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
