using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Controls;
using Avalonia.OpenGL;
using SharpDX.DirectComposition;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32
{
    public class EglGlPlatformSurface : IGlPlatformSurface
    {
        private VirtualSurface _virtualSurface;
        private Device _device;

        public interface IEglWindowGlPlatformSurfaceInfo
        {
            IntPtr Handle { get; }
            PixelSize Size { get; }
            double Scaling { get; }
        }

        private readonly EglDisplay _display;
        private readonly EglContext _context;
        private readonly IEglWindowGlPlatformSurfaceInfo _info;

        public void AttachToWindow(IntPtr hWnd)
        {
            return;
            if (_virtualSurface != null)
            {
                return;
            }

            var Direct2D1Factory = new SharpDX.Direct2D1.Factory1(
                        SharpDX.Direct2D1.FactoryType.MultiThreaded,
                        SharpDX.Direct2D1.DebugLevel.None);

            var featureLevels = new[]
               {
                    SharpDX.Direct3D.FeatureLevel.Level_11_1,
                    SharpDX.Direct3D.FeatureLevel.Level_11_0,
                    SharpDX.Direct3D.FeatureLevel.Level_10_1,
                    SharpDX.Direct3D.FeatureLevel.Level_10_0,
                    SharpDX.Direct3D.FeatureLevel.Level_9_3,
                    SharpDX.Direct3D.FeatureLevel.Level_9_2,
                    SharpDX.Direct3D.FeatureLevel.Level_9_1,
                };

            var Direct3D11Device = new SharpDX.Direct3D11.Device(
                    SharpDX.Direct3D.DriverType.Hardware,
                    SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport | SharpDX.Direct3D11.DeviceCreationFlags.VideoSupport,
                    featureLevels);


            var DxgiDevice = Direct3D11Device.QueryInterface<SharpDX.DXGI.Device1>();

            var Direct2D1Device = new SharpDX.Direct2D1.Device(Direct2D1Factory, DxgiDevice);


            var DCompDevice = new SharpDX.DirectComposition.DesktopDevice(Direct2D1Device);

            _device = DCompDevice.QueryInterface<Device>();

            var visual = new SharpDX.DirectComposition.Visual(_device);


            //var surface = Target.FromHwnd(DCompDevice, hWnd, false);
            //surface.Root = visual;

            //GetClientRect(hWnd, out var window_rectangle);

            //_virtualSurface = new SharpDX.DirectComposition.VirtualSurface(_device, window_rectangle.right, window_rectangle.bottom, SharpDX.DXGI.Format.B8G8R8A8_UNorm, SharpDX.DXGI.AlphaMode.Premultiplied);

            //visual.Content = _virtualSurface;
        }

        public SharpDX.Direct2D1.DeviceContext BeginDraw()
        {
            //var result = _virtualSurface.BeginDraw<SharpDX.Direct2D1.DeviceContext>(null, out var offset);

            //var brush = new SharpDX.Direct2D1.SolidColorBrush(result, new SharpDX.Mathematics.Interop.RawColor4(1, 0, 0, 1));

            //result.DrawLine(new SharpDX.Mathematics.Interop.RawVector2(0, 0), new SharpDX.Mathematics.Interop.RawVector2(100, 100), brush);

            //return result;
            return null;
        }

        public void EndDraw()
        {
            _virtualSurface?.EndDraw();
            _device?.Commit();
        }

        public EglGlPlatformSurface(EglContext context, IEglWindowGlPlatformSurfaceInfo info)
        {
            _display = context.Display;
            _context = context;
            _info = info;
        }

        protected virtual EglSurface CreateEglSurface() => _display.CreateWindowSurface(_info.Handle);

        public IGlPlatformSurfaceRenderTarget CreateGlRenderTarget()
        {
            var glSurface = CreateEglSurface();
            return new RenderTarget(this, _display, _context, _info);
        }

        class RenderTarget : IGlPlatformSurfaceRenderTargetWithCorruptionInfo
        {
            private readonly EglDisplay _display;
            private readonly EglContext _context;            
            private readonly IEglWindowGlPlatformSurfaceInfo _info;
            private PixelSize _initialSize;
            EglGlPlatformSurface _surface;

            public RenderTarget(EglGlPlatformSurface surface,  EglDisplay display, EglContext context, IEglWindowGlPlatformSurfaceInfo info)
            {
                _surface = surface;
                _display = display;
                _context = context;                
                _info = info;
                _initialSize = info.Size;
            }

            public void Dispose() { }

            public bool IsCorrupted => _initialSize != _info.Size;

            public IGlPlatformSurfaceRenderingSession BeginDraw()
            {
                var l = _context.Lock();
                try
                {
                    if (IsCorrupted)
                        throw new RenderTargetCorruptedException();
                    _surface.BeginDraw();
                    //var restoreContext = _context.MakeCurrent(_sr);
                    _display.EglInterface.WaitClient();
                    _display.EglInterface.WaitGL();
                    _display.EglInterface.WaitNative(EglConsts.EGL_CORE_NATIVE_ENGINE);

                    return new Session(_surface, _display, _context, null, _info, l, null);
                }
                catch
                {
                    l.Dispose();
                    throw;
                }
            }

            class Session : IGlPlatformSurfaceRenderingSession
            {
                private readonly EglContext _context;
                private readonly EglSurface _glSurface;
                private readonly IEglWindowGlPlatformSurfaceInfo _info;
                private readonly EglDisplay _display;
                private IDisposable _lock;
                private readonly IDisposable _restoreContext;
                EglGlPlatformSurface _dcompSurface;


                public Session(EglGlPlatformSurface dcompSurface, EglDisplay display, EglContext context,
                    EglSurface glSurface, IEglWindowGlPlatformSurfaceInfo info,
                    IDisposable @lock, IDisposable restoreContext)
                {
                    _dcompSurface = dcompSurface;
                    _context = context;
                    _display = display;
                    _glSurface = glSurface;
                    _info = info;
                    _lock = @lock;
                    _restoreContext = restoreContext;
                }

                public void Dispose()
                {
                    _context.GlInterface.Flush();
                    _display.EglInterface.WaitGL();
                   // _glSurface.SwapBuffers();
                    _display.EglInterface.WaitClient();
                    _display.EglInterface.WaitGL();
                    _display.EglInterface.WaitNative(EglConsts.EGL_CORE_NATIVE_ENGINE);
                    _dcompSurface.EndDraw();
                    _restoreContext?.Dispose();
                    _lock.Dispose();
                }

                public IGlContext Context => _context;
                public PixelSize Size => _info.Size;
                public double Scaling => _info.Scaling;
                public bool IsYFlipped { get; }
            }
        }
    }
}
