using System;
using System.Collections.Generic;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using SkiaSharp;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.Skia;

internal class GlSkiaExternalObjectsFeature : IExternalObjectsRenderInterfaceContextFeature, IGlSkiaFboProvider
{
    private readonly GlSkiaGpu _gpu;
    private readonly IGlContextExternalObjectsFeature? _feature;
    private int _fbo;

    public GlSkiaExternalObjectsFeature(GlSkiaGpu gpu, IGlContextExternalObjectsFeature? feature)
    {
        _gpu = gpu;
        _feature = feature;
    }

    public IReadOnlyList<string> SupportedImageHandleTypes => _feature?.SupportedImportableExternalImageTypes
                                                              ?? Array.Empty<string>();
    public IReadOnlyList<string> SupportedSemaphoreTypes => _feature?.SupportedImportableExternalSemaphoreTypes
                                                            ?? Array.Empty<string>();

    public IPlatformRenderInterfaceImportedImage ImportImage(IPlatformHandle handle,
        PlatformGraphicsExternalImageProperties properties)
    {
        if (_feature == null)
            throw new NotSupportedException("Importing this platform handle is not supported");
        using (_gpu.EnsureCurrent())
        {
            var image = _feature.ImportImage(handle, properties);
            return new GlSkiaImportedImage(_gpu, this, image);
        }
    }

    public IPlatformRenderInterfaceImportedImage ImportImage(ICompositionImportableSharedGpuContextImage image)
    {
        var img = (GlSkiaSharedTextureForComposition)image;
        if (!img.Context.IsSharedWith(_gpu.GlContext))
            throw new InvalidOperationException("Contexts do not belong to the same share group");
        
        return new GlSkiaImportedImage(_gpu, this, img);
    }

    public IPlatformRenderInterfaceImportedSemaphore ImportSemaphore(IPlatformHandle handle)
    {
        if (_feature == null)
            throw new NotSupportedException("Importing this platform handle is not supported");
        using (_gpu.EnsureCurrent())
        {
            var semaphore = _feature.ImportSemaphore(handle);
            return new GlSkiaImportedSemaphore(_gpu, semaphore);
        }
    }

    public CompositionGpuImportedImageSynchronizationCapabilities GetSynchronizationCapabilities(string imageHandleType)
        => _feature?.GetSynchronizationCapabilities(imageHandleType) ?? default;

    public int Fbo
    {
        get
        {
            if (_fbo == 0)
                _fbo = _gpu.GlContext.GlInterface.GenFramebuffer();

            return _fbo;
        }
    }

    public byte[]? DeviceUuid => _feature?.DeviceUuid;
    public byte[]? DeviceLuid => _feature?.DeviceLuid;
}

internal interface IGlSkiaFboProvider
{
    int Fbo { get; }
}

internal class GlSkiaImportedSemaphore : IPlatformRenderInterfaceImportedSemaphore
{
    private readonly GlSkiaGpu _gpu;
    public IGlExternalSemaphore Semaphore { get; }

    public GlSkiaImportedSemaphore(GlSkiaGpu gpu, IGlExternalSemaphore semaphore)
    {
        _gpu = gpu;
        Semaphore = semaphore;
    }

    public void Dispose() => Semaphore.Dispose();
}

internal class GlSkiaImportedImage : IPlatformRenderInterfaceImportedImage
{
    private readonly GlSkiaSharedTextureForComposition? _sharedTexture;
    private readonly GlSkiaGpu _gpu;
    private readonly IGlSkiaFboProvider _fboProvider;
    private readonly IGlExternalImageTexture? _image;

    public GlSkiaImportedImage(GlSkiaGpu gpu, IGlSkiaFboProvider fboProvider, IGlExternalImageTexture image)
    {
        _gpu = gpu;
        _fboProvider = fboProvider;
        _image = image;
    }

    public GlSkiaImportedImage(GlSkiaGpu gpu, IGlSkiaFboProvider fboProvider, GlSkiaSharedTextureForComposition sharedTexture)
    {
        _gpu = gpu;
        _fboProvider = fboProvider;
        _sharedTexture = sharedTexture;
    }

    public void Dispose()
    {
        _image?.Dispose();
        _sharedTexture?.Dispose(_gpu.GlContext);
    }

    SKColorType ConvertColorType(PlatformGraphicsExternalImageFormat format) =>
        format switch
        {
            PlatformGraphicsExternalImageFormat.B8G8R8A8UNorm => SKColorType.Bgra8888,
            PlatformGraphicsExternalImageFormat.R8G8B8A8UNorm => SKColorType.Rgba8888,
            _ => SKColorType.Rgba8888
        };

    SKImage? TryCreateImage(int target, int textureId, int format, int width, int height, bool topLeft)
    {
        var origin = topLeft ? GRSurfaceOrigin.TopLeft : GRSurfaceOrigin.BottomLeft;

        using var texture = new GRBackendTexture(width, height, false,
            new GRGlTextureInfo((uint)target, (uint)textureId, (uint)format));

        var image = SKImage.FromAdoptedTexture(_gpu.GrContext, texture, origin, SKColorType.Rgba8888);
        if (image is not null)
            return image;

        using var unformatted = new GRBackendTexture(width, height, false,
            new GRGlTextureInfo((uint)target, (uint)textureId));

        return SKImage.FromAdoptedTexture(_gpu.GrContext, unformatted, origin, SKColorType.Rgba8888);
    }

    IBitmapImpl TakeSnapshot()
    {
        var width = _image?.Properties.Width ?? _sharedTexture!.Size.Width;
        var height = _image?.Properties.Height ?? _sharedTexture!.Size.Height;
        var internalFormat = _image?.InternalFormat ?? _sharedTexture!.InternalFormat;
        var textureId = _image?.TextureId ?? _sharedTexture!.TextureId;
        var topLeft = _image?.Properties.TopLeftOrigin ?? false;
        var textureType = _image?.TextureType ?? GL_TEXTURE_2D;

        var context = _gpu.GlContext;
        var snapshotTextureId = CopyToNewTexture(textureType, textureId, internalFormat, width, height);
        var snapshotImage = TryCreateImage(textureType, snapshotTextureId, internalFormat, width, height, topLeft);

        if (snapshotImage is null)
        {
            context.GlInterface.DeleteTexture(snapshotTextureId);
            throw new OpenGlException("Unable to consume provided texture");
        }

        var rv = new ImmutableBitmap(snapshotImage, () =>
        {
            IDisposable? restoreContext = null;
            try
            {
                restoreContext = context.EnsureCurrent();
            }
            catch
            {
                // Ignore, context is likely dead
            }

            using (restoreContext)
            {
                snapshotImage.Dispose();
            }
        });

        _gpu.GrContext.Flush();
        context.GlInterface.Flush();
        return rv;
    }

    public IBitmapImpl SnapshotWithKeyedMutex(uint acquireIndex, uint releaseIndex)
    {
        if (_image is null)
        {
            throw new NotSupportedException("Only supported with an external image");
        }

        using (_gpu.EnsureCurrent())
        {
            _image.AcquireKeyedMutex(acquireIndex);
            try
            {
                return TakeSnapshot();
            }
            finally
            {
                _image.ReleaseKeyedMutex(releaseIndex);
            }
        }
    }

    public IBitmapImpl SnapshotWithSemaphores(IPlatformRenderInterfaceImportedSemaphore waitForSemaphore,
        IPlatformRenderInterfaceImportedSemaphore signalSemaphore)
    {
        if (_image is null)
        {
            throw new NotSupportedException("Only supported with an external image");
        }

        var wait = (GlSkiaImportedSemaphore)waitForSemaphore;
        var signal = (GlSkiaImportedSemaphore)signalSemaphore;
        using (_gpu.EnsureCurrent())
        {
            wait.Semaphore.WaitSemaphore(_image);
            try
            {
                return TakeSnapshot();
            }
            finally
            {
                signal.Semaphore.SignalSemaphore(_image);
            }
        }
    }

    public IBitmapImpl SnapshotWithTimelineSemaphores(IPlatformRenderInterfaceImportedSemaphore waitForSemaphore,
        ulong waitForValue, IPlatformRenderInterfaceImportedSemaphore signalSemaphore, ulong signalValue)
    {
        if (_image is null)
        {
            throw new NotSupportedException("Only supported with an external image");
        }

        var wait = (GlSkiaImportedSemaphore)waitForSemaphore;
        var signal = (GlSkiaImportedSemaphore)signalSemaphore;
        using (_gpu.EnsureCurrent())
        {
            wait.Semaphore.WaitTimelineSemaphore(_image, waitForValue);
            try
            {
                return TakeSnapshot();
            }
            finally
            {
                signal.Semaphore.SignalTimelineSemaphore(_image, signalValue);
            }
        }
    }

    public IBitmapImpl SnapshotWithAutomaticSync()
    {
        using (_gpu.EnsureCurrent())
            return TakeSnapshot();
    }

    private int CopyToNewTexture(int textureType, int sourceTextureId, int internalFormat, int width, int height)
    {
        var gl = _gpu.GlContext.GlInterface;

        using var _ = _gpu.EnsureCurrent();

        // Snapshot current values
        gl.GetIntegerv(GL_FRAMEBUFFER_BINDING, out var oldFbo);
        gl.GetIntegerv(GL_SCISSOR_TEST, out var oldScissorTest);

        // Bind source texture
        gl.BindFramebuffer(GL_FRAMEBUFFER, _fboProvider.Fbo);
        gl.Disable(GL_SCISSOR_TEST);
        gl.FramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, textureType, sourceTextureId, 0);

        // Create destination texture
        var destTextureId = gl.GenTexture();
        gl.BindTexture(textureType, destTextureId);
        gl.TexImage2D(textureType, 0, internalFormat, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, IntPtr.Zero);

        // Copy
        gl.CopyTexSubImage2D(textureType, 0, 0, 0, 0, 0, width, height);

        // Flush
        gl.FramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, textureType, 0, 0);
        gl.Flush();

        // Restore old values
        gl.BindFramebuffer(GL_FRAMEBUFFER, oldFbo);
        if (oldScissorTest != 0)
            gl.Enable(GL_SCISSOR_TEST);

        return destTextureId;
    }
}
