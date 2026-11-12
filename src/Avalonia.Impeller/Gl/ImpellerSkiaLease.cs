using System;
using System.Runtime.InteropServices;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Skia;
using NImpeller;
using SkiaSharp;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.Impeller;

internal sealed class ImpellerSkiaLeaseFeature : ISkiaSharpApiLeaseFeature
{
    private readonly ImpellerDrawingContext _context;
    private readonly ImpellerGlBackendContext _glBackend;

    private ImpellerSkiaLeaseFeature(ImpellerDrawingContext context, ImpellerGlBackendContext glBackend)
    {
        _context = context;
        _glBackend = glBackend;
    }

    // Skia renders into a GL texture on Impeller's context, so leases are only available on OpenGL.
    public static object? Get(ImpellerDrawingContext context, Type type) =>
        type == typeof(ISkiaSharpApiLeaseFeature) && context.Backend is ImpellerGlBackendContext gl
            ? new ImpellerSkiaLeaseFeature(context, gl)
            : null;

    public ISkiaSharpApiLease Lease() => new ImpellerSkiaLease(_context, _glBackend);
}

/// <summary>A Skia GPU context on Impeller's GL context.</summary>
internal sealed class SkiaGlContext : IDisposable
{
    private readonly IGlContext _glContext;

    // glColorMask / glStencilMaskSeparate aren't on Avalonia's GlInterface.
    private delegate void GlColorMask(byte r, byte g, byte b, byte a);
    private delegate void GlStencilMaskSeparate(int face, uint mask);
    private readonly GlColorMask _colorMask;
    private readonly GlStencilMaskSeparate _stencilMaskSeparate;

    public SkiaGlContext(IGlContext glContext)
    {
        _glContext = glContext;
        var gl = glContext.GlInterface;
        _colorMask = Marshal.GetDelegateForFunctionPointer<GlColorMask>(gl.GetProcAddress("glColorMask"));
        _stencilMaskSeparate =
            Marshal.GetDelegateForFunctionPointer<GlStencilMaskSeparate>(gl.GetProcAddress("glStencilMaskSeparate"));
        using (glContext.EnsureCurrent())
        {
            // Wayland prefers desktop OpenGL contexts; Skia must match the context's profile.
            var iface = glContext.Version.Type == GlProfileType.OpenGL
                ? GRGlInterface.CreateOpenGl(proc => gl.GetProcAddress(proc))
                : GRGlInterface.CreateGles(proc => gl.GetProcAddress(proc));
            GrContext = GRContext.CreateGl(iface)
                ?? throw new PlatformNotSupportedException("Failed to create a Skia GRContext for the lease.");
        }
    }

    public GRContext GrContext { get; }

    /// <summary>Resets the GL state Impeller expects, as <c>RenderPassGLES::ResetGLState</c> does.</summary>
    public void RestoreImpellerGlState(int framebuffer)
    {
        // Values commented out of Avalonia's GlConsts.
        const int glStencilTest = 0x0B90, glBlend = 0x0BE2, glDither = 0x0BD0, glFront = 0x0404;

        var gl = _glContext.GlInterface;
        gl.Disable(GL_SCISSOR_TEST);
        gl.Disable(GL_DEPTH_TEST);
        gl.Disable(glStencilTest);
        gl.Disable(GL_CULL_FACE);
        gl.Disable(glBlend);
        gl.Disable(glDither);
        _colorMask(1, 1, 1, 1);
        gl.DepthMask(1);
        _stencilMaskSeparate(glFront, 0xFFFFFFFF);
        _stencilMaskSeparate(GL_BACK, 0xFFFFFFFF);

        gl.UseProgram(0);
        gl.BindVertexArray(0);
        gl.BindBuffer(GL_ARRAY_BUFFER, 0);
        gl.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, 0);
        gl.ActiveTexture(GL_TEXTURE0);
        gl.BindFramebuffer(GL_FRAMEBUFFER, framebuffer);
    }

    // Bindings made by leased GL code, which Impeller's state restore unbinds.
    private int _leasedVertexArray, _leasedArrayBuffer, _leasedProgram;

    public void SaveLeasedBindings()
    {
        const int glVertexArrayBinding = 0x85B5, glArrayBufferBinding = 0x8894, glCurrentProgram = 0x8B8D;
        var gl = _glContext.GlInterface;
        gl.GetIntegerv(glVertexArrayBinding, out _leasedVertexArray);
        gl.GetIntegerv(glArrayBufferBinding, out _leasedArrayBuffer);
        gl.GetIntegerv(glCurrentProgram, out _leasedProgram);
    }

    public void RestoreLeasedBindings()
    {
        var gl = _glContext.GlInterface;
        gl.BindVertexArray(_leasedVertexArray);
        gl.BindBuffer(GL_ARRAY_BUFFER, _leasedArrayBuffer);
        gl.UseProgram(_leasedProgram);
    }

    public void Dispose() => GrContext.Dispose();
}

internal sealed class ImpellerSkiaLease : ISkiaSharpApiLease
{
    private readonly ImpellerDrawingContext _context;
    private readonly ImpellerGlBackendContext _glBackend;
    private readonly SkiaGlContext _skia;
    private readonly Rect _deviceBounds;
    private readonly int _glTexture;
    private readonly int _prevFbo;
    private readonly GRBackendTexture _backendTexture;
    private readonly SKSurface _surface;

    public ImpellerSkiaLease(ImpellerDrawingContext context, ImpellerGlBackendContext glBackend)
    {
        _context = context;
        _glBackend = glBackend;
        _skia = glBackend.GetExtension(() => new SkiaGlContext(glBackend.GlContext));

        var clip = context.CurrentClipDeviceBounds;
        var w = Math.Max(1, (int)Math.Ceiling(clip.Width));
        var h = Math.Max(1, (int)Math.Ceiling(clip.Height));
        _deviceBounds = new Rect(Math.Floor(clip.X), Math.Floor(clip.Y), w, h);

        var gl = glBackend.GlContext.GlInterface;
        gl.GetIntegerv(GL_FRAMEBUFFER_BINDING, out _prevFbo);

        _skia.GrContext.ResetContext();

        gl.GetIntegerv(GL_TEXTURE_BINDING_2D, out var prevTexture);
        _glTexture = gl.GenTexture();
        gl.BindTexture(GL_TEXTURE_2D, _glTexture);
        gl.TexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, w, h, 0, GL_RGBA, GL_UNSIGNED_BYTE, IntPtr.Zero);
        gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
        gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
        gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
        gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
        gl.BindTexture(GL_TEXTURE_2D, prevTexture);

        _backendTexture = new GRBackendTexture(w, h, false,
            new GRGlTextureInfo((uint)GL_TEXTURE_2D, (uint)_glTexture, (uint)GL_RGBA8));
        _surface = SKSurface.Create(_skia.GrContext, _backendTexture, GRSurfaceOrigin.TopLeft, SKColorType.Rgba8888)
            ?? throw new InvalidOperationException("Failed to create a GPU Skia lease surface.");
        _surface.Canvas.Clear(SKColors.Transparent);

        var matrix = SKMatrix.Concat(
            SKMatrix.CreateTranslation(-(float)_deviceBounds.X, -(float)_deviceBounds.Y),
            context.Transform.ToSKMatrix());
        _surface.Canvas.SetMatrix(matrix);
    }

    public SKCanvas SkCanvas => _surface.Canvas;
    public GRContext? GrContext => _skia.GrContext;
    public SKSurface? SkSurface => _surface;
    public double CurrentOpacity => 1.0;
    public ISkiaSharpPlatformGraphicsApiLease? TryLeasePlatformGraphicsApi()
    {
        if (_platformApiLeased)
            throw new InvalidOperationException("The platform graphics API is already leased.");
        return new PlatformApiLease(this);
    }

    private bool _platformApiLeased;

    private sealed class PlatformApiLease : ISkiaSharpPlatformGraphicsApiLease
    {
        private readonly ImpellerSkiaLease _parent;

        public PlatformApiLease(ImpellerSkiaLease parent)
        {
            _parent = parent;
            parent._surface.Canvas.Flush();
            parent._skia.GrContext.Flush();
            parent._platformApiLeased = true;

            // Leased GL code gets back its own bindings from the previous lease.
            var gl = parent._glBackend.GlContext.GlInterface;
            gl.GetIntegerv(GL_FRAMEBUFFER_BINDING, out var framebuffer);
            parent._skia.RestoreImpellerGlState(framebuffer);
            parent._skia.RestoreLeasedBindings();
        }

        public IPlatformGraphicsContext Context => _parent._glBackend.GlContext;

        public void Dispose()
        {
            _parent._platformApiLeased = false;
            _parent._skia.SaveLeasedBindings();

            var gl = _parent._glBackend.GlContext.GlInterface;
            gl.GetIntegerv(GL_FRAMEBUFFER_BINDING, out var framebuffer);
            _parent._skia.RestoreImpellerGlState(framebuffer);
            _parent._skia.GrContext.ResetContext();
        }
    }

    public void Dispose()
    {
        _surface.Canvas.Flush();
        _skia.GrContext.Flush();
        _skia.GrContext.ResetContext();

        _skia.RestoreImpellerGlState(_prevFbo);

        var w = (int)_deviceBounds.Width;
        var h = (int)_deviceBounds.Height;
        var texture = _glBackend.ImpellerContext.TextureCreateWithOpenGLTextureHandleNew(new ImpellerTextureDescriptor
        {
            Pixel_format = ImpellerPixelFormat.kImpellerPixelFormatRGBA8888,
            Size = new ImpellerISize(w, h),
            Mip_count = 1
        }, (ulong)_glTexture);

        var gl = _glBackend.GlContext.GlInterface;
        var surface = _surface;
        var backendTexture = _backendTexture;
        var glTexture = _glTexture;
        var resources = new DisposeAction(() =>
        {
            texture?.Dispose();
            surface.Dispose();
            backendTexture.Dispose();
            gl.DeleteTexture(glTexture);
        });

        if (texture is null)
            resources.Dispose();
        else
            _context.DrawDeviceTexture(texture, _deviceBounds, resources);
    }

    private sealed class DisposeAction(Action action) : IDisposable
    {
        public void Dispose() => action();
    }
}
