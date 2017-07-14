using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using Avalonia.Direct2D1;
using Microsoft.Wpf.Interop.DirectX;
using SharpDX;
using SharpDX.Direct2D1;
using RenderTarget = SharpDX.Direct2D1.RenderTarget;

namespace Avalonia.Win32.Interop.Wpf
{
    class D3D11ImageSurface : IExternalDirect2DRenderTargetSurface
    {
        private readonly D3D11Image _image = new D3D11Image();
        private readonly WpfTopLevelImpl _root;

        private RenderTarget _renderTarget;
        private Size _imageSize = Size.Empty;
        private TimeSpan _lastRenderTime;

        public D3D11ImageSurface(WpfTopLevelImpl root)
        {
            _root = root;
        }

        public RenderTarget CreateRenderTarget()
        {
            InitializeRenderTarget();
            Debug.Assert(_renderTarget != null);
            return _renderTarget;
        }

        public void BeforeDrawing()
        {
            UpdateImageSize();
        }

        public void AfterDrawing()
        {
        }

        private void InitializeRenderTarget()
        {
            var window = Window.GetWindow(_root.Parent);
            if (window == null)
            {
                return;
            }

            UpdateImageSize();
            _root.ImageSource = _image;

            _image.WindowOwner = new System.Windows.Interop.WindowInteropHelper(window).Handle;
            _image.OnRender = OnImageRender;
            CompositionTarget.Rendering += OnCompositionTargetRendering; // TODO[F]: Remove handler on dispose?
            _image.RequestRender();
        }

        private void UpdateImageSize()
        {
            var currentSize = new Size(_root.ActualWidth, _root.ActualHeight);
            if (currentSize != _imageSize && currentSize.Width > 0.0 && currentSize.Height > 0.0)
            {
                _image.SetPixelSize((int)currentSize.Width, (int)currentSize.Height);
                _imageSize = currentSize;
            }
        }

        private void OnImageRender(IntPtr handle, bool isNewSurface)
        {
            if (isNewSurface)
            {
                if (_renderTarget != null)
                {
                    _renderTarget.Dispose();
                    _renderTarget = null;
                }

                var comObject = new ComObject(handle);
                var resource = comObject.QueryInterface<SharpDX.DXGI.Resource>();
                var texture = resource.QueryInterface<SharpDX.Direct3D11.Texture2D>();
                using (var surface = texture.QueryInterface<SharpDX.DXGI.Surface>())
                {
                    var source = PresentationSource.FromVisual(_root);
                    Debug.Assert(source != null);
                    Debug.Assert(source.CompositionTarget != null);

                    var dpiX = 96.0f * (float)source.CompositionTarget.TransformToDevice.M11;
                    var dpiY = 96.0f * (float)source.CompositionTarget.TransformToDevice.M22;
                    var properties = new RenderTargetProperties
                    {
                        DpiX = dpiX,
                        DpiY = dpiY,
                        MinLevel = FeatureLevel.Level_DEFAULT,
                        PixelFormat = new SharpDX.Direct2D1.PixelFormat(SharpDX.DXGI.Format.Unknown, AlphaMode.Premultiplied),
                        Type = RenderTargetType.Default,
                        Usage = RenderTargetUsage.None
                    };

                    var factory = AvaloniaLocator.Current.GetService<Factory>();
                    _renderTarget = new RenderTarget(factory, surface, properties);
                }
            }
        }

        private void OnCompositionTargetRendering(object sender, EventArgs e)
        {
            var args = (RenderingEventArgs)e;

            // It's possible for Rendering to call back twice in the same frame
            // so only render when we haven't already rendered in this frame.
            if (_lastRenderTime != args.RenderingTime)
            {
                _image.RequestRender();
                _lastRenderTime = args.RenderingTime;
            }
        }
    }
}
