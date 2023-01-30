using System;
using System.Collections.Generic;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;

namespace Avalonia.OpenGL;

public interface IGlContextExternalObjectsFeature
{
    IReadOnlyList<string> SupportedImportableExternalImageTypes { get; }
    IReadOnlyList<string> SupportedExportableExternalImageTypes { get; }
    IReadOnlyList<string> SupportedImportableExternalSemaphoreTypes { get; }
    IReadOnlyList<string> SupportedExportableExternalSemaphoreTypes { get; }
    IReadOnlyList<PlatformGraphicsExternalImageFormat> GetSupportedFormatsForExternalMemoryType(string type);

    IGlExportableExternalImageTexture CreateImage(string type,PixelSize size, PlatformGraphicsExternalImageFormat format);

    IGlExportableExternalImageTexture CreateSemaphore(string type);
    IGlExternalImageTexture ImportImage(IPlatformHandle handle, PlatformGraphicsExternalImageProperties properties);
    IGlExternalSemaphore ImportSemaphore(IPlatformHandle handle);
    CompositionGpuImportedImageSynchronizationCapabilities GetSynchronizationCapabilities(string imageHandleType);
    public byte[]? DeviceLuid { get; }
    public byte[]? DeviceUuid { get; }
}

public interface IGlExternalSemaphore : IDisposable
{
    void WaitSemaphore(IGlExternalImageTexture texture);
    void SignalSemaphore(IGlExternalImageTexture texture);
}

public interface IGlExportableExternalSemaphore : IGlExternalSemaphore
{
    IPlatformHandle GetHandle();
}

public interface IGlExternalImageTexture : IDisposable
{
    void AcquireKeyedMutex(uint key);
    void ReleaseKeyedMutex(uint key);
    int TextureId { get; }
    int InternalFormat { get; }
    
    PlatformGraphicsExternalImageProperties Properties { get; }
}

public interface IGlExportableExternalImageTexture : IGlExternalImageTexture
{
    IPlatformHandle GetHandle();
}
