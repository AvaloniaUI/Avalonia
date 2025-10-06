using System;
using System.Collections.Generic;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;

namespace Avalonia.Metal;

[PrivateApi]
public interface IMetalExternalObjectsFeature
{
    IReadOnlyList<string> SupportedImageHandleTypes { get; }
    IReadOnlyList<string> SupportedSemaphoreTypes { get; }
    byte[]? DeviceLuid { get; }
    CompositionGpuImportedImageSynchronizationCapabilities GetSynchronizationCapabilities(string imageHandleType);
    IMetalExternalTexture ImportImage(IPlatformHandle handle, PlatformGraphicsExternalImageProperties properties);
    IMetalSharedEvent ImportSharedEvent(IPlatformHandle handle);

    void SubmitWait(IMetalSharedEvent @event, ulong waitForValue);
    void SubmitSignal(IMetalSharedEvent @event, ulong signalValue);
}

[PrivateApi]
public interface IMetalExternalTexture : IDisposable
{
    int Width { get; }
    int Height { get; }
    int Samples { get; }
    IntPtr Handle { get; }
}

[PrivateApi]
public interface IMetalSharedEvent : IDisposable
{
    IntPtr Handle { get; }
}
