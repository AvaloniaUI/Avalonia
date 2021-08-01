using System;

using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;

namespace Avalonia.Android.OpenGL
{
    internal sealed class GlRenderTarget : EglPlatformSurfaceRenderTargetBase, IGlPlatformSurfaceRenderTargetWithCorruptionInfo
    {
        private readonly EglGlPlatformSurfaceBase.IEglWindowGlPlatformSurfaceInfo _info;
        private readonly EglSurface _surface;
        private readonly IntPtr _handle;

        public GlRenderTarget(
            EglPlatformOpenGlInterface egl,
            EglGlPlatformSurfaceBase.IEglWindowGlPlatformSurfaceInfo info,
            EglSurface surface,
            IntPtr handle)
            : base(egl)
        {
            _info = info;
            _surface = surface;
            _handle = handle;
        }

        public bool IsCorrupted => _handle != _info.Handle;

        public override IGlPlatformSurfaceRenderingSession BeginDraw() => BeginDraw(_surface, _info);
    }
}
