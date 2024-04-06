using System.Runtime.InteropServices.JavaScript;
using Avalonia.Browser.Interop;
using Avalonia.Input;

namespace Avalonia.Browser
{
    internal class BrowserMouseDevice : MouseDevice
    {
        internal long PointerId { get; }

        public BrowserMouseDevice(long pointerId, JSObject container) : base( new BrowserMousePointer(pointerId, container))
        {
            PointerId = pointerId;
        }

        internal class BrowserMousePointer : Pointer
        {
            private readonly JSObject _container;
            private readonly long _pointerId;

            internal BrowserMousePointer(long pointerId, JSObject container) : base(GetNextFreeId(),PointerType.Mouse, true)
            {
                _pointerId = pointerId;
                _container = container;
            }
            
            protected override void PlatformCapture(IInputElement? element)
            {
                if (element is { })
                    InputHelper.SetPointerCapture(_container, _pointerId);
                else
                    InputHelper.ReleasePointerCapture(_container, _pointerId);
            }
        }
    }
}
