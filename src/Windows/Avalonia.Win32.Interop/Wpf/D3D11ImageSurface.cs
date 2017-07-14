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
using SharpDX.Mathematics.Interop;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Factory = SharpDX.Direct2D1.Factory;
using RenderTarget = SharpDX.Direct2D1.RenderTarget;

namespace Avalonia.Win32.Interop.Wpf
{
    class D3D11ImageSurface : IExternalDirect2DRenderTargetSurface, IDisposable
    {
        private D3D11Image _image;
        private readonly WpfTopLevelImpl _root;

        private RenderTarget _renderTarget;
        private Size _imageSize = Size.Empty;
        private bool _isDirty = false;

        public D3D11ImageSurface(WpfTopLevelImpl root)
        {
            _root = root;        }

        public RenderTarget GetOrCreateRenderTarget()
        {
            return _renderTarget;
        }

        public void DestroyRenderTarget()
        {
        }

        public void BeforeDrawing()
        {
        }

        public void AfterDrawing()
        {
        }

        private static readonly System.Windows.Forms.Control s_dummy = new System.Windows.Forms.Control();

        public void Initialize()
        {
            _image = _image ?? new D3D11Image();
            UpdateImageSize();
            _root.ImageSource = _image;
            _image.WindowOwner = s_dummy.Handle;
            _image.OnRender = OnImageRender;
            CompositionTarget.Rendering += OnCompositionTargetRendering; // TODO[F]: Remove handler on dispose?
        }

        Size GetSize() => new Size(_root.ActualWidth, _root.ActualHeight);
        
        public void UpdateImageSize()
        {
            
            var virtualSize = GetSize();
            var scaling = _root.GetScaling();
            var currentSize = virtualSize * scaling;
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
                _renderTarget?.Dispose();
                _renderTarget = null;

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
                    _renderTarget = new RenderTarget(AvaloniaLocator.Current.GetService<Factory>(), surface, properties);
                }
            }
            _root.ControlRoot.PlatformImpl?.Paint?.Invoke(new Rect(0, 0, _root.ActualWidth,
                _root.ActualHeight));
            _isDirty = false;
        }

        private void OnCompositionTargetRendering(object sender, EventArgs e)
        {
            if (_root.Parent == null)
                return;
            UpdateImageSize();
            if (_isDirty)
            {
                _image.RequestRender();
                _root.InvalidateVisual();
            }
        }

        public void Dispose()
        {
            _renderTarget?.Dispose();
            _renderTarget = null;
            _image?.Dispose();
            _image = null;
            CompositionTarget.Rendering -= OnCompositionTargetRendering;
        }

        public void MakeDirty() => _isDirty = true;
    }
}
