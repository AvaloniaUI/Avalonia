using Avalonia.Blazor.Interop;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Embedding;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SkiaSharp;

namespace Avalonia.Blazor
{
    public partial class AvaloniaView : ITextInputMethodImpl
    {
        private readonly RazorViewTopLevelImpl _topLevelImpl;
        private EmbeddableControlRoot _topLevel;

        // Interop
        private SKHtmlCanvasInterop _interop = null!;
        private SizeWatcherInterop _sizeWatcher = null!;
        private DpiWatcherInterop _dpiWatcher = null!;
        private SKHtmlCanvasInterop.GLInfo _jsGlInfo = null!;
        private ElementReference _htmlCanvas;
        private double _dpi;
        private SKSize _canvasSize;

        private GRContext? _context;
        private GRGlInterface? _glInterface;
        private const SKColorType ColorType = SKColorType.Rgba8888;

        private bool _initialised;

        [Inject] IJSRuntime Js { get; set; } = null!;

        public AvaloniaView()
        {
            _topLevelImpl = new RazorViewTopLevelImpl(this);

            _topLevel = new EmbeddableControlRoot(_topLevelImpl);

            if (Application.Current.ApplicationLifetime is ISingleViewApplicationLifetime lifetime)
            {
                _topLevel.Content = lifetime.MainView;
            };
        }

        void OnMouseMove(MouseEventArgs e)
        {
            _topLevelImpl.RawMouseEvent(RawPointerEventType.Move, new Point(e.ClientX, e.ClientY),
                RawInputModifiers.None);
        }

        void OnMouseUp(MouseEventArgs e)
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

        void OnMouseDown(MouseEventArgs e)
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

        void OnWheel(WheelEventArgs e)
        {
            _topLevelImpl.RawMouseWheelEvent(new Point(e.ClientX, e.ClientY),
                new Vector(-(e.DeltaX / 50), -(e.DeltaY / 50)), GetModifiers(e));
        }

        static RawInputModifiers GetModifiers(WheelEventArgs e)
        {
            RawInputModifiers modifiers = RawInputModifiers.None;

            if (e.CtrlKey) modifiers |= RawInputModifiers.Control;
            if (e.AltKey) modifiers |= RawInputModifiers.Alt;
            if (e.ShiftKey) modifiers |= RawInputModifiers.Shift;
            if (e.MetaKey) modifiers |= RawInputModifiers.Meta;

            return modifiers;
        }

        static RawInputModifiers GetModifiers(MouseEventArgs e)
        {
            RawInputModifiers modifiers = RawInputModifiers.None;

            if (e.CtrlKey) modifiers |= RawInputModifiers.Control;
            if (e.AltKey) modifiers |= RawInputModifiers.Alt;
            if (e.ShiftKey) modifiers |= RawInputModifiers.Shift;
            if (e.MetaKey) modifiers |= RawInputModifiers.Meta;

            return modifiers;
        }

        static RawInputModifiers GetModifiers(KeyboardEventArgs e)
        {
            RawInputModifiers modifiers = RawInputModifiers.None;

            if (e.CtrlKey) modifiers |= RawInputModifiers.Control;
            if (e.AltKey) modifiers |= RawInputModifiers.Alt;
            if (e.ShiftKey) modifiers |= RawInputModifiers.Shift;
            if (e.MetaKey) modifiers |= RawInputModifiers.Meta;

            return modifiers;
        }

        void OnKeyDown(KeyboardEventArgs e)
        {
            _topLevelImpl.RawKeyboardEvent(RawKeyEventType.KeyDown, e.Key, GetModifiers(e));
        }

        void OnKeyUp(KeyboardEventArgs e)
        {
            _topLevelImpl.RawKeyboardEvent(RawKeyEventType.KeyUp, e.Code, GetModifiers(e));
        }

        void OnInput(ChangeEventArgs e)
        {
            _topLevelImpl.RawTextEvent(e.Value.ToString());

            Js.InvokeVoidAsync("clearInput");
        }

        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }



        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                {
                    await Js.InvokeVoidAsync("hideInput");
                    await Js.InvokeVoidAsync("setCursor", "default");

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


                        // bump the default resource cache limit
                        _context.SetResourceCacheLimit(256 * 1024 * 1024);
                        Console.WriteLine("glcontext created and resource limit set");
                    }

                    _topLevelImpl.SetSurface(_context, _jsGlInfo, ColorType,
                        new PixelSize((int)_canvasSize.Width, (int)_canvasSize.Height), _dpi);

                    _initialised = true;

                    await Task.Delay(250); // without this we get some kind of initialisation error with gl
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
            Console.WriteLine($"focus input box. {active}");

            Js.InvokeVoidAsync("clearInput");

            if (active)
            {
                Js.InvokeVoidAsync("showInput");
                Js.InvokeVoidAsync("focusInput");
            }
            else
            {
                Js.InvokeVoidAsync("hideInput");
            }
        }

        public void SetCursorRect(Rect rect)
        {
            Console.WriteLine("SetCursorRect");
        }

        public void SetOptions(TextInputOptionsQueryEventArgs options)
        {
            Console.WriteLine("SetOptions");
        }

        public void Reset()
        {
            Console.WriteLine("reset");
            Js.InvokeVoidAsync("clearInput");
        }
    }
}
