using System;
using System.Collections.Generic;
using Avalonia.Metal;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using SkiaSharp;

namespace Avalonia.Skia.Metal;

class SkiaMetalExternalObjectsFeature(SkiaMetalGpu gpu, IMetalExternalObjectsFeature inner) : IExternalObjectsRenderInterfaceContextFeature
{
    public IReadOnlyList<string> SupportedImageHandleTypes => inner.SupportedImageHandleTypes;
    
    public IReadOnlyList<string> SupportedSemaphoreTypes => inner.SupportedSemaphoreTypes;
    
    class ImportedSemaphore(IMetalSharedEvent ev) : IPlatformRenderInterfaceImportedSemaphore
    {
        public IMetalSharedEvent Event => ev;
        public void Dispose() => ev.Dispose();
    }
    
    class ImportedImage(SkiaMetalGpu gpu, IMetalExternalObjectsFeature feature, IMetalExternalTexture texture,
        SKColorType colorType, bool topLeftOrigin) : IPlatformRenderInterfaceImportedImage
    {
        public void Dispose() => texture.Dispose();

        public IBitmapImpl SnapshotWithKeyedMutex(uint acquireIndex, uint releaseIndex) => throw new System.NotSupportedException();

        public IBitmapImpl SnapshotWithSemaphores(IPlatformRenderInterfaceImportedSemaphore waitForSemaphore,
            IPlatformRenderInterfaceImportedSemaphore signalSemaphore) =>
            throw new System.NotSupportedException();

        public IBitmapImpl SnapshotWithTimelineSemaphores(IPlatformRenderInterfaceImportedSemaphore waitForSemaphore,
            ulong waitForValue, IPlatformRenderInterfaceImportedSemaphore signalSemaphore, ulong signalValue)
        {
            gpu.GrContext.Flush(true, false);
            feature.SubmitWait(((ImportedSemaphore)waitForSemaphore).Event, waitForValue);
            using var backendTarget = new GRBackendRenderTarget(texture.Width, texture.Height, new GRMtlTextureInfo(texture.Handle));
            
            using var surface = SKSurface.Create(gpu.GrContext, backendTarget,
                topLeftOrigin ? GRSurfaceOrigin.TopLeft : GRSurfaceOrigin.BottomLeft,
                colorType);
            var rv = new ImmutableBitmap(surface.Snapshot());
            gpu.GrContext.Flush();
            feature.SubmitSignal(((ImportedSemaphore)signalSemaphore).Event, signalValue);
            return rv;
        }

        public IBitmapImpl SnapshotWithAutomaticSync() => throw new System.NotSupportedException();
    }
    
    public IPlatformRenderInterfaceImportedImage ImportImage(IPlatformHandle handle,
        PlatformGraphicsExternalImageProperties properties)
    {
        var format = properties.Format switch
        {
            PlatformGraphicsExternalImageFormat.R8G8B8A8UNorm => SKColorType.Rgba8888,
            PlatformGraphicsExternalImageFormat.B8G8R8A8UNorm => SKColorType.Bgra8888,
            _ => throw new NotSupportedException("Pixel format is not supported")
        };

        return new ImportedImage(gpu, inner, inner.ImportImage(handle, properties), format,
            properties.TopLeftOrigin);
    }

    public IPlatformRenderInterfaceImportedImage ImportImage(ICompositionImportableSharedGpuContextImage image) => throw new System.NotSupportedException();

    public IPlatformRenderInterfaceImportedSemaphore ImportSemaphore(IPlatformHandle handle) => new ImportedSemaphore(inner.ImportSharedEvent(handle));

    public CompositionGpuImportedImageSynchronizationCapabilities GetSynchronizationCapabilities(string imageHandleType)
        => inner.GetSynchronizationCapabilities(imageHandleType);

    public byte[]? DeviceUuid { get; } = null;
    public byte[]? DeviceLuid => inner.DeviceLuid;
}
