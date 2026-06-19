using System;
using System.Collections.Generic;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;

namespace Avalonia.OpenGL;

[PrivateApi]
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

[PrivateApi]
public interface IGlExternalSemaphore : IDisposable
{
    void WaitSemaphore(IGlExternalImageTexture texture);
    void SignalSemaphore(IGlExternalImageTexture texture);
    void WaitTimelineSemaphore(IGlExternalImageTexture texture, ulong value);
    void SignalTimelineSemaphore(IGlExternalImageTexture texture, ulong value);
}

[PrivateApi]
public interface IGlExportableExternalSemaphore : IGlExternalSemaphore
{
    IPlatformHandle GetHandle();
}

[PrivateApi]
public interface IGlExternalImageTexture : IDisposable
{
    void AcquireKeyedMutex(uint key);
    void ReleaseKeyedMutex(uint key);
    int TextureId { get; }
    int InternalFormat { get; }
    /// <summary>
    /// GL_TEXTURE_2D or GL_TEXTURE_RECTANGLE
    /// </summary>
    public int TextureType { get; }
    
    PlatformGraphicsExternalImageProperties Properties { get; }
}

[PrivateApi]
public interface IGlExportableExternalImageTexture : IGlExternalImageTexture
{
    IPlatformHandle GetHandle();
}
