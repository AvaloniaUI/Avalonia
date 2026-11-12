using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.OpenGL;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using NImpeller;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.Impeller;

/// <summary>Texture sharing for OpenGL controls. External image handles and semaphores are not supported.</summary>
internal sealed class ImpellerTextureSharingFeature : IOpenGlTextureSharingRenderInterfaceContextFeature
{
    private readonly ImpellerGlBackendContext _context;

    public ImpellerTextureSharingFeature(ImpellerGlBackendContext context) => _context = context;

    public bool CanCreateSharedContext => _context.GlContext.CanCreateSharedContext;

    public IGlContext? CreateSharedContext(IEnumerable<GlVersion>? preferredVersions = null) =>
        _context.GlContext.CreateSharedContext(preferredVersions);

    public ICompositionImportableOpenGlSharedTexture CreateSharedTextureForComposition(IGlContext context, PixelSize size)
    {
        if (!context.IsSharedWith(_context.GlContext))
            throw new InvalidOperationException("Contexts do not belong to the same share group");

        using (context.EnsureCurrent())
        {
            var gl = context.GlInterface;
            gl.GetIntegerv(GL_TEXTURE_BINDING_2D, out var oldTexture);
            var tex = gl.GenTexture();

            var format = context.Version.Type == GlProfileType.OpenGLES && context.Version.Major == 2
                ? GL_RGBA
                : GL_RGBA8;

            gl.BindTexture(GL_TEXTURE_2D, tex);
            gl.TexImage2D(GL_TEXTURE_2D, 0, format, size.Width, size.Height, 0, GL_RGBA, GL_UNSIGNED_BYTE, IntPtr.Zero);
            gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
            gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
            gl.BindTexture(GL_TEXTURE_2D, oldTexture);

            return new ImpellerSharedTextureForComposition(context, tex, format, size);
        }
    }
}

internal sealed class ImpellerSharedTextureForComposition : ICompositionImportableOpenGlSharedTexture
{
    private readonly object _lock = new();

    public ImpellerSharedTextureForComposition(IGlContext context, int textureId, int internalFormat, PixelSize size)
    {
        Context = context;
        TextureId = textureId;
        InternalFormat = internalFormat;
        Size = size;
    }

    public IGlContext Context { get; }
    public int TextureId { get; private set; }
    public int InternalFormat { get; }
    public PixelSize Size { get; }

    public void Dispose(IGlContext context)
    {
        lock (_lock)
        {
            if (TextureId == 0)
                return;
            try
            {
                using (context.EnsureCurrent())
                    context.GlInterface.DeleteTexture(TextureId);
            }
            catch
            {
                // Context is likely dead; ignore.
            }
            TextureId = 0;
        }
    }

    public void Dispose() => Dispose(Context);
}

internal sealed class ImpellerExternalObjectsFeature : IExternalObjectsRenderInterfaceContextFeature
{
    private readonly ImpellerGlBackendContext _context;
    private int _readFbo;
    private int _drawFbo;

    public ImpellerExternalObjectsFeature(ImpellerGlBackendContext context) => _context = context;

    public IReadOnlyList<string> SupportedImageHandleTypes => Array.Empty<string>();
    public IReadOnlyList<string> SupportedSemaphoreTypes => Array.Empty<string>();

    public IPlatformRenderInterfaceImportedImage ImportImage(IPlatformHandle handle,
        PlatformGraphicsExternalImageProperties properties) =>
        throw new NotSupportedException("The Impeller backend only supports OpenGL shared textures, not external image handles.");

    public IPlatformRenderInterfaceImportedImage ImportImage(ICompositionImportableSharedGpuContextImage image)
    {
        var shared = (ImpellerSharedTextureForComposition)image;
        if (!shared.Context.IsSharedWith(_context.GlContext))
            throw new InvalidOperationException("Contexts do not belong to the same share group");
        var gl = _context.GlContext.GlInterface;
        if (_readFbo == 0) _readFbo = gl.GenFramebuffer();
        if (_drawFbo == 0) _drawFbo = gl.GenFramebuffer();
        return new ImpellerImportedImage(_context, _readFbo, _drawFbo, shared);
    }

    public IPlatformRenderInterfaceImportedSemaphore ImportSemaphore(IPlatformHandle handle) =>
        throw new NotSupportedException("The Impeller backend does not support importing external semaphores.");

    public CompositionGpuImportedImageSynchronizationCapabilities GetSynchronizationCapabilities(string imageHandleType) =>
        default;

    public byte[]? DeviceUuid => null;
    public byte[]? DeviceLuid => null;
}

internal sealed class ImpellerImportedImage : IPlatformRenderInterfaceImportedImage
{
    private readonly ImpellerGlBackendContext _context;
    private readonly int _readFbo;
    private readonly int _drawFbo;
    private readonly ImpellerSharedTextureForComposition _shared;

    public ImpellerImportedImage(ImpellerGlBackendContext context, int readFbo, int drawFbo,
        ImpellerSharedTextureForComposition shared)
    {
        _context = context;
        _readFbo = readFbo;
        _drawFbo = drawFbo;
        _shared = shared;
    }

    public void Dispose() => _shared.Dispose(_context.GlContext);

    public IBitmapImpl SnapshotWithAutomaticSync()
    {
        using (_context.GlContext.EnsureCurrent())
            return TakeSnapshot();
    }

    public IBitmapImpl SnapshotWithKeyedMutex(uint acquireIndex, uint releaseIndex) =>
        throw new NotSupportedException("Keyed-mutex sync is only supported for external image handles.");

    public IBitmapImpl SnapshotWithSemaphores(IPlatformRenderInterfaceImportedSemaphore waitForSemaphore,
        IPlatformRenderInterfaceImportedSemaphore signalSemaphore) =>
        throw new NotSupportedException("Semaphore sync is only supported for external image handles.");

    public IBitmapImpl SnapshotWithTimelineSemaphores(IPlatformRenderInterfaceImportedSemaphore waitForSemaphore,
        ulong waitForValue, IPlatformRenderInterfaceImportedSemaphore signalSemaphore, ulong signalValue) =>
        throw new NotSupportedException("Timeline-semaphore sync is only supported for external image handles.");

    // Copies the texture so the snapshot stays stable, flipping it: GL textures are bottom-up, Impeller samples top-down.
    private IBitmapImpl TakeSnapshot()
    {
        var gl = _context.GlContext.GlInterface;
        var w = _shared.Size.Width;
        var h = _shared.Size.Height;

        gl.GetIntegerv(GL_FRAMEBUFFER_BINDING, out var oldFbo);

        var dest = gl.GenTexture();
        gl.BindTexture(GL_TEXTURE_2D, dest);
        gl.TexImage2D(GL_TEXTURE_2D, 0, _shared.InternalFormat, w, h, 0, GL_RGBA, GL_UNSIGNED_BYTE, IntPtr.Zero);
        gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
        gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
        gl.BindTexture(GL_TEXTURE_2D, 0);

        gl.BindFramebuffer(GL_READ_FRAMEBUFFER, _readFbo);
        gl.FramebufferTexture2D(GL_READ_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, _shared.TextureId, 0);
        gl.BindFramebuffer(GL_DRAW_FRAMEBUFFER, _drawFbo);
        gl.FramebufferTexture2D(GL_DRAW_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, dest, 0);

        gl.BlitFramebuffer(0, h, w, 0, 0, 0, w, h, GL_COLOR_BUFFER_BIT, GL_NEAREST);

        gl.FramebufferTexture2D(GL_READ_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, 0, 0);
        gl.FramebufferTexture2D(GL_DRAW_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, 0, 0);
        gl.BindFramebuffer(GL_FRAMEBUFFER, oldFbo);
        gl.Flush();

        return new ImpellerSharedBitmap(_context, dest, _shared.Size);
    }
}

/// <summary>An imported GL texture.</summary>
internal sealed class ImpellerSharedBitmap : IBitmapImpl, IImpellerTextureSource
{
    private readonly ImpellerGlBackendContext _context;
    private readonly int _texture;
    private ImpellerTexture? _impellerTexture;

    public ImpellerSharedBitmap(ImpellerGlBackendContext context, int texture, PixelSize size)
    {
        _context = context;
        _texture = texture;
        PixelSize = size;
    }

    public Vector Dpi => new(96, 96);
    public PixelSize PixelSize { get; }
    public int Version => 1;

    public ImpellerTexture? GetTexture(ImpellerBackendContext context)
    {
        if (context != _context)
            return null;
        _impellerTexture ??= context.ImpellerContext.TextureCreateWithOpenGLTextureHandleNew(
            new ImpellerTextureDescriptor
            {
                Pixel_format = ImpellerPixelFormat.kImpellerPixelFormatRGBA8888,
                Size = new ImpellerISize(PixelSize.Width, PixelSize.Height),
                Mip_count = 1
            }, (ulong)_texture);
        return _impellerTexture;
    }

    public void Save(Stream stream, BitmapEncoderOptions options) =>
        throw new NotSupportedException("Saving an imported GPU texture is not supported.");

    public void Dispose()
    {
        _impellerTexture?.Dispose();
        _impellerTexture = null;
        try
        {
            using (_context.GlContext.EnsureCurrent())
                _context.GlContext.GlInterface.DeleteTexture(_texture);
        }
        catch
        {
            // Context is likely dead; ignore.
        }
    }
}
