using System;
using Avalonia.OpenGL;
using NImpeller;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.Impeller;

/// <summary>A GL texture with a framebuffer, wrapped as an Impeller texture.</summary>
internal sealed class ImpellerGlFramebuffer : IDisposable
{
    private readonly ImpellerGlBackendContext _context;
    private int _texture;
    private int _framebuffer;
    private int _depthStencil;

    public ImpellerGlFramebuffer(ImpellerGlBackendContext context, PixelSize size)
    {
        _context = context;
        Size = size;
        var gl = context.GlContext.GlInterface;
        gl.GetIntegerv(GL_FRAMEBUFFER_BINDING, out var oldFramebuffer);
        gl.GetIntegerv(GL_TEXTURE_BINDING_2D, out var oldTexture);
        try
        {
            _texture = gl.GenTexture();
            gl.BindTexture(GL_TEXTURE_2D, _texture);
            gl.TexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, size.Width, size.Height, 0, GL_RGBA, GL_UNSIGNED_BYTE,
                IntPtr.Zero);
            gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
            gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
            gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
            gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);

            // Impeller clips with the stencil buffer.
            _depthStencil = gl.GenRenderbuffer();
            gl.BindRenderbuffer(GL_RENDERBUFFER, _depthStencil);
            gl.RenderbufferStorage(GL_RENDERBUFFER, GL_DEPTH24_STENCIL8, size.Width, size.Height);
            gl.BindRenderbuffer(GL_RENDERBUFFER, 0);

            _framebuffer = gl.GenFramebuffer();
            gl.BindFramebuffer(GL_FRAMEBUFFER, _framebuffer);
            gl.FramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, _texture, 0);
            gl.FramebufferRenderbuffer(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_RENDERBUFFER, _depthStencil);
            gl.FramebufferRenderbuffer(GL_FRAMEBUFFER, GL_STENCIL_ATTACHMENT, GL_RENDERBUFFER, _depthStencil);
            var status = gl.CheckFramebufferStatus(GL_FRAMEBUFFER);
            if (status != GL_FRAMEBUFFER_COMPLETE)
                throw new InvalidOperationException($"Incomplete framebuffer for an Impeller layer: 0x{status:X}.");

            Texture = context.ImpellerContext.TextureCreateWithOpenGLTextureHandleNew(new ImpellerTextureDescriptor
            {
                Pixel_format = ImpellerPixelFormat.kImpellerPixelFormatRGBA8888,
                Size = new ImpellerISize(size.Width, size.Height),
                Mip_count = 1
            }, (ulong)_texture) ?? throw new InvalidOperationException("Failed to wrap a GL texture for Impeller.");
        }
        catch
        {
            Dispose();
            throw;
        }
        finally
        {
            gl.BindTexture(GL_TEXTURE_2D, oldTexture);
            gl.BindFramebuffer(GL_FRAMEBUFFER, oldFramebuffer);
        }
    }

    public PixelSize Size { get; }

    public ImpellerTexture Texture { get; } = null!;

    public ImpellerSurface CreateSurface() =>
        _context.ImpellerContext.SurfaceCreateWrappedFBONew((ulong)_framebuffer,
            ImpellerPixelFormat.kImpellerPixelFormatRGBA8888, new ImpellerISize(Size.Width, Size.Height))
        ?? throw new InvalidOperationException("Failed to wrap a GL framebuffer for Impeller.");

    /// <summary>Reads the pixels top-down as RGBA8888 premultiplied.</summary>
    public void ReadPixels(IntPtr destination)
    {
        var gl = _context.GlContext.GlInterface;
        gl.GetIntegerv(GL_FRAMEBUFFER_BINDING, out var oldFramebuffer);
        gl.BindFramebuffer(GL_FRAMEBUFFER, _framebuffer);
        gl.Finish();
        gl.ReadPixels(0, 0, Size.Width, Size.Height, GL_RGBA, GL_UNSIGNED_BYTE, destination);
        gl.BindFramebuffer(GL_FRAMEBUFFER, oldFramebuffer);
    }

    public void Dispose()
    {
        Texture?.Dispose();
        if (_framebuffer == 0 && _depthStencil == 0 && _texture == 0)
            return;
        try
        {
            using (_context.GlContext.EnsureCurrent())
            {
                var gl = _context.GlContext.GlInterface;
                if (_framebuffer != 0)
                    gl.DeleteFramebuffer(_framebuffer);
                if (_depthStencil != 0)
                    gl.DeleteRenderbuffer(_depthStencil);
                if (_texture != 0)
                    gl.DeleteTexture(_texture);
            }
        }
        catch
        {
            // The context is lost along with its objects.
        }
        _framebuffer = _depthStencil = _texture = 0;
    }
}
