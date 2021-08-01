using System;
using Android.Views;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;

namespace Avalonia.Android.Platform.Specific.Helpers
{
    public class AndroidTouchEventsHelper<TView> : IDisposable where TView : ITopLevelImpl, IAndroidView
    {
        private TView _view;
        public bool HandleEvents { get; set; }

        public AndroidTouchEventsHelper(TView view, Func<IInputRoot> getInputRoot, Func<MotionEvent, int, Point> getPointfunc)
        {
            this._view = view;
            HandleEvents = true;
            _getPointFunc = getPointfunc;
            _getInputRoot = getInputRoot;
        }

        private TouchDevice _touchDevice = new TouchDevice();
        private Func<MotionEvent, int, Point> _getPointFunc;
        private Func<IInputRoot> _getInputRoot;

        public bool? DispatchTouchEvent(MotionEvent e, out bool callBase)
        {
            if (!HandleEvents)
            {
                callBase = true;
                return null;
            }

            var eventTime = DateTime.Now;

            //Basic touch support
            var pointerEventType = e.Action switch
            {
                MotionEventActions.Down => RawPointerEventType.TouchBegin,
                MotionEventActions.Up => RawPointerEventType.TouchEnd,
                MotionEventActions.Cancel => RawPointerEventType.TouchCancel,
                _ => RawPointerEventType.TouchUpdate
            };

            if (e.Action.HasFlag(MotionEventActions.PointerDown))
            {
                pointerEventType = RawPointerEventType.TouchBegin;
            }

            if (e.Action.HasFlag(MotionEventActions.PointerUp))
            {
                pointerEventType = RawPointerEventType.TouchEnd;
            }

            for (int i = 0; i < e.PointerCount; i++)
            {
                //if point is in view otherwise it's possible avalonia not to find the proper window to dispatch the event
                var point = _getPointFunc(e, i);

                double x = _view.View.GetX();
                double y = _view.View.GetY();
                double r = x + _view.View.Width;
                double b = y + _view.View.Height;

                if (x <= point.X && r >= point.X && y <= point.Y && b >= point.Y)
                {
                    var inputRoot = _getInputRoot();

                    var mouseEvent = new RawTouchEventArgs(_touchDevice, (uint)eventTime.Ticks, inputRoot,
                        i == e.ActionIndex ? pointerEventType : RawPointerEventType.TouchUpdate, point, RawInputModifiers.None, e.GetPointerId(i));
                    _view.Input(mouseEvent);
                }
            }

            callBase = true;
            //if return false events for move and up are not received!!!
            return e.Action != MotionEventActions.Up;
        }

        public void Dispose()
        {
            HandleEvents = false;
        }
    }
}
