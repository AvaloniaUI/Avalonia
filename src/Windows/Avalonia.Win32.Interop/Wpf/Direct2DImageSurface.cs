using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Avalonia.Direct2D1;
using Vortice.Direct2D1;
using Vortice.Direct3D11;
using Vortice.Direct3D9;
using Vortice.DXGI;
using AlphaMode = Vortice.DCommon.AlphaMode;
using Device = Vortice.Direct3D11.ID3D11Device;
using Format = Vortice.DXGI.Format;
using Query = Vortice.Direct3D11.ID3D11Query;
using QueryType = Vortice.Direct3D11.QueryType;
using RenderTarget = Vortice.Direct2D1.ID2D1RenderTarget;
using Surface = Vortice.DXGI.IDXGISurface;

namespace Avalonia.Win32.Interop.Wpf
{
    class Direct2DImageSurface : IExternalDirect2DRenderTargetSurface, IDisposable
    {
        class SwapBuffer: IDisposable
        {
            private readonly Query _event;
            private readonly Vortice.Direct3D11.ID3D11Resource _resource;
            private readonly Vortice.Direct3D11.ID3D11Resource _sharedResource;
            public Vortice.Direct3D9.IDirect3DSurface9 Texture { get; }
            public RenderTarget Target { get;}
            public IntSize Size { get; }

            public SwapBuffer(IntSize size, Vector dpi)
            {
                int width = (int) size.Width;
                int height = (int) size.Height;
                _event = s_dxDevice.CreateQuery(new QueryDescription {QueryType = QueryType.Event});
                using (var texture = s_dxDevice.CreateTexture2D(new Texture2DDescription
                {
                    Width = width,
                    Height = height,
                    ArraySize = 1,
                    MipLevels = 1,
                    Format = Format.B8G8R8A8_UNorm,
                    Usage = Vortice.Direct3D11.Usage.Default,
                    SampleDescription = new SampleDescription(2, 0),
                    BindFlags = BindFlags.RenderTarget,
                }))
                using (var surface = texture.QueryInterface<Surface>())
                
                {
                    _resource = texture.QueryInterface<Vortice.Direct3D11.ID3D11Resource>();

                    Target = Direct2D1Platform.Direct2D1Factory.CreateDxgiSurfaceRenderTarget(
                        surface,
                        new RenderTargetProperties
                        {
                            DpiX = (float)dpi.X,
                            DpiY = (float)dpi.Y,
                            MinLevel = FeatureLevel.Level_10,
                            PixelFormat = new Vortice.DCommon.PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied),
                        });
                }
                using (var texture = s_dxDevice.CreateTexture2D(new Texture2DDescription
                {
                    Width = width,
                    Height = height,
                    ArraySize = 1,
                    MipLevels = 1,
                    Format = Format.B8G8R8A8_UNorm,
                    Usage = Vortice.Direct3D11.Usage.Default,
                    SampleDescription = new SampleDescription(1, 0),
                    BindFlags = BindFlags.RenderTarget|BindFlags.ShaderResource,
                    OptionFlags = ResourceOptionFlags.Shared,
                }))
                using (var resource = texture.QueryInterface<Vortice.DXGI.IDXGIResource>())
                {
                    _sharedResource = texture.QueryInterface<Vortice.Direct3D11.ID3D11Resource>();
                    var handle = resource.SharedHandle;
                    using var texture9 = s_d3DDevice.CreateTexture(
                        texture.Description.Width, texture.Description.Height, 1,
                        Vortice.Direct3D9.Usage.RenderTarget, Vortice.Direct3D9.Format.A8R8G8B8, Pool.Default,
                        ref handle
                    );
                    Texture = texture9.GetSurfaceLevel(0);
                }
                Size = size;
            }

            public void Dispose()
            {
                Texture?.Dispose();
                Target?.Dispose();
                _resource?.Dispose();
                _sharedResource?.Dispose();
                _event?.Dispose();
            }

            public void Flush()
            {
                s_dxDevice.ImmediateContext.ResolveSubresource(_resource, 0, _sharedResource, 0, Format.B8G8R8A8_UNorm);
                s_dxDevice.ImmediateContext.Flush();
                s_dxDevice.ImmediateContext.End(_event);
                while (!s_dxDevice.ImmediateContext.IsDataAvailable(_event))
                {
                }
            }
        }

        private D3DImage _image;
        private SwapBuffer _backBuffer;
        private readonly WpfTopLevelImpl _impl;
        private static Device s_dxDevice;
        private static IDirect3D9Ex s_d3DContext;
        private static IDirect3DDevice9Ex s_d3DDevice;
        private Vector _oldDpi;


        [DllImport("user32.dll", SetLastError = false)]
        private static extern IntPtr GetDesktopWindow();
        void EnsureDirectX()
        {
            if(s_d3DDevice != null)
                return;
            D3D9.Create9Ex(out s_d3DContext).CheckError();

            Vortice.Direct3D9.PresentParameters presentparams = new()
            {
                Windowed = true,
                SwapEffect = Vortice.Direct3D9.SwapEffect.Discard,
                DeviceWindowHandle = GetDesktopWindow(),
                PresentationInterval = PresentInterval.Default
            };
            s_dxDevice ??= AvaloniaLocator.Current.GetService<Vortice.DXGI.IDXGIDevice>()
                                          .QueryInterface<Vortice.Direct3D11.ID3D11Device>();
            s_d3DDevice = s_d3DContext.CreateDeviceEx(0, DeviceType.Hardware, IntPtr.Zero, CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded | CreateFlags.FpuPreserve, presentparams);

        }

        public Direct2DImageSurface(WpfTopLevelImpl impl)
        {
            _impl = impl;
        }

        public RenderTarget GetOrCreateRenderTarget()
        {
            EnsureDirectX();
            var scale = _impl.GetScaling();
            var size = new IntSize(_impl.ActualWidth * scale.X, _impl.ActualHeight * scale.Y);
            var dpi = scale * 96;

            if (_backBuffer!=null && _backBuffer.Size == size)
                return _backBuffer.Target;

            if (_image == null || _oldDpi.X != dpi.X || _oldDpi.Y != dpi.Y)
            {
                _image = new D3DImage(dpi.X, dpi.Y);
            }
            _impl.ImageSource = _image;
            
            RemoveAndDispose(ref _backBuffer);
            if (size == default(IntSize))
            {
                _image.Lock();
                _image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
                _image.Unlock();
                return null;
            }
            _backBuffer = new SwapBuffer(size, dpi);

            return _backBuffer.Target;
        }

        void RemoveAndDispose<T>(ref T d) where T : IDisposable
        {
            d?.Dispose();
            d = default(T);
        }

        void Swap()
        {
            _backBuffer.Flush();
            _image.Lock();
            _image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _backBuffer?.Texture?.NativePointer ?? IntPtr.Zero, true);
            _image.AddDirtyRect(new Int32Rect(0, 0, _image.PixelWidth, _image.PixelHeight));
            _image.Unlock();
        }

        public void DestroyRenderTarget()
        {
            RemoveAndDispose(ref _backBuffer);
        }

        public void BeforeDrawing()
        {
            
        }

        public void AfterDrawing() => Swap();
        public void Dispose()
        {
            RemoveAndDispose(ref _backBuffer);
        }
    }
}
