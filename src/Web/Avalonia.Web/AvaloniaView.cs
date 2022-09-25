using System;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Platform.Storage;
using Avalonia.Rendering.Composition;
using Avalonia.Web.Interop;
using SkiaSharp;

namespace Avalonia.Web
{
    public partial class AvaloniaView : ITextInputMethodImpl
    {
        private readonly BrowserTopLevelImpl _topLevelImpl;
        private EmbeddableControlRoot _topLevel;

        private readonly JSObject _containerElement;
        private readonly JSObject _canvas;
        private readonly JSObject _nativeControlsContainer;
        private readonly JSObject _inputElement;

        private GLInfo? _jsGlInfo = null;
        private double _dpi = 1;
        private Size _canvasSize = new(100.0, 100.0);

        private GRContext? _context;
        private GRGlInterface? _glInterface;
        private const SKColorType ColorType = SKColorType.Rgba8888;

        private bool _useGL;
        private bool _inputElementFocused;
        private static int _canvasCount;

        public AvaloniaView(string divId)
        {
            var host = DomHelper.GetElementById(divId);
            if (host == null)
            {
                throw new Exception($"Element with id {divId} was not found in the html document.");
            }

            var hostContent = DomHelper.CreateAvaloniaHost(host);
            if (hostContent == null)
            {
                throw new InvalidOperationException("Avalonia WASM host wasn't initialized.");
            }

            _containerElement = hostContent.GetPropertyAsJSObject("host")
                ?? throw new InvalidOperationException("Host cannot be null");
            _canvas = hostContent.GetPropertyAsJSObject("canvas")
                ?? throw new InvalidOperationException("Canvas cannot be null");
            _nativeControlsContainer = hostContent.GetPropertyAsJSObject("nativeHost")
                ?? throw new InvalidOperationException("NativeHost cannot be null");
            _inputElement = hostContent.GetPropertyAsJSObject("inputElement")
                ?? throw new InvalidOperationException("InputElement cannot be null");

            _canvas.SetProperty("id", $"avaloniaCanvas{_canvasCount++}");

            _topLevelImpl = new BrowserTopLevelImpl(this);

            _topLevel = new EmbeddableControlRoot(_topLevelImpl);
            _topLevelImpl.SetCssCursor = (cursor) =>
            {
                InputHelper.SetCursor(_containerElement, cursor); // macOS
                InputHelper.SetCursor(_canvas, cursor); // windows
            };

            _topLevel.Prepare();

            _topLevel.Renderer.Start();

            InputHelper.SubscribeKeyboardEvents(
                _containerElement,
                (code, key, modifier) => _topLevelImpl.RawKeyboardEvent(RawKeyEventType.KeyDown, code, key, (RawInputModifiers)modifier),
                (code, key, modifier) => _topLevelImpl.RawKeyboardEvent(RawKeyEventType.KeyUp, code, key, (RawInputModifiers)modifier));

            InputHelper.SubscribePointerEvents(_containerElement, OnPointerMove, OnPointerDown, OnPointerUp, OnWheel);

            var skiaOptions = AvaloniaLocator.Current.GetService<SkiaOptions>();

            _dpi = DomHelper.ObserveDpi(OnDpiChanged);

            _useGL = skiaOptions?.CustomGpuFactory != null;

            if (_useGL)
            {
                _jsGlInfo = CanvasHelper.InitialiseGL(_canvas, OnRenderFrame);
                // create the SkiaSharp context
                if (_context == null)
                {
                    _glInterface = GRGlInterface.Create();
                    _context = GRContext.CreateGl(_glInterface);

                    // bump the default resource cache limit
                    _context.SetResourceCacheLimit(skiaOptions?.MaxGpuResourceSizeBytes ?? 32 * 1024 * 1024);
                }

                _topLevelImpl.Surfaces = new[] { new BrowserSkiaSurface(_context, _jsGlInfo, ColorType, new PixelSize((int)_canvasSize.Width, (int)_canvasSize.Height), _dpi, GRSurfaceOrigin.BottomLeft) };
            }
            else
            {
                //var rasterInitialized = _interop.InitRaster();
                //Console.WriteLine("raster initialized: {0}", rasterInitialized);

                //_topLevelImpl.SetSurface(ColorType,
                // new PixelSize((int)_canvasSize.Width, (int)_canvasSize.Height), _dpi, _interop.PutImageData);
            }

            CanvasHelper.SetCanvasSize(_canvas, (int)(_canvasSize.Width * _dpi), (int)(_canvasSize.Height * _dpi));

            _topLevelImpl.SetClientSize(_canvasSize, _dpi);

            DomHelper.ObserveSize(host, divId, OnSizeChanged);

            CanvasHelper.RequestAnimationFrame(_canvas, true);
        }

        private static RawPointerPoint ExtractRawPointerFromJSArgs(JSObject args)
        {
            var point = new RawPointerPoint
            {
                Position = new Point(args.GetPropertyAsDouble("offsetX"), args.GetPropertyAsDouble("offsetY")),
                Pressure = (float)args.GetPropertyAsDouble("pressure"),
                XTilt = (float)args.GetPropertyAsDouble("tiltX"),
                YTilt = (float)args.GetPropertyAsDouble("tiltY"),
                Twist = (float)args.GetPropertyAsDouble("twist")
            };

            return point;
        }

        private bool OnPointerMove(JSObject args)
        {
            var type = args.GetPropertyAsString("pointertype");

            var point = ExtractRawPointerFromJSArgs(args);

            return _topLevelImpl.RawPointerEvent(RawPointerEventType.Move, type!, point, GetModifiers(args), args.GetPropertyAsInt32("pointerId"));
        }

        private bool OnPointerDown(JSObject args)
        {
            var pointerType = args.GetPropertyAsString("pointerType");

            var type = pointerType switch
            {
                "touch" => RawPointerEventType.TouchBegin,
                _ => args.GetPropertyAsInt32("button") switch
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

            var point = ExtractRawPointerFromJSArgs(args);

            return _topLevelImpl.RawPointerEvent(type, pointerType!, point, GetModifiers(args), args.GetPropertyAsInt32("pointerId"));
        }

        private bool OnPointerUp(JSObject args)
        {
            var pointerType = args.GetPropertyAsString("pointerType") ?? "mouse";

            var type = pointerType switch
            {
                "touch" => RawPointerEventType.TouchEnd,
                _ => args.GetPropertyAsInt32("button") switch
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

            var point = ExtractRawPointerFromJSArgs(args);

            return _topLevelImpl.RawPointerEvent(type, pointerType, point, GetModifiers(args), args.GetPropertyAsInt32("pointerId"));
        }

        private bool OnWheel(JSObject args)
        {
            return _topLevelImpl.RawMouseWheelEvent(new Point(args.GetPropertyAsDouble("clientX"), args.GetPropertyAsDouble("clientY")),
                new Vector(-(args.GetPropertyAsDouble("deltaX") / 50), -(args.GetPropertyAsDouble("deltaY") / 50)), GetModifiers(args));
        }

        private static RawInputModifiers GetModifiers(JSObject e)
        {
            var modifiers = RawInputModifiers.None;

            if (e.GetPropertyAsBoolean("ctrlKey"))
                modifiers |= RawInputModifiers.Control;
            if (e.GetPropertyAsBoolean("altKey"))
                modifiers |= RawInputModifiers.Alt;
            if (e.GetPropertyAsBoolean("shiftKey"))
                modifiers |= RawInputModifiers.Shift;
            if (e.GetPropertyAsBoolean("metaKey"))
                modifiers |= RawInputModifiers.Meta;

            var buttons = e.GetPropertyAsInt32("buttons");
            if ((buttons & 1L) == 1)
                modifiers |= RawInputModifiers.LeftMouseButton;

            if ((buttons & 2L) == 2)
                modifiers |= e.GetPropertyAsString("type") == "pen" ? RawInputModifiers.PenBarrelButton : RawInputModifiers.RightMouseButton;

            if ((buttons & 4L) == 4)
                modifiers |= RawInputModifiers.MiddleMouseButton;

            if ((buttons & 8L) == 8)
                modifiers |= RawInputModifiers.XButton1MouseButton;

            if ((buttons & 16L) == 16)
                modifiers |= RawInputModifiers.XButton2MouseButton;

            if ((buttons & 32L) == 32)
                modifiers |= RawInputModifiers.PenEraser;

            return modifiers;
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

        public Control? Content
        {
            get => (Control)_topLevel.Content!;
            set => _topLevel.Content = value;
        }

        internal INativeControlHostImpl GetNativeControlHostImpl()
        {
            throw new NotImplementedException();
            //return _nativeControlHost ?? throw new InvalidOperationException("Blazor View wasn't initialized yet");
        }

        internal IStorageProvider GetStorageProvider()
        {
            throw new NotImplementedException();
            //return _storageProvider ?? throw new InvalidOperationException("Blazor View wasn't initialized yet");
        }

        private void ForceBlit()
        {
            // Note: this is technically a hack, but it's a kinda unique use case when
            // we want to blit the previous frame
            // renderer doesn't have much control over the render target
            // we render on the UI thread
            // We also don't want to have it as a meaningful public API.
            // Therefore we have InternalsVisibleTo hack here.

            if (_topLevel.Renderer is CompositingRenderer dr)
            {
                dr.CompositionTarget.ImmediateUIThreadRender();
            }
        }

        private void OnDpiChanged(double oldDpi, double newDpi)
        {
            if (Math.Abs(_dpi - newDpi) > 0.0001)
            {
                _dpi = newDpi;

                CanvasHelper.SetCanvasSize(_canvas, (int)(_canvasSize.Width * _dpi), (int)(_canvasSize.Height * _dpi));

                _topLevelImpl.SetClientSize(_canvasSize, _dpi);

                ForceBlit();
            }
        }

        private void OnSizeChanged(int height, int width)
        {
            var newSize = new Size(height, width);

            if (_canvasSize != newSize)
            {
                _canvasSize = newSize;

                CanvasHelper.SetCanvasSize(_canvas, (int)(_canvasSize.Width * _dpi), (int)(_canvasSize.Height * _dpi));

                _topLevelImpl.SetClientSize(_canvasSize, _dpi);

                ForceBlit();
            }
        }

        public void SetClient(ITextInputMethodClient? client)
        {
            
        }

        public void SetCursorRect(Rect rect)
        {
        }

        public void SetOptions(TextInputOptions options)
        {
        }

        public void Reset()
        {
            
        }
    }
}
