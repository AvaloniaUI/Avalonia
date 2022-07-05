using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Rendering;
using Avalonia.Web.Blazor.Interop;
using SkiaSharp;

#nullable enable

namespace Avalonia.Web.Blazor
{
    internal class RazorViewTopLevelImpl : ITopLevelImplWithTextInputMethod, ITopLevelImplWithNativeControlHost, ITopLevelImplWithStorageProvider
    {
        private Size _clientSize;
        private IBlazorSkiaSurface? _currentSurface;
        private IInputRoot? _inputRoot;
        private readonly Stopwatch _sw = Stopwatch.StartNew();
        private readonly AvaloniaView _avaloniaView;
        private readonly TouchDevice _touchDevice;
        private string _currentCursor = CssCursor.Default;

        public RazorViewTopLevelImpl(AvaloniaView avaloniaView)
        {
            _avaloniaView = avaloniaView;
            TransparencyLevel = WindowTransparencyLevel.None;
            AcrylicCompensationLevels = new AcrylicPlatformCompensationLevels(1, 1, 1);
            _touchDevice = new TouchDevice();
        }

        public ulong Timestamp => (ulong)_sw.ElapsedMilliseconds;


        internal void SetSurface(GRContext context, SKHtmlCanvasInterop.GLInfo glInfo, SKColorType colorType, PixelSize size, double scaling)
        {
            _currentSurface =
                new BlazorSkiaSurface(context, glInfo, colorType, size, scaling, GRSurfaceOrigin.BottomLeft);
        }

        internal void SetSurface(SKColorType colorType, PixelSize size, double scaling, Action<IntPtr, SKSizeI> blitCallback)
        {
            _currentSurface = new BlazorSkiaRasterSurface(colorType, size, scaling, blitCallback);
        }

        public void SetClientSize(SKSize size, double dpi)
        {
            var newSize = new Size(size.Width, size.Height);

            if (Math.Abs(RenderScaling - dpi) > 0.0001)
            {
                if (_currentSurface is { })
                {
                    _currentSurface.Scaling = dpi;
                }
                
                ScalingChanged?.Invoke(dpi);
            }

            if (newSize != _clientSize)
            {
                _clientSize = newSize;

                if (_currentSurface is { })
                {
                    _currentSurface.Size = new PixelSize((int)size.Width, (int)size.Height);
                }

                Resized?.Invoke(newSize, PlatformResizeReason.User);
            }
        }

        public void RawTouchEvent(RawPointerEventType type, Point p, RawInputModifiers modifiers, long touchPointId)
        {
            if (_inputRoot is { })
            {
                Input?.Invoke(new RawTouchEventArgs(_touchDevice, Timestamp, _inputRoot, type, p, modifiers, touchPointId));
            }
        }

        public void RawMouseEvent(RawPointerEventType type, Point p, RawInputModifiers modifiers)
        {
            if (_inputRoot is { })
            {
                Input?.Invoke(new RawPointerEventArgs(MouseDevice, Timestamp, _inputRoot, type, p, modifiers));
            }
        }

        public void RawMouseWheelEvent(Point p, Vector v, RawInputModifiers modifiers)
        {
            if (_inputRoot is { })
            {
                Input?.Invoke(new RawMouseWheelEventArgs(MouseDevice, Timestamp, _inputRoot, p, v, modifiers));
            }
        }

        public void RawKeyboardEvent(RawKeyEventType type, string code, string key, RawInputModifiers modifiers)
        {
            if (Keycodes.KeyCodes.TryGetValue(code, out var avkey))
            {
                if (_inputRoot is { })
                {
                    Input?.Invoke(new RawKeyEventArgs(KeyboardDevice, Timestamp, _inputRoot, type, avkey, modifiers));
                }
            }
            else if (Keycodes.KeyCodes.TryGetValue(key, out avkey))
            {
                if (_inputRoot is { })
                {
                    Input?.Invoke(new RawKeyEventArgs(KeyboardDevice, Timestamp, _inputRoot, type, avkey, modifiers));
                }
            }
        }

        public void RawTextEvent(string text)
        {
            if (_inputRoot is { })
            {
                Input?.Invoke(new RawTextInputEventArgs(KeyboardDevice, Timestamp, _inputRoot, text));
            }
        }

        public void Dispose()
        {

        }

        public IRenderer CreateRenderer(IRenderRoot root)
        {
            var loop = AvaloniaLocator.Current.GetRequiredService<IRenderLoop>();
            return new DeferredRenderer(root, loop);
        }

        public void Invalidate(Rect rect)
        {
            //Console.WriteLine("invalidate rect called");
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            _inputRoot = inputRoot;
        }

        public Point PointToClient(PixelPoint point) => new Point(point.X, point.Y);

        public PixelPoint PointToScreen(Point point) => new PixelPoint((int)point.X, (int)point.Y);

        public void SetCursor(ICursorImpl? cursor)
        {
            var val = (cursor as CssCursor)?.Value ?? CssCursor.Default;
            if (_currentCursor != val)
            {
                SetCssCursor?.Invoke(val);
                _currentCursor = val;
            }
        }

        public IPopupImpl? CreatePopup()
        {
            return null;
        }

        public void SetTransparencyLevelHint(WindowTransparencyLevel transparencyLevel)
        {

        }

        public Size ClientSize => _clientSize;
        public Size? FrameSize => null;
        public double RenderScaling => _currentSurface?.Scaling ?? 1;

        public IEnumerable<object> Surfaces => new object[] { _currentSurface! };

        public Action<string>? SetCssCursor { get; set; }
        public Action<RawInputEventArgs>? Input { get; set; }
        public Action<Rect>? Paint { get; set; }
        public Action<Size, PlatformResizeReason>? Resized { get; set; }
        public Action<double>? ScalingChanged { get; set; }
        public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }
        public Action? Closed { get; set; }
        public Action? LostFocus { get; set; }
        public IMouseDevice MouseDevice { get; } = new MouseDevice();

        public IKeyboardDevice KeyboardDevice { get; } = BlazorWindowingPlatform.Keyboard;
        public WindowTransparencyLevel TransparencyLevel { get; }
        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; }

        public ITextInputMethodImpl TextInputMethod => _avaloniaView;

        public INativeControlHostImpl? NativeControlHost => _avaloniaView.GetNativeControlHostImpl();
        public IStorageProvider StorageProvider => _avaloniaView.GetStorageProvider();
    }
}
