using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Foundation;
using UIKit;

namespace Avalonia.iOS
{
    class TouchHandler
    {
        private readonly AvaloniaView _view;
        private readonly ITopLevelImpl _tl;
        public TouchDevice _device = new TouchDevice();

        public TouchHandler(AvaloniaView view, ITopLevelImpl tl)
        {
            _view = view;
            _tl = tl;
        }

        static ulong Ts(UIEvent evt) => (ulong) (evt.Timestamp * 1000);
        private IInputRoot Root => _view.InputRoot;
        private static long _nextTouchPointId = 1;
        private Dictionary<UITouch, long> _knownTouches = new Dictionary<UITouch, long>();

        public void Handle(NSSet touches, UIEvent evt)
        {
            foreach (UITouch t in touches)
            {
                var pt = t.LocationInView(_view).ToAvalonia();
                if (!_knownTouches.TryGetValue(t, out var id))
                    _knownTouches[t] = id = _nextTouchPointId++;

                var ev = new RawTouchEventArgs(_device, Ts(evt), Root,
                    t.Phase switch
                    {
                        UITouchPhase.Began => RawPointerEventType.TouchBegin,
                        UITouchPhase.Ended => RawPointerEventType.TouchEnd,
                        UITouchPhase.Cancelled => RawPointerEventType.TouchCancel,
                        _ => RawPointerEventType.TouchUpdate
                    }, pt, RawInputModifiers.None, id);

                _tl.Input?.Invoke(ev);
                
                if (t.Phase == UITouchPhase.Cancelled || t.Phase == UITouchPhase.Ended)
                    _knownTouches.Remove(t);
            }
        }
        
    }
}
