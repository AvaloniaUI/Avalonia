using System;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Platform;
using Avalonia.Input.TextInput;
using Avalonia.Platform.Storage;
using Avalonia.Rendering.Composition;
using Avalonia.Web.Blazor;
using Avalonia.Web.Interop;
using SkiaSharp;
using static Avalonia.Web.AvaloniaRuntime;

namespace Avalonia.Web
{
    public partial class AvaloniaView : ITextInputMethodImpl
    {
        [JSImport("globalThis.document.getElementById")]
        internal static partial JSObject GetElementById(string id);

        private readonly RazorViewTopLevelImpl _topLevelImpl;
        private EmbeddableControlRoot _topLevel;

        // Interop
        /*private SKHtmlCanvasInterop? _interop = null;
        private SizeWatcherInterop? _sizeWatcher = null;
        private DpiWatcherInterop? _dpiWatcher = null;*/
        private GLInfo? _jsGlInfo = null;
        /*private AvaloniaModule? _avaloniaModule = null;
        private InputHelperInterop? _inputHelper = null;
        private InputHelperInterop? _canvasHelper = null;
        private InputHelperInterop? _containerHelper = null;
        private NativeControlHostInterop? _nativeControlHost = null;
        private StorageProviderInterop? _storageProvider = null;
        private ElementReference _htmlCanvas;
        private ElementReference _inputElement;
        private ElementReference _containerElement;
        private ElementReference _nativeControlsContainer;*/
        private JSObject _canvas;
        private double _dpi = 1;
        private Size _canvasSize = new(100.0, 100.0);

        private GRContext? _context;
        private GRGlInterface? _glInterface;
        private const SKColorType ColorType = SKColorType.Rgba8888;

        private bool _useGL;
        private bool _inputElementFocused;

        public AvaloniaView()
        {
            var div = GetElementById("out");

            _canvas = CreateCanvas(div);
            _canvas.SetProperty("id", "mycanvas");

            _topLevelImpl = new RazorViewTopLevelImpl(this);

            _topLevel = new EmbeddableControlRoot(_topLevelImpl);

            _topLevel.Prepare();

            _topLevel.Renderer.Start();

            InputHelper.SubscribeKeyboardEvents(
                div,
                (code, key, modifier) => _topLevelImpl.RawKeyboardEvent(Input.Raw.RawKeyEventType.KeyDown, code, key, (Input.RawInputModifiers)modifier),
                (code, key, modifier) => _topLevelImpl.RawKeyboardEvent(Input.Raw.RawKeyEventType.KeyUp, code, key, (Input.RawInputModifiers)modifier));

            var skiaOptions = AvaloniaLocator.Current.GetService<SkiaOptions>();

            _dpi = ObserveDpi(OnDpiChanged);

            Console.WriteLine($"Started observing dpi: {_dpi}");

            _useGL = skiaOptions?.CustomGpuFactory != null;

            if (_useGL)
            {
                _jsGlInfo = AvaloniaRuntime.InitialiseGL(_canvas, OnRenderFrame);
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

                _topLevelImpl.Surfaces = new[] { new BlazorSkiaSurface(_context, _jsGlInfo, ColorType, new PixelSize((int)_canvasSize.Width, (int)_canvasSize.Height), _dpi, GRSurfaceOrigin.BottomLeft) };
            }
            else
            {
                //var rasterInitialized = _interop.InitRaster();
                //Console.WriteLine("raster initialized: {0}", rasterInitialized);

                //_topLevelImpl.SetSurface(ColorType,
                // new PixelSize((int)_canvasSize.Width, (int)_canvasSize.Height), _dpi, _interop.PutImageData);
            }

            AvaloniaRuntime.SetCanvasSize(_canvas, (int)(_canvasSize.Width * _dpi), (int)(_canvasSize.Height * _dpi));

            _topLevelImpl.SetClientSize(_canvasSize, _dpi);

            ObserveSize(_canvas, "mycanvas", OnSizeChanged);

            RequestAnimationFrame(_canvas, true);
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

        public bool KeyPreventDefault { get; set; }

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
                //dr.CompositionTarget.ImmediateUIThreadRender();
            }
        }

        private void OnDpiChanged(double oldDpi, double newDpi)
        {
            if (Math.Abs(_dpi - newDpi) > 0.0001)
            {
                _dpi = newDpi;

                SetCanvasSize(_canvas, (int)(_canvasSize.Width * _dpi), (int)(_canvasSize.Height * _dpi));

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

                SetCanvasSize(_canvas, (int)(_canvasSize.Width * _dpi), (int)(_canvasSize.Height * _dpi));

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
