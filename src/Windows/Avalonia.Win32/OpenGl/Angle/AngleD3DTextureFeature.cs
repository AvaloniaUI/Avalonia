using System.Runtime.InteropServices;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Win32.DirectX;

namespace Avalonia.Win32.OpenGl.Angle;

internal class AngleD3DTextureFeature  : IGlPlatformSurfaceRenderTargetFactory
{
    public bool CanRenderToSurface(IGlContext context, object surface) =>
        context is EglContext
        {
            Display: AngleWin32EglDisplay { PlatformApi: AngleOptions.PlatformApi.DirectX11 }
        } && surface is IDirect3D11TexturePlatformSurface;

    private class RenderTargetWrapper : EglPlatformSurfaceRenderTargetBase
    {
        private readonly AngleWin32EglDisplay _angle;
        private readonly IDirect3D11TextureRenderTarget _target;

        public RenderTargetWrapper(EglContext context,
            AngleWin32EglDisplay angle,
            IDirect3D11TextureRenderTarget target) : base(context)
        {
            _angle = angle;
            _target = target;
        }

        public override IGlPlatformSurfaceRenderingSession BeginDrawCore()
        {
            var success = false;
            var contextLock = Context.EnsureCurrent();
            IDirect3D11TextureRenderTargetRenderSession? session = null;
            EglSurface? surface = null;
            try
            {
                try
                {
                    session = _target.BeginDraw();
                }
                catch (RenderTargetCorruptedException e)
                {
                    if (e.InnerException is COMException com
                        && ((DXGI_ERROR)com.HResult).IsDeviceLostError()) 
                        Context.NotifyContextLost();

                    throw;
                }

                surface = _angle.WrapDirect3D11Texture(session.D3D11Texture2D, session.Offset.X, session.Offset.Y,
                    session.Size.Width, session.Size.Height);
                var rv = BeginDraw(surface, session.Size, session.Scaling, () =>
                {
                    using(contextLock)
                    using (session)
                    using (surface)
                    {
                    }
                }, true);
                success = true;
                return rv;
            }
            finally
            {
                if (!success)
                {
                    using(contextLock)
                    using (session)
                    using (surface)
                    {
                    }
                }
            }
        }

        public override void Dispose()
        {
            _target.Dispose();
            base.Dispose();
        }

        public override bool IsCorrupted => _target.IsCorrupted || base.IsCorrupted;
    }
    
    public IGlPlatformSurfaceRenderTarget CreateRenderTarget(IGlContext context, object surface)
    {
        var ctx = (EglContext)context;
        var angle = (AngleWin32EglDisplay)ctx.Display;
        var textureSurface = (IDirect3D11TexturePlatformSurface)surface;
        try
        {
            var target = textureSurface.CreateRenderTarget(context, angle.GetDirect3DDevice());
            return new RenderTargetWrapper(ctx, angle, target);
        }
        catch (COMException com)
        {
            if (((DXGI_ERROR)com.HResult).IsDeviceLostError())
                ctx.NotifyContextLost();
            throw;
        }
    }
}
