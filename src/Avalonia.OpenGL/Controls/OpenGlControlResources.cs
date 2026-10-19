using System;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.Reactive;
using Avalonia.Rendering.Composition;
using static Avalonia.OpenGL.GlConsts;
namespace Avalonia.OpenGL.Controls;

internal sealed class OpenGlControlBaseResources : IAsyncDisposable
{
    private int _depthBuffer;
    public int Fbo { get; private set; }
    private PixelSize _depthBufferSize;
    public CompositionDrawingSurface Surface { get; }
    private readonly CompositionOpenGlSwapchain _swapchain;
    private readonly ICompositionGlContext _compositionContext;

    public IGlContext Context => _compositionContext.GlContext;

    public bool IsLost => !_compositionContext.IsValidForInterop;

    public OpenGlControlBaseResources(ICompositionGlContext context, CompositionDrawingSurface surface)
    {
        _compositionContext = context;
        Surface = surface;
        using (Context.MakeCurrent())
            Fbo = Context.GlInterface.GenFramebuffer();
        _swapchain = new CompositionOpenGlSwapchain(context, surface);
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
        CompositionOpenGlSwapchain.Lease? lease = null;
        var success = false;
        try
        {
            var gl = Context.GlInterface;
            gl.BindFramebuffer(GL_FRAMEBUFFER, Fbo);
            UpdateDepthRenderbuffer(size);

            lease = _swapchain.BeginDraw(size, out var texture);
            gl.FramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, texture.Target, texture.TextureId, 0);

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
                    // The texture should be unbound from user framebuffers before it's presented.
                    // User code could have bound another framebuffer, so rebind ours first
                    var glDone = Context.GlInterface;
                    glDone.BindFramebuffer(GL_FRAMEBUFFER, Fbo);
                    glDone.FramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0,
                        texture.Target, 0, 0);
                    lease.Dispose();
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
                try
                {
                    // The frame wasn't rendered, so it shouldn't reach the surface
                    lease?.Discard();
                }
                finally
                {
                    restoreContext.Dispose();
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!IsLost)
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
        }

        Surface.Dispose();

        await _swapchain.DisposeAsync();
        await _compositionContext.DisposeAsync();
    }
}
