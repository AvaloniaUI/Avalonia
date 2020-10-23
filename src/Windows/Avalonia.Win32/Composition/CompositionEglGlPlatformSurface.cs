﻿using System;
using System.Runtime.InteropServices;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Angle;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;
using Windows.UI.Composition.Interop;

namespace Avalonia.Win32
{
    internal class CompositionEglGlPlatformSurface : EglGlPlatformSurfaceBase
    {
        private EglPlatformOpenGlInterface _egl;
        private readonly IEglWindowGlPlatformSurfaceInfo _info;
        private ICompositionDrawingSurfaceInterop _surfaceInterop;
        private Windows.UI.Composition.Visual _surface;

        public CompositionEglGlPlatformSurface(EglPlatformOpenGlInterface egl, IEglWindowGlPlatformSurfaceInfo info) : base()
        {
            _egl = egl;
            _info = info;
        }

        public IBlurHost AttachToCompositionTree(CompositionConnector connector, IntPtr hwnd)
        {
            using (_egl.PrimaryContext.MakeCurrent())
            {
                _surfaceInterop = connector.InitialiseWindowCompositionTree(hwnd, out _surface, out var blurHost);
                return blurHost;
            }
        }

        public override IGlPlatformSurfaceRenderTarget CreateGlRenderTarget()
        {
            return new CompositionRenderTarget(_egl, _surface, _surfaceInterop, _info);
        }

        class CompositionRenderTarget : EglPlatformSurfaceRenderTargetBase
        {
            private readonly EglPlatformOpenGlInterface _egl;
            private readonly IEglWindowGlPlatformSurfaceInfo _info;
            private PixelSize _currentSize;
            private readonly ICompositionDrawingSurfaceInterop _surfaceInterop;
            private static Guid s_Iid = Guid.Parse("6f15aaf2-d208-4e89-9ab4-489535d34f9c");
            private Windows.UI.Composition.Visual _compositionVisual;

            public CompositionRenderTarget(EglPlatformOpenGlInterface egl,
                Windows.UI.Composition.Visual compositionVisual,
                ICompositionDrawingSurfaceInterop interopSurface,
                IEglWindowGlPlatformSurfaceInfo info)
                : base(egl)
            {
                _egl = egl;
                _surfaceInterop = interopSurface;
                _info = info;
                _currentSize = info.Size;
                _compositionVisual = compositionVisual;

                using (_egl.PrimaryContext.MakeCurrent())
                {
                    _surfaceInterop.Resize(new POINT { X = _info.Size.Width, Y = _info.Size.Height });
                }

                _compositionVisual.Size = new System.Numerics.Vector2(_info.Size.Width, _info.Size.Height);
            }

            public override IGlPlatformSurfaceRenderingSession BeginDraw()
            {
                IntPtr texture;
                EglSurface surface;

                using (_egl.PrimaryEglContext.EnsureCurrent())
                {
                    if (_info.Size != _currentSize)
                    {
                        _surfaceInterop.Resize(new POINT { X = _info.Size.Width, Y = _info.Size.Height });
                        _compositionVisual.Size = new System.Numerics.Vector2(_info.Size.Width, _info.Size.Height);
                        _currentSize = _info.Size;
                    }

                    var offset = new POINT();

                    _surfaceInterop.BeginDraw(
                        IntPtr.Zero,
                        ref s_Iid,
                        out texture, ref offset);

                    surface = (_egl.Display as AngleWin32EglDisplay).WrapDirect3D11Texture(_egl, texture, offset.X, offset.Y, _info.Size.Width, _info.Size.Height);
                }

                return base.BeginDraw(surface, _info, () => { _surfaceInterop.EndDraw(); Marshal.Release(texture); surface.Dispose(); }, true);
            }
        }
    }
}

