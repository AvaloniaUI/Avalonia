using System;
using System.Runtime.InteropServices;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Angle;
using Windows.UI.Composition.Interop;

namespace Avalonia.Win32
{
    internal class CompositionEglGlPlatformSurface : EglGlPlatformSurfaceBase
    {
        private readonly EglDisplay _display;
        private readonly EglContext _context;
        private readonly IEglWindowGlPlatformSurfaceInfo _info;
        private ICompositionDrawingSurfaceInterop _surfaceInterop;
        private Windows.UI.Composition.Visual _surface;

        public CompositionEglGlPlatformSurface(EglContext context, IEglWindowGlPlatformSurfaceInfo info) : base()
        {
            _display = context.Display;
            _context = context;
            _info = info;
        }

        public IBlurHost AttachToCompositionTree(IntPtr hwnd)
        {
            _surfaceInterop = CompositionHost.Instance.InitialiseWindowCompositionTree(hwnd, out _surface, out var blurHost);

            return blurHost;
        }

        public override IGlPlatformSurfaceRenderTarget CreateGlRenderTarget()
        {
            return new CompositionRenderTarget(_display, _context, _surface, _surfaceInterop, _info);
        }

        class CompositionRenderTarget : EglPlatformSurfaceRenderTargetBase
        {
            private readonly EglDisplay _display;
            private readonly IEglWindowGlPlatformSurfaceInfo _info;
            private readonly PixelSize _initialSize;
            private readonly ICompositionDrawingSurfaceInterop _surfaceInterop;
            private static Guid s_Iid = Guid.Parse("6f15aaf2-d208-4e89-9ab4-489535d34f9c");
            private Windows.UI.Composition.Visual _compositionVisual;

            public CompositionRenderTarget(EglDisplay display,
                EglContext context,
                Windows.UI.Composition.Visual compositionVisual,
                ICompositionDrawingSurfaceInterop interopSurface,
                IEglWindowGlPlatformSurfaceInfo info)
                : base(display, context)
            {
                _display = display;
                _surfaceInterop = interopSurface;
                _info = info;
                _initialSize = info.Size;
                _compositionVisual = compositionVisual;
                _surfaceInterop.Resize(new POINT { X = _info.Size.Width, Y = _info.Size.Height });
                _compositionVisual.Size = new System.Numerics.Vector2(_info.Size.Width, _info.Size.Height);
            }

            public override bool IsCorrupted => false;
            public override IGlPlatformSurfaceRenderingSession BeginDraw()
            {
                var offset = new POINT();

                _surfaceInterop.BeginDraw(
                    IntPtr.Zero,
                    ref s_Iid,
                    out IntPtr texture, ref offset);

                var surface = (_display as AngleWin32EglDisplay).WrapDirect3D11Texture(texture, offset.X, offset.Y, _info.Size.Width, _info.Size.Height);

                return base.BeginDraw(surface, _info, () => { _surfaceInterop.EndDraw(); Marshal.Release(texture); surface.Dispose(); }, true);
            }
        }
    }
}

