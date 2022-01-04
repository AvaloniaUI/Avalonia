using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Embedding;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Web.Blazor.Interop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SkiaSharp;

namespace Avalonia.Web.Blazor
{
    public partial class AvaloniaView : ITextInputMethodImpl
    {
        private readonly RazorViewTopLevelImpl _topLevelImpl;
        private EmbeddableControlRoot _topLevel;

        // Interop
        private SKHtmlCanvasInterop _interop = null!;
        private SizeWatcherInterop _sizeWatcher = null!;
        private DpiWatcherInterop _dpiWatcher = null!;
        private SKHtmlCanvasInterop.GLInfo? _jsGlInfo = null!;
        private InputHelperInterop _inputHelper = null!;
        private InputHelperInterop _canvasHelper = null!;
        private ElementReference _htmlCanvas;
        private ElementReference _inputElement;
        private double _dpi;
        private SKSize _canvasSize;

        private GRContext? _context;
        private GRGlInterface? _glInterface;
        private const SKColorType ColorType = SKColorType.Rgba8888;

        private bool _initialised;

        [Inject] private IJSRuntime Js { get; set; } = null!;

        public AvaloniaView()
        {
            _topLevelImpl = new RazorViewTopLevelImpl(this);

            _topLevel = new EmbeddableControlRoot(_topLevelImpl);

            if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime lifetime)
            {
                _topLevel.Content = lifetime.MainView;
            }
        }

        private void OnTouchStart(TouchEventArgs e)
        {
            foreach (var touch in e.ChangedTouches)
            {
                _topLevelImpl.RawTouchEvent(RawPointerEventType.TouchBegin, new Point(touch.ClientX, touch.ClientY),
                    GetModifiers(e), touch.Identifier);
            }
        }

        private void OnTouchEnd(TouchEventArgs e)
        {
            foreach (var touch in e.ChangedTouches)
            {
                _topLevelImpl.RawTouchEvent(RawPointerEventType.TouchEnd, new Point(touch.ClientX, touch.ClientY),
                    GetModifiers(e), touch.Identifier);
            }
        }

        private void OnTouchCancel(TouchEventArgs e)
        {
            foreach (var touch in e.ChangedTouches)
            {
                _topLevelImpl.RawTouchEvent(RawPointerEventType.TouchCancel, new Point(touch.ClientX, touch.ClientY),
                    GetModifiers(e), touch.Identifier);
            }
        }

        private void OnTouchMove(TouchEventArgs e)
        {
            foreach (var touch in e.ChangedTouches)
            {
                _topLevelImpl.RawTouchEvent(RawPointerEventType.TouchUpdate, new Point(touch.ClientX, touch.ClientY),
                    GetModifiers(e), touch.Identifier);
            }
        }

        private void OnMouseMove(MouseEventArgs e)
        {
            _topLevelImpl.RawMouseEvent(RawPointerEventType.Move, new Point(e.ClientX, e.ClientY), GetModifiers(e));
        }

        private void OnMouseUp(MouseEventArgs e)
        {
            RawPointerEventType type = default;

            switch (e.Button)
            {
                case 0:
                    type = RawPointerEventType.LeftButtonUp;
                    break;

                case 1:
                    type = RawPointerEventType.MiddleButtonUp;
                    break;

                case 2:
                    type = RawPointerEventType.RightButtonUp;
                    break;
            }

            _topLevelImpl.RawMouseEvent(type, new Point(e.ClientX, e.ClientY), GetModifiers(e));
        }

        private void OnMouseDown(MouseEventArgs e)
        {
            RawPointerEventType type = default;

            switch (e.Button)
            {
                case 0:
                    type = RawPointerEventType.LeftButtonDown;
                    break;

                case 1:
                    type = RawPointerEventType.MiddleButtonDown;
                    break;

                case 2:
                    type = RawPointerEventType.RightButtonDown;
                    break;
            }

            _topLevelImpl.RawMouseEvent(type, new Point(e.ClientX, e.ClientY), GetModifiers(e));
        }

        private void OnWheel(WheelEventArgs e)
        {
            _topLevelImpl.RawMouseWheelEvent(new Point(e.ClientX, e.ClientY),
                new Vector(-(e.DeltaX / 50), -(e.DeltaY / 50)), GetModifiers(e));
        }

        private static RawInputModifiers GetModifiers(WheelEventArgs e)
        {
            var modifiers = RawInputModifiers.None;

            if (e.CtrlKey)
                modifiers |= RawInputModifiers.Control;
            if (e.AltKey)
                modifiers |= RawInputModifiers.Alt;
            if (e.ShiftKey)
                modifiers |= RawInputModifiers.Shift;
            if (e.MetaKey)
                modifiers |= RawInputModifiers.Meta;

            if ((e.Buttons & 1L) == 1)
                modifiers |= RawInputModifiers.LeftMouseButton;

            if ((e.Buttons & 2L) == 2)
                modifiers |= RawInputModifiers.RightMouseButton;

            if ((e.Buttons & 4L) == 4)
                modifiers |= RawInputModifiers.MiddleMouseButton;

            return modifiers;
        }

        private static RawInputModifiers GetModifiers(TouchEventArgs e)
        {
            var modifiers = RawInputModifiers.None;

            if (e.CtrlKey)
                modifiers |= RawInputModifiers.Control;
            if (e.AltKey)
                modifiers |= RawInputModifiers.Alt;
            if (e.ShiftKey)
                modifiers |= RawInputModifiers.Shift;
            if (e.MetaKey)
                modifiers |= RawInputModifiers.Meta;

            return modifiers;
        }

        private static RawInputModifiers GetModifiers(MouseEventArgs e)
        {
            var modifiers = RawInputModifiers.None;

            if (e.CtrlKey)
                modifiers |= RawInputModifiers.Control;
            if (e.AltKey)
                modifiers |= RawInputModifiers.Alt;
            if (e.ShiftKey)
                modifiers |= RawInputModifiers.Shift;
            if (e.MetaKey)
                modifiers |= RawInputModifiers.Meta;

            if ((e.Buttons & 1L) == 1)
                modifiers |= RawInputModifiers.LeftMouseButton;

            if ((e.Buttons & 2L) == 2)
                modifiers |= RawInputModifiers.RightMouseButton;

            if ((e.Buttons & 4L) == 4)
                modifiers |= RawInputModifiers.MiddleMouseButton;

            return modifiers;
        }

        private static RawInputModifiers GetModifiers(KeyboardEventArgs e)
        {
            var modifiers = RawInputModifiers.None;

            if (e.CtrlKey)
                modifiers |= RawInputModifiers.Control;
            if (e.AltKey)
                modifiers |= RawInputModifiers.Alt;
            if (e.ShiftKey)
                modifiers |= RawInputModifiers.Shift;
            if (e.MetaKey)
                modifiers |= RawInputModifiers.Meta;

            return modifiers;
        }

        private void OnKeyDown(KeyboardEventArgs e)
        {
            _topLevelImpl.RawKeyboardEvent(RawKeyEventType.KeyDown, e.Key, GetModifiers(e));
        }

        private void OnKeyUp(KeyboardEventArgs e)
        {
            _topLevelImpl.RawKeyboardEvent(RawKeyEventType.KeyUp, e.Code, GetModifiers(e));
        }

        private void OnInput(ChangeEventArgs e)
        {
            if (e.Value != null)
            {
                var inputData = e.Value.ToString();
                if (inputData != null)
                {
                    _topLevelImpl.RawTextEvent(inputData);
                }
            }

            _inputHelper.Clear();
        }

        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                Threading.Dispatcher.UIThread.Post(async () =>
                {
                    _inputHelper = await InputHelperInterop.ImportAsync(Js, _inputElement);
                    _canvasHelper = await InputHelperInterop.ImportAsync(Js, _htmlCanvas);

                    _inputHelper.Hide();
                    _canvasHelper.SetCursor("default");
                    _topLevelImpl.SetCssCursor = x =>
                    {
                        _inputHelper.SetCursor(x);//macOS
                        _canvasHelper.SetCursor(x);//windows
                    };

                    Console.WriteLine("starting html canvas setup");
                    _interop = await SKHtmlCanvasInterop.ImportAsync(Js, _htmlCanvas, OnRenderFrame);

                    Console.WriteLine("Interop created");
                    _jsGlInfo = _interop.InitGL();

                    Console.WriteLine("jsglinfo created - init gl");

                    _sizeWatcher = await SizeWatcherInterop.ImportAsync(Js, _htmlCanvas, OnSizeChanged);
                    _dpiWatcher = await DpiWatcherInterop.ImportAsync(Js, OnDpiChanged);

                    Console.WriteLine("watchers created.");

                    // create the SkiaSharp context
                    if (_context == null)
                    {
                        Console.WriteLine("create glcontext");
                        _glInterface = GRGlInterface.Create();
                        _context = GRContext.CreateGl(_glInterface);

                        var options = AvaloniaLocator.Current.GetService<SkiaOptions>();
                        // bump the default resource cache limit
                        _context.SetResourceCacheLimit(options?.MaxGpuResourceSizeBytes ?? 32 * 1024 * 1024);
                        Console.WriteLine("glcontext created and resource limit set");
                    }

                    _topLevelImpl.SetSurface(_context, _jsGlInfo, ColorType,
                        new PixelSize((int)_canvasSize.Width, (int)_canvasSize.Height), _dpi);

                    _initialised = true;

                    _topLevel.Prepare();

                    _topLevel.Renderer.Start();
                    Invalidate();
                });
            }
        }

        private void OnRenderFrame()
        {
            if (_canvasSize.Width <= 0 || _canvasSize.Height <= 0 || _dpi <= 0 || _jsGlInfo == null)
            {
                Console.WriteLine("nothing to render");
                return;
            }

            ManualTriggerRenderTimer.Instance.RaiseTick();
        }

        public void Dispose()
        {
            _dpiWatcher.Unsubscribe(OnDpiChanged);
            _sizeWatcher.Dispose();
            _interop.Dispose();
        }

        private void OnDpiChanged(double newDpi)
        {
            _dpi = newDpi;

            _topLevelImpl.SetClientSize(_canvasSize, _dpi);

            Invalidate();
        }

        private void OnSizeChanged(SKSize newSize)
        {
            _canvasSize = newSize;

            _topLevelImpl.SetClientSize(_canvasSize, _dpi);

            Invalidate();
        }

        public void Invalidate()
        {
            if (!_initialised || _canvasSize.Width <= 0 || _canvasSize.Height <= 0 || _dpi <= 0 || _jsGlInfo == null)
            {
                Console.WriteLine("invalidate ignored");
                return;
            }

            _interop.RequestAnimationFrame(true, (int)(_canvasSize.Width * _dpi), (int)(_canvasSize.Height * _dpi));
        }

        public void SetActive(bool active)
        {
            _inputHelper.Clear();

            if (active)
            {
                _inputHelper.Show();
                _inputHelper.Focus();
            }
            else
            {
                _inputHelper.Hide();
            }
        }

        public void SetCursorRect(Rect rect)
        {
        }

        public void SetOptions(TextInputOptionsQueryEventArgs options)
        {
        }

        public void Reset()
        {
            _inputHelper.Clear();
        }
    }
}
