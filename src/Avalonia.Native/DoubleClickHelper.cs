using Avalonia.Input;
using Avalonia.Platform;

namespace Avalonia.Native
{
    internal class DoubleClickHelper
    {
        private int _clickCount;
        private Rect _lastClickRect;
        private ulong _lastClickTime;

        public bool IsDoubleClick(
            ulong timestamp,
            Point p)
        {
            var settings = AvaloniaLocator.Current.GetService<IPlatformSettings>();
            var doubleClickTime = settings?.GetDoubleTapTime(PointerType.Mouse).TotalMilliseconds ?? 500;
            var doubleClickSize = settings?.GetDoubleTapSize(PointerType.Mouse) ?? new Size(4, 4);

            if (!_lastClickRect.Contains(p) || timestamp - _lastClickTime > doubleClickTime)
            {
                _clickCount = 0;
            }

            ++_clickCount;
            _lastClickTime = timestamp;
            _lastClickRect = new Rect(p, new Size())
                .Inflate(new Thickness(doubleClickSize.Width / 2, doubleClickSize.Height / 2));

            return _clickCount == 2;
        }
    }
}
