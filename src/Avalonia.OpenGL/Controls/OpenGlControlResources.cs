using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Reactive;
using Avalonia.Rendering.Composition;
using static Avalonia.OpenGL.GlConsts;
namespace Avalonia.OpenGL.Controls;

internal class OpenGlControlBaseResources : IAsyncDisposable
{
    private int _depthBuffer;
    public int Fbo { get; private set; }
    private PixelSize _depthBufferSize;
    public CompositionDrawingSurface Surface { get; }
    private readonly CompositionOpenGlSwapchain _swapchain;
    public IGlContext Context { get; private set; }

    public static OpenGlControlBaseResources? TryCreate(CompositionDrawingSurface surface,
        ICompositionGpuInterop interop,
        IOpenGlTextureSharingRenderInterfaceContextFeature feature)
    {
        IGlContext? context;
        try
        {
            context = feature.CreateSharedContext();
        }
        catch (Exception e)
        {
            Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                "Unable to initialize OpenGL: unable to create additional OpenGL context: {exception}", e);
            return null;
        }

        if (context == null)
        {
            Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                "Unable to initialize OpenGL: unable to create additional OpenGL context.");
            return null;
        }

        return new OpenGlControlBaseResources(context, surface, interop, feature, null);
    }

    public static OpenGlControlBaseResources? TryCreate(IGlContext context, CompositionDrawingSurface surface,
        ICompositionGpuInterop interop, IGlContextExternalObjectsFeature externalObjects)
    {
        if (!interop.SupportedImageHandleTypes.Contains(KnownPlatformGraphicsExternalImageHandleTypes
                .D3D11TextureGlobalSharedHandle)
            || !externalObjects.SupportedExportableExternalImageTypes.Contains(
                KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureGlobalSharedHandle))
            return null;
        return new OpenGlControlBaseResources(context, surface, interop, null, externalObjects);
    }
    
    private OpenGlControlBaseResources(IGlContext context,
        CompositionDrawingSurface surface,
        ICompositionGpuInterop interop,
        IOpenGlTextureSharingRenderInterfaceContextFeature? feature,
        IGlContextExternalObjectsFeature? externalObjects
        )
    {
        Context = context;
        Surface = surface;
        using (context.MakeCurrent())
            Fbo = context.GlInterface.GenFramebuffer();
        _swapchain =
            feature != null ?
                new CompositionOpenGlSwapchain(context, interop, Surface, feature) :
                new CompositionOpenGlSwapchain(context, interop, Surface, externalObjects);
    }

    private void UpdateDepthRenderbuffer(PixelSize size)
    {
        if (size == _depthBufferSize && _depthBuffer != 0)
            return;

        var gl = Context.GlInterface;
        gl.GetIntegerv(GL_RENDERBUFFER_BINDING, out var oldRenderBuffer);
        if (_depthBuffer != 0) gl.DeleteRenderbuffer(_depthBuffer);

        _depthBuffer = gl.GenRenderbuffer();
        gl.BindRenderbuffer(GL_RENDERBUFFER, _depthBuffer);
        gl.RenderbufferStorage(GL_RENDERBUFFER,
            Context.Version.Type == GlProfileType.OpenGLES ? GL_DEPTH_COMPONENT16 : GL_DEPTH_COMPONENT,
            size.Width, size.Height);
        gl.FramebufferRenderbuffer(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_RENDERBUFFER, _depthBuffer);
        gl.BindRenderbuffer(GL_RENDERBUFFER, oldRenderBuffer);
        _depthBufferSize = size;
    }
    
    public IDisposable BeginDraw(PixelSize size)
    {
        var restoreContext = Context.EnsureCurrent();
        IDisposable? imagePresent = null;
        var success = false;
        try
        {
            var gl = Context.GlInterface;
            Context.GlInterface.BindFramebuffer(GL_FRAMEBUFFER, Fbo);
            UpdateDepthRenderbuffer(size);

            imagePresent = _swapchain.BeginDraw(size, out var texture);
            gl.FramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, texture.TextureId, 0);

            var status = gl.CheckFramebufferStatus(GL_FRAMEBUFFER);
            if (status != GL_FRAMEBUFFER_COMPLETE)
            {
                int code = gl.GetError();
                Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                    "Unable to configure OpenGL FBO: {code}", code);
                throw OpenGlException.GetFormattedException("Unable to configure OpenGL FBO", code);
            }

            success = true;
            return Disposable.Create(() =>
            {
                try
                {
                    Context.GlInterface.Flush();
                    imagePresent.Dispose();
                }
                finally
                {
                    restoreContext.Dispose();
                }
            });
        }
        finally
        {
            if (!success)
            {
                imagePresent?.Dispose();
                restoreContext.Dispose();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Context is { IsLost: false })
        {
            try
            {
                using (Context.MakeCurrent())
                {
                    var gl = Context.GlInterface;
                    if (Fbo != 0)
                        gl.DeleteFramebuffer(Fbo);
                    Fbo = 0;
                    if (_depthBuffer != 0)
                        gl.DeleteRenderbuffer(_depthBuffer);
                    _depthBuffer = 0;
                }

            }
            catch
            {
                //
            }

            Surface.Dispose();
           
            await _swapchain.DisposeAsync();
            Context.Dispose();

            Context = null!;
        }
    }
}
