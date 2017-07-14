using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using Avalonia.Direct2D1;
using Avalonia.Direct2D1.Media;
using Avalonia.Platform;
using Microsoft.Wpf.Interop.DirectX;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Device = SharpDX.Direct3D11.Device;
using Factory = SharpDX.Direct2D1.Factory;
using RenderTarget = SharpDX.Direct2D1.RenderTarget;

namespace Avalonia.Win32.Interop.Wpf
{
    class D3D11ImageSurface : IExternalDirect2DRenderTargetSurface
    {
        private readonly D3D11Image _image = new D3D11Image();
        private Direct2D1Platform _platform;
        private Factory _factory;
        private readonly WpfTopLevelImpl _root;
        private bool _initialized = false;

        private RenderTarget _frontRenderTarget;
        private RenderTargetBitmapImpl _backBuffer;
        private Size _imageSize = Size.Empty;
        private bool _isDirty = false;

        public D3D11ImageSurface(WpfTopLevelImpl root)
        {
            _root = root;
        }

        public RenderTarget GetOrCreateRenderTarget()
        {
            InitializeRenderTarget();
            Debug.Assert(_backBuffer != null);
            return _backBuffer.RenderTarget;
        }

        public void DestroyRenderTarget()
        {
            //TODO
        }

        public void BeforeDrawing()
        {
            UpdateImageSize();
        }

        public void AfterDrawing()
        {
            _isDirty = true;
        }

        private void InitializeRenderTarget()
        {
            if (_platform == null)
            {
                _platform = (Direct2D1Platform) AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
                _factory = AvaloniaLocator.Current.GetService<Factory>();
            }
            var window = Window.GetWindow(_root.Parent);
            if (window == null)
                return;
            
            UpdateImageSize();
            if (_initialized)
                return;
            _root.ImageSource = _image;

            _image.WindowOwner = new System.Windows.Interop.WindowInteropHelper(window).Handle;
            _image.OnRender = OnImageRender;
            CompositionTarget.Rendering += OnCompositionTargetRendering; // TODO[F]: Remove handler on dispose?
            _initialized = true;
        }

        Size GetSize() => new Size(_root.ActualWidth, _root.ActualHeight);
        
        private void UpdateImageSize()
        {
            
            var virtualSize = GetSize();
            var scaling = _root.GetScaling();
            var currentSize = virtualSize * scaling;
            if (currentSize != _imageSize && currentSize.Width > 0.0 && currentSize.Height > 0.0)
            {
                _image.SetPixelSize((int)currentSize.Width, (int)currentSize.Height);
                _imageSize = currentSize;
                _backBuffer?.Dispose();
                _backBuffer = null;
                _backBuffer = (RenderTargetBitmapImpl) _platform.CreateRenderTargetBitmap((int) virtualSize.Width,
                    (int) virtualSize.Height,
                    scaling.X * 96, scaling.Y * 96);
            }
        }
        

        private void OnImageRender(IntPtr handle, bool isNewSurface)
        {
            if (isNewSurface)
            {
                _frontRenderTarget?.Dispose();
                _frontRenderTarget = null;

                using (var comObject = new ComObject(handle))
                using (var surface = comObject.QueryInterface<SharpDX.DXGI.Surface>())
                {
                    var scale = _root.GetScaling();
                    var dpiX = 96.0f * (float) scale.X;
                    var dpiY = 96.0f * (float) scale.Y;
                    var properties = new RenderTargetProperties
                    {
                        DpiX = dpiX,
                        DpiY = dpiY,
                        MinLevel = FeatureLevel.Level_DEFAULT,
                        PixelFormat =
                            new SharpDX.Direct2D1.PixelFormat(SharpDX.DXGI.Format.Unknown, AlphaMode.Premultiplied),
                        Type = RenderTargetType.Default,
                        Usage = RenderTargetUsage.None
                    };
                    _frontRenderTarget = new RenderTarget(_factory, surface, properties);
                }
            }
            _frontRenderTarget.BeginDraw();
            using (var bitmap = _backBuffer.GetDirect2DBitmap(_frontRenderTarget))
                _frontRenderTarget.DrawBitmap(bitmap,
                    new RawRectangleF(0, 0, bitmap.Size.Width, bitmap.Size.Height),
                    1, BitmapInterpolationMode.Linear);
            _frontRenderTarget.EndDraw();
            _isDirty = false;
        }

        private void OnCompositionTargetRendering(object sender, EventArgs e)
        {
            if (_isDirty)
                _image.RequestRender();
        }
    }
}
