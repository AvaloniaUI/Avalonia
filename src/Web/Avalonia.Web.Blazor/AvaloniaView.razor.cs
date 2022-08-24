using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Platform.Storage;
using Avalonia.Rendering;
using Avalonia.Web.Blazor.Interop;
using Avalonia.Web.Blazor.Interop.Storage;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

using SkiaSharp;
using HTMLPointerEventArgs = Microsoft.AspNetCore.Components.Web.PointerEventArgs;

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
        private AvaloniaModule? _avaloniaModule = null;
        private InputHelperInterop? _inputHelper = null;
        private InputHelperInterop? _canvasHelper = null;
        private NativeControlHostInterop? _nativeControlHost = null;
        private StorageProviderInterop? _storageProvider = null;
        private ElementReference _htmlCanvas;
        private ElementReference _inputElement;
        private ElementReference _nativeControlsContainer;
        private double _dpi = 1;
        private SKSize _canvasSize = new (100, 100);

        private GRContext? _context;
        private GRGlInterface? _glInterface;
        private const SKColorType ColorType = SKColorType.Rgba8888;

        private bool _initialised;
        private bool _useGL;
        private bool _inputElementFocused;

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

        internal IStorageProvider GetStorageProvider()
        {
            return _storageProvider ?? throw new InvalidOperationException("Blazor View wasn't initialized yet");
        }

        private void OnPointerCancel(HTMLPointerEventArgs e)
        {
            if (e.PointerType == "touch")
            {
                _topLevelImpl.RawPointerEvent(RawPointerEventType.TouchCancel, e.PointerType, GetPointFromEventArgs(e),
                    GetModifiers(e), e.PointerId);
            }
        }

        private void OnPointerMove(HTMLPointerEventArgs e)
        {
            var type = e.PointerType switch
            {
                "touch" => RawPointerEventType.TouchUpdate,
                _ => RawPointerEventType.Move
            };

            _topLevelImpl.RawPointerEvent(type, e.PointerType, GetPointFromEventArgs(e), GetModifiers(e), e.PointerId);
        }

        private void OnPointerUp(HTMLPointerEventArgs e)
        {
            var type = e.PointerType switch
            {
                "touch" => RawPointerEventType.TouchEnd,
                _ => e.Button switch
                {
                    0 => RawPointerEventType.LeftButtonUp,
                    1 => RawPointerEventType.MiddleButtonUp,
                    2 => RawPointerEventType.RightButtonUp,
                    3 => RawPointerEventType.XButton1Up,
                    4 => RawPointerEventType.XButton2Up,
                    // 5 => Pen eraser button,
                    _ => RawPointerEventType.Move
                }
            };

            _topLevelImpl.RawPointerEvent(type, e.PointerType, GetPointFromEventArgs(e), GetModifiers(e), e.PointerId);
        }

        private void OnPointerDown(HTMLPointerEventArgs e)
        {
            var type = e.PointerType switch
            {
                "touch" => RawPointerEventType.TouchBegin,
                _ => e.Button switch
                {
                    0 => RawPointerEventType.LeftButtonDown,
                    1 => RawPointerEventType.MiddleButtonDown,
                    2 => RawPointerEventType.RightButtonDown,
                    3 => RawPointerEventType.XButton1Down,
                    4 => RawPointerEventType.XButton2Down,
                    // 5 => Pen eraser button,
                    _ => RawPointerEventType.Move
                }
            };

            _topLevelImpl.RawPointerEvent(type, e.PointerType, GetPointFromEventArgs(e), GetModifiers(e), e.PointerId);
        }

        private static RawPointerPoint GetPointFromEventArgs(HTMLPointerEventArgs args)
        {
            return new RawPointerPoint
            {
                Position = new Point(args.ClientX, args.ClientY),
                Pressure = args.Pressure,
                XTilt = args.TiltX,
                YTilt = args.TiltY
                // Twist = args.Twist - read from JS code directly when
            };
        }

        private void OnWheel(WheelEventArgs e)
        {
            _topLevelImpl.RawMouseWheelEvent( new Point(e.ClientX, e.ClientY),
                new Vector(-(e.DeltaX / 50), -(e.DeltaY / 50)), GetModifiers(e));
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
                modifiers |= e.Type == "pen" ? RawInputModifiers.PenBarrelButton : RawInputModifiers.RightMouseButton;

            if ((e.Buttons & 4L) == 4)
                modifiers |= RawInputModifiers.MiddleMouseButton;
            
            if ((e.Buttons & 8L) == 8)
                modifiers |= RawInputModifiers.XButton1MouseButton;
            
            if ((e.Buttons & 16L) == 16)
                modifiers |= RawInputModifiers.XButton2MouseButton;
            
            if ((e.Buttons & 32L) == 32)
                modifiers |= RawInputModifiers.PenEraser;

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

        private void OnFocus(FocusEventArgs e)
        {
            // if focus has unexpectedly moved from the input element to the container element,
            // shift it back to the input element
            if (_inputElementFocused && _inputHelper is not null)
            {
                _inputHelper.Focus();
            }
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

                _avaloniaModule = await AvaloniaModule.ImportAsync(Js);

                _inputHelper = new InputHelperInterop(_avaloniaModule, _inputElement);
                _canvasHelper = new InputHelperInterop(_avaloniaModule, _htmlCanvas);

                _inputHelper.Hide();
                _canvasHelper.SetCursor("default");
                _topLevelImpl.SetCssCursor = x =>
                {
                    _inputHelper.SetCursor(x); //macOS
                    _canvasHelper.SetCursor(x); //windows
                };

                _nativeControlHost = new NativeControlHostInterop(_avaloniaModule, _nativeControlsContainer);
                _storageProvider = await StorageProviderInterop.ImportAsync(Js);
                
                Console.WriteLine("starting html canvas setup");
                _interop = new SKHtmlCanvasInterop(_avaloniaModule, _htmlCanvas, OnRenderFrame);

                Console.WriteLine("Interop created");
                
                var skiaOptions = AvaloniaLocator.Current.GetService<SkiaOptions>();
                _useGL = skiaOptions?.CustomGpuFactory != null;

                if (_useGL)
                {
                    _jsGlInfo = _interop.InitGL();
                    Console.WriteLine("jsglinfo created - init gl");
                }
                else
                {
                    var rasterInitialized = _interop.InitRaster();
                    Console.WriteLine("raster initialized: {0}", rasterInitialized);
                }

                if (_useGL)
                {
                    // create the SkiaSharp context
                    if (_context == null)
                    {
                        Console.WriteLine("create glcontext");
                        _glInterface = GRGlInterface.Create();
                        _context = GRContext.CreateGl(_glInterface);

                        
                        // bump the default resource cache limit
                        _context.SetResourceCacheLimit(skiaOptions?.MaxGpuResourceSizeBytes ?? 32 * 1024 * 1024);
                        Console.WriteLine("glcontext created and resource limit set");
                    }

                    _topLevelImpl.SetSurface(_context, _jsGlInfo!, ColorType,
                        new PixelSize((int)_canvasSize.Width, (int)_canvasSize.Height), _dpi);
                }
                else
                {
                    _topLevelImpl.SetSurface(ColorType,
                        new PixelSize((int)_canvasSize.Width, (int)_canvasSize.Height), _dpi, _interop.PutImageData);
                }
                
                _interop.SetCanvasSize((int)(_canvasSize.Width * _dpi), (int)(_canvasSize.Height * _dpi));

                _initialised = true;

                Threading.Dispatcher.UIThread.Post(async () =>
                {
                    _interop.RequestAnimationFrame(true);
                    
                    _sizeWatcher = new SizeWatcherInterop(_avaloniaModule, _htmlCanvas, OnSizeChanged);
                    _dpiWatcher = new DpiWatcherInterop(_avaloniaModule, OnDpiChanged);
                    
                    _topLevel.Prepare();

                    _topLevel.Renderer.Start();
                });
            }
        }

        private void OnRenderFrame()
        {
            if (_useGL && (_jsGlInfo == null))
            {
                Console.WriteLine("nothing to render");
                return;
            }
            if (_canvasSize.Width <= 0 || _canvasSize.Height <= 0 || _dpi <= 0)
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
                _inputElementFocused = true;
                _inputHelper.Focus();
            }
            else
            {
                _inputElementFocused = false;
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
