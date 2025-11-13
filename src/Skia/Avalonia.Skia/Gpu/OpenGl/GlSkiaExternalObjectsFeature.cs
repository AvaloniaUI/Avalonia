using System;
using System.Collections.Generic;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using SkiaSharp;

namespace Avalonia.Skia;

internal class GlSkiaExternalObjectsFeature : IExternalObjectsRenderInterfaceContextFeature
{
    private readonly GlSkiaGpu _gpu;
    private readonly IGlContextExternalObjectsFeature? _feature;

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
            return new GlSkiaImportedImage(_gpu, image);
        }
    }

    public IPlatformRenderInterfaceImportedImage ImportImage(ICompositionImportableSharedGpuContextImage image)
    {
        var img = (GlSkiaSharedTextureForComposition)image;
        if (!img.Context.IsSharedWith(_gpu.GlContext))
            throw new InvalidOperationException("Contexts do not belong to the same share group");
        
        return new GlSkiaImportedImage(_gpu, img);
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

    public byte[]? DeviceUuid => _feature?.DeviceUuid;
    public byte[]? DeviceLuid => _feature?.DeviceLuid;
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
    private readonly IGlExternalImageTexture? _image;

    private SnapshotBuffer? _snapshotBuffer;

    public GlSkiaImportedImage(GlSkiaGpu gpu, IGlExternalImageTexture image)
    {
        _gpu = gpu;
        _image = image;
    }

    public GlSkiaImportedImage(GlSkiaGpu gpu, GlSkiaSharedTextureForComposition sharedTexture)
    {
        _gpu = gpu;
        _sharedTexture = sharedTexture;
    }

    public void Dispose()
    {
        _snapshotBuffer?.Dispose();
        _snapshotBuffer = null;

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

        var image = SKImage.FromTexture(_gpu.GrContext, texture, origin, SKColorType.Rgba8888);
        if (image is not null)
            return image;

        using var unformatted = new GRBackendTexture(width, height, false,
            new GRGlTextureInfo((uint)GlConsts.GL_TEXTURE_2D, (uint)textureId));

        return SKImage.FromTexture(_gpu.GrContext, unformatted, origin, SKColorType.Rgba8888);
    }
    
    IBitmapImpl TakeSnapshot()
    {
        var width = _image?.Properties.Width ?? _sharedTexture!.Size.Width;
        var height = _image?.Properties.Height ?? _sharedTexture!.Size.Height;
        var internalFormat = _image?.InternalFormat ?? _sharedTexture!.InternalFormat;
        var textureId = _image?.TextureId ?? _sharedTexture!.TextureId;
        var topLeft = _image?.Properties.TopLeftOrigin ?? false;
        var textureType = _image?.TextureType ?? GlConsts.GL_TEXTURE_2D;

        using var image = TryCreateImage(textureType, textureId, internalFormat, width, height, topLeft);
        if (image is null)
            throw new OpenGlException("Unable to consume provided texture");

        var snapshotBuffer = RentSnapshotBuffer(width, height);

        var snapshotCanvas = snapshotBuffer.Surface.Canvas;
        snapshotCanvas.Clear();
        snapshotCanvas.DrawImage(image, SKRect.Create(width, height));
        snapshotCanvas.Flush();

        var snapshotImage = snapshotBuffer.Surface.Snapshot();
        var context = _gpu.GlContext;

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
                ReturnSnapshotBuffer(snapshotBuffer);
            }
        });

        _gpu.GrContext.Flush();
        _gpu.GlContext.GlInterface.Flush();
        return rv;
    }

    private SnapshotBuffer RentSnapshotBuffer(int width, int height)
    {
        if (_snapshotBuffer is not null && (_snapshotBuffer.Width != width || _snapshotBuffer.Height != height))
        {
            _snapshotBuffer.Dispose();
            _snapshotBuffer = null;
        }

        if (_snapshotBuffer is not null)
        {
            var result = _snapshotBuffer;
            _snapshotBuffer = null;
            return result;
        }

        return new SnapshotBuffer(width, height, SKSurface.Create(new SKImageInfo(width, height, SKColorType.Bgra8888)));
    }

    private void ReturnSnapshotBuffer(SnapshotBuffer buffer)
    {
        if (_snapshotBuffer is null)
            _snapshotBuffer = buffer;
        else
            buffer.Dispose();
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

    private sealed class SnapshotBuffer(int width, int height, SKSurface surface)
        : IDisposable
    {
        public int Width { get; } = width;
        public int Height { get; } = height;
        public SKSurface Surface { get; } = surface;

        public void Dispose()
            => Surface.Dispose();
    }
}
