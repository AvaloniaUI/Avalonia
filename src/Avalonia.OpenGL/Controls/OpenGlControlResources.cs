using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.OpenGL.Composition;
using Avalonia.Platform;
using Avalonia.Reactive;
using Avalonia.Rendering.Composition;
using static Avalonia.OpenGL.GlConsts;
namespace Avalonia.OpenGL.Controls;

internal class OpenGlControlBaseResources : IAsyncDisposable
{
    private readonly ICompositionGlContext _context;
    private readonly ICompositionGlSwapchain _swapchain;
    private int _depthBuffer;
    public int Fbo { get; private set; }
    private PixelSize _depthBufferSize;
    public IGlContext Context => _context.Context;


    public OpenGlControlBaseResources(ICompositionGlContext context, ICompositionGlSwapchain swapchain)
    {
        _context = context;
        _swapchain = swapchain;
        using (Context.EnsureCurrent())
            Fbo = Context.GlInterface.GenFramebuffer();
    }

    public static async Task<OpenGlControlBaseResources?> TryCreateAsync(Compositor compositor, Visual visual, PixelSize initialSize)
    {
        var context = await compositor.TryCreateCompatibleGlContextAsync();
            
        if (context == null)
        {
            Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                "Compositor backend doesn't support OpenGL interop");
            return null;
        }
            
        bool success = false;
        try
        {
            var swapchain = context.CreateSwapchain(visual, initialSize);
            try
            {
                try
                {
                    var rv = new OpenGlControlBaseResources(context, swapchain);
                    success = true;
                    return rv;
                }
                catch (Exception e)
                {
                    Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                        "Unable to initialize OpenGL: {exception}", e);
                    return null;
                }
            }
            finally
            {
                if (!success)
                    await swapchain.DisposeAsync();
            }
        }
        finally
        {
            if(!success)
                await context.DisposeAsync();
        }
    }

    private void UpdateDepthRenderbuffer(PixelSize size)
    {
        if (size == _depthBufferSize && _depthBuffer != 0)
            return;

        _swapchain.Resize(size);
        
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
        ICompositionGlSwapchainLockedTexture? texture = null;
        var success = false;
        try
        {
            var gl = Context.GlInterface;
            Context.GlInterface.BindFramebuffer(GL_FRAMEBUFFER, Fbo);
            UpdateDepthRenderbuffer(size);

            texture = _swapchain.GetNextTextureIgnoringQueueLimits();
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
            return texture;
        }
        finally
        {
            if (!success)
            {
                texture?.Dispose();
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
           
            await _swapchain.DisposeAsync();
            await _context.DisposeAsync();
        }
    }
}
