using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Rendering;
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
        private SKHtmlCanvasInterop? _interop = null;
        private SizeWatcherInterop? _sizeWatcher = null;
        private DpiWatcherInterop? _dpiWatcher = null;
        private SKHtmlCanvasInterop.GLInfo? _jsGlInfo = null;
        private InputHelperInterop? _inputHelper = null;
        private InputHelperInterop? _canvasHelper = null;
        private NativeControlHostInterop? _nativeControlHost = null;
        private ElementReference _htmlCanvas;
        private ElementReference _inputElement;
        private ElementReference _nativeControlsContainer;
        private double _dpi = 1;
        private SKSize _canvasSize = new (100, 100);

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

        internal INativeControlHostImpl GetNativeControlHostImpl()
        {
            return _nativeControlHost ?? throw new InvalidOperationException("Blazor View wasn't initialized yet");
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

        private void OnPointerMove(Microsoft.AspNetCore.Components.Web.PointerEventArgs e)
        {
            if (e.PointerType != "touch")
            {
                _topLevelImpl.RawMouseEvent(RawPointerEventType.Move, new Point(e.ClientX, e.ClientY), GetModifiers(e));
            }
        }

        private void OnPointerUp(Microsoft.AspNetCore.Components.Web.PointerEventArgs e)
        {
            if (e.PointerType == "touch")
            {
                _topLevelImpl.RawTouchEvent(RawPointerEventType.TouchEnd, new Point(e.ClientX, e.ClientY),
                    GetModifiers(e), e.PointerId);
            }
            else
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
        }

        private void OnPointerDown(Microsoft.AspNetCore.Components.Web.PointerEventArgs e)
        {
            if (e.PointerType == "touch")
            {
                _topLevelImpl.RawTouchEvent(RawPointerEventType.TouchBegin, new Point(e.ClientX, e.ClientY),
                    GetModifiers(e), e.PointerId);
            }
            else
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

        private static RawInputModifiers GetModifiers(Microsoft.AspNetCore.Components.Web.PointerEventArgs e)
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
            _topLevelImpl.RawKeyboardEvent(RawKeyEventType.KeyDown, e.Code, e.Key, GetModifiers(e));
        }

        private void OnKeyUp(KeyboardEventArgs e)
        {
            _topLevelImpl.RawKeyboardEvent(RawKeyEventType.KeyUp, e.Code, e.Key, GetModifiers(e));
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

            _inputHelper?.Clear();
        }

        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                AvaloniaLocator.CurrentMutable.Bind<IJSInProcessRuntime>().ToConstant((IJSInProcessRuntime)Js);

                _inputHelper = await InputHelperInterop.ImportAsync(Js, _inputElement);
                _canvasHelper = await InputHelperInterop.ImportAsync(Js, _htmlCanvas);

                _inputHelper.Hide();
                _canvasHelper.SetCursor("default");
                _topLevelImpl.SetCssCursor = x =>
                {
                    _inputHelper.SetCursor(x); //macOS
                    _canvasHelper.SetCursor(x); //windows
                };

                _nativeControlHost = await NativeControlHostInterop.ImportAsync(Js, _nativeControlsContainer);

                Console.WriteLine("starting html canvas setup");
                _interop = await SKHtmlCanvasInterop.ImportAsync(Js, _htmlCanvas, OnRenderFrame);

                Console.WriteLine("Interop created");
                _jsGlInfo = _interop.InitGL();

                Console.WriteLine("jsglinfo created - init gl");

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
                
                _interop.SetCanvasSize((int)(_canvasSize.Width * _dpi), (int)(_canvasSize.Height * _dpi));

                _initialised = true;

                Threading.Dispatcher.UIThread.Post(async () =>
                {
                    _interop.RequestAnimationFrame(true);
                    
                    _sizeWatcher = await SizeWatcherInterop.ImportAsync(Js, _htmlCanvas, OnSizeChanged);
                    _dpiWatcher = await DpiWatcherInterop.ImportAsync(Js, OnDpiChanged);
                    
                    _topLevel.Prepare();

                    _topLevel.Renderer.Start();
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
            _dpiWatcher?.Unsubscribe(OnDpiChanged);
            _sizeWatcher?.Dispose();
            _interop?.Dispose();
        }

        private void ForceBlit()
        {
            // Note: this is technically a hack, but it's a kinda unique use case when
            // we want to blit the previous frame
            // renderer doesn't have much control over the render target
            // we render on the UI thread
            // We also don't want to have it as a meaningful public API.
            // Therefore we have InternalsVisibleTo hack here.

            if (_topLevel.Renderer is DeferredRenderer dr)
            {
                dr.Render(true);
            }
        }

        private void OnDpiChanged(double newDpi)
        {
            if (Math.Abs(_dpi - newDpi) > 0.0001)
            {
                _dpi = newDpi;

                _interop!.SetCanvasSize((int)(_canvasSize.Width * _dpi), (int)(_canvasSize.Height * _dpi));

                _topLevelImpl.SetClientSize(_canvasSize, _dpi);

                ForceBlit();
            }
        }

        private void OnSizeChanged(SKSize newSize)
        {
            if (_canvasSize != newSize)
            {
                _canvasSize = newSize;

                _interop!.SetCanvasSize((int)(_canvasSize.Width * _dpi), (int)(_canvasSize.Height * _dpi));

                _topLevelImpl.SetClientSize(_canvasSize, _dpi);

                ForceBlit();
            }
        }

        public void SetClient(ITextInputMethodClient? client)
        {
            if (_inputHelper is null)
            {
                return;
            }

            _inputHelper.Clear();

            var active = client is { };
            
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

        public void SetOptions(TextInputOptions options)
        {
        }

        public void Reset()
        {
            _inputHelper?.Clear();
        }
    }
}
