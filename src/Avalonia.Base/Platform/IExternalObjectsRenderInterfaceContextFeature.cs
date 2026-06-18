using System;
using System.Collections.Generic;
using Avalonia.Metadata;
using Avalonia.Rendering.Composition;

namespace Avalonia.Platform;

[Unstable]
public interface IExternalObjectsRenderInterfaceContextFeature
{
    /// <summary>
    /// Returns the list of image handle types supported by the current GPU backend, see <see cref="KnownPlatformGraphicsExternalImageHandleTypes"/>
    /// </summary>
    IReadOnlyList<string> SupportedImageHandleTypes { get; }
    
    /// <summary>
    /// Returns the list of semaphore types supported by the current GPU backend, see <see cref="KnownPlatformGraphicsExternalSemaphoreHandleTypes"/>
    /// </summary>
    IReadOnlyList<string> SupportedSemaphoreTypes { get; }

    IPlatformRenderInterfaceImportedImage ImportImage(IPlatformHandle handle,
        PlatformGraphicsExternalImageProperties properties);

    IPlatformRenderInterfaceImportedImage ImportImage(ICompositionImportableSharedGpuContextImage image);

    IPlatformRenderInterfaceImportedSemaphore ImportSemaphore(IPlatformHandle handle);
    
    CompositionGpuImportedImageSynchronizationCapabilities GetSynchronizationCapabilities(string imageHandleType);
    public byte[]? DeviceUuid { get; }
    public byte[]? DeviceLuid { get; }
}

/// <summary>
/// This interface allows proper management of ref-counted platform handles.
/// If we immediately wrap the handle, the caller can destroy its copy immediately after the call
/// This is needed for MoltenVK-based users that can e.g. get an MTLSharedEvent from a VkSemaphore.
/// This does NOT actually increase the ref-counter of MTLSharedEvent, since it's declared as
/// __unsafe_unretained in vulkan headers.
/// Same happens with exporting an IOSurfaceRef from a VkImage.
/// So in a case when the VkSemaphore or VkImage is destroyed, the "handle" which is actually a pointer
/// will be pointing to a dead object.
/// To prevent this we need to increase the reference counter in a handle-specific means
/// synchronously before returning control back to the user.
///
/// This is not needed for fds or DXGI handles, since those are _created_ on demand as proper NT handles
/// </summary>
[Unstable, NotClientImplementable]
public interface IExternalObjectsHandleWrapRenderInterfaceContextFeature
{
    IExternalObjectsWrappedGpuHandle? WrapImageHandleOnAnyThread(IPlatformHandle handle,
        PlatformGraphicsExternalImageProperties properties);
    IExternalObjectsWrappedGpuHandle? WrapSemaphoreHandleOnAnyThread(IPlatformHandle handle);

}

[Unstable, NotClientImplementable]
public interface IExternalObjectsWrappedGpuHandle : IPlatformHandle, IDisposable
{
    
}

[Unstable]
public interface IPlatformRenderInterfaceImportedObject : IDisposable
{
    
}

[Unstable]
public interface IPlatformRenderInterfaceImportedImage : IPlatformRenderInterfaceImportedObject
{
    IBitmapImpl SnapshotWithKeyedMutex(uint acquireIndex, uint releaseIndex);

    IBitmapImpl SnapshotWithSemaphores(IPlatformRenderInterfaceImportedSemaphore waitForSemaphore,
        IPlatformRenderInterfaceImportedSemaphore signalSemaphore);

    IBitmapImpl SnapshotWithTimelineSemaphores(
        IPlatformRenderInterfaceImportedSemaphore waitForSemaphore, ulong waitForValue,
        IPlatformRenderInterfaceImportedSemaphore signalSemaphore, ulong signalValue);

    IBitmapImpl SnapshotWithAutomaticSync();
}

[Unstable]
public interface IPlatformRenderInterfaceImportedSemaphore : IPlatformRenderInterfaceImportedObject
{
    
}
