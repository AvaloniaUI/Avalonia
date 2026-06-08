using System;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Wayland.Server.Interop;
using Avalonia.Wayland.Server.Persistent;

namespace Avalonia.Wayland.Server.Transient.Rendering;

/// <summary>
/// WSI-based EGL render surface. Wraps the owning <see cref="WSurface"/>'s
/// <c>wl_surface</c> in a <c>wl_egl_window</c> and lets the EGL driver own
/// buffer allocation and presentation. <c>eglSwapBuffers</c> is the
/// implicit <c>wl_surface.commit</c> point — we never call commit ourselves
/// on this path.
/// </summary>
internal sealed class WaylandEglWsiSurface : EglGlPlatformSurfaceBase, IPlatformRenderSurface
{
    private readonly WSurface _surface;

    public WaylandEglWsiSurface(WSurface surface)
    {
        _surface = surface;
    }

    public bool IsReady => _surface.State.IsReady;

    public override IGlPlatformSurfaceRenderTarget CreateGlRenderTarget(IGlContext context)
        => new RenderTarget(_surface, (EglContext)context);

    private sealed class RenderTarget : EglPlatformSurfaceRenderTargetBase
    {
        private readonly WSurface _surface;
        private IntPtr _eglWindow;
        private EglSurface? _eglSurface;
        private PixelSize _currentSize;

        public RenderTarget(WSurface surface, EglContext context) : base(context)
        {
            _surface = surface;
        }
        
        protected override bool SkipWaits => false;

        public override PlatformRenderTargetState State =>
            IsCorrupted ? PlatformRenderTargetState.Corrupted : _surface.State;

        public override IGlPlatformSurfaceRenderingSession BeginDrawCore(IRenderTarget.RenderTargetSceneInfo sceneInfo)
        {
            if (!_surface.State.IsReady)
                throw new RenderTargetNotReadyException();

            EnsureWindow(sceneInfo.Size);

            // OnBeforeNewBufferAttached must run *immediately* before the
            // implicit commit performed by eglSwapBuffers; the base class
            // invokes beforeSwap right before the swap call.
            //
            // SwapInterval(0) is set inside beforeSwap (so it runs with our
            // surface current); GTK re-asserts it every frame defensively
            // and we follow suit — some drivers reset it on resize.
            return BeginDraw(_eglSurface!, sceneInfo.Size, sceneInfo.Scaling,
                beforeSwap: () =>
                {
                    var display = Context.Display;
                    display.EglInterface.SwapInterval(display.Handle, 0);
                    _surface.OnBeforeNewBufferAttached(sceneInfo);
                });
        }

        public override void Dispose()
        {
            _eglSurface?.Dispose();
            _eglSurface = null;
            if (_eglWindow != IntPtr.Zero)
            {
                WaylandEglNativeMethods.wl_egl_window_destroy(_eglWindow);
                _eglWindow = IntPtr.Zero;
            }
            _currentSize = default;
            base.Dispose();
        }

        private void EnsureWindow(PixelSize size)
        {
            if (_eglWindow != IntPtr.Zero && _currentSize == size)
                return;

            if (_eglWindow == IntPtr.Zero)
            {
                _eglWindow = WaylandEglNativeMethods.wl_egl_window_create(
                    _surface.WlSurface!.Handle, Math.Max(1, size.Width), Math.Max(1, size.Height));
                if (_eglWindow == IntPtr.Zero)
                    throw new OpenGlException("wl_egl_window_create failed");

                var display = Context.Display;
                var raw = display.EglInterface.CreateWindowSurface(display.Handle, display.Config, _eglWindow, null);
                if (raw == IntPtr.Zero)
                {
                    WaylandEglNativeMethods.wl_egl_window_destroy(_eglWindow);
                    _eglWindow = IntPtr.Zero;
                    throw OpenGlException.GetFormattedException("eglCreateWindowSurface", display.EglInterface);
                }

                _eglSurface = new EglSurface(display, raw);
            }
            else
            {
                WaylandEglNativeMethods.wl_egl_window_resize(_eglWindow,
                    Math.Max(1, size.Width), Math.Max(1, size.Height), 0, 0);
            }

            _currentSize = size;
        }
    }
}
