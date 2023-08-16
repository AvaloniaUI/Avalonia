using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition;

[NotClientImplementable]
public interface ICompositionGpuInterop
{
    /// <summary>
    /// Returns the list of image handle types supported by the current GPU backend, see <see cref="KnownPlatformGraphicsExternalImageHandleTypes"/>
    /// </summary>
    IReadOnlyList<string> SupportedImageHandleTypes { get; }
    
    /// <summary>
    /// Returns the list of semaphore types supported by the current GPU backend, see <see cref="KnownPlatformGraphicsExternalSemaphoreHandleTypes"/>
    /// </summary>
    IReadOnlyList<string> SupportedSemaphoreTypes { get; }

    /// <summary>
    /// Returns the supported ways to synchronize access to the imported GPU image
    /// </summary>
    /// <returns></returns>
    CompositionGpuImportedImageSynchronizationCapabilities GetSynchronizationCapabilities(string imageHandleType);
    
    /// <summary>
    /// Asynchronously imports a texture. The returned object is immediately usable.
    /// </summary>
    ICompositionImportedGpuImage ImportImage(IPlatformHandle handle,
        PlatformGraphicsExternalImageProperties properties);

    /// <summary>
    /// Asynchronously imports a texture. The returned object is immediately usable.
    /// If import operation fails, the caller is responsible for destroying the handle
    /// </summary>
    /// <param name="image">An image that belongs to the same GPU context or the same GPU context sharing group as one used by compositor</param>
    ICompositionImportedGpuImage ImportImage(ICompositionImportableSharedGpuContextImage image);

    /// <summary>
    /// Asynchronously imports a semaphore object. The returned object is immediately usable.
    /// If import operation fails, the caller is responsible for destroying the handle
    /// </summary>
    ICompositionImportedGpuSemaphore ImportSemaphore(IPlatformHandle handle);
    
    /// <summary>
    /// Asynchronously imports a semaphore object. The returned object is immediately usable.
    /// </summary>
    /// <param name="image">A semaphore that belongs to the same GPU context or the same GPU context sharing group as one used by compositor</param>
    ICompositionImportedGpuImage ImportSemaphore(ICompositionImportableSharedGpuContextSemaphore image);
    
    /// <summary>
    /// Indicates if the device context this instance is associated with is no longer available
    /// </summary>
    public bool IsLost { get; }
    
    /// <summary>
    /// The LUID of the graphics adapter used by the compositor
    /// </summary>
    public byte[]? DeviceLuid { get; set; }
    
    /// <summary>
    /// The UUID of the graphics adapter used by the compositor
    /// </summary>
    public byte[]? DeviceUuid { get; set; }
}

[Flags]
public enum CompositionGpuImportedImageSynchronizationCapabilities
{
    /// <summary>
    /// Pre-render and after-render semaphores must be provided alongside with the image
    /// </summary>
    Semaphores = 1,
    /// <summary>
    /// Image must be created with D3D11_RESOURCE_MISC_SHARED_KEYEDMUTEX or in other compatible way
    /// </summary>
    KeyedMutex = 2,
    /// <summary>
    /// Synchronization and ordering is somehow handled by the underlying platform
    /// </summary>
    Automatic = 4
}

/// <summary>
/// An imported GPU object that's usable by composition APIs 
/// </summary>
[NotClientImplementable]
public interface ICompositionGpuImportedObject : IAsyncDisposable
{
    /// <summary>
    /// Tracks the import status of the object. Once the task is completed,
    /// the user code is allowed to free the resource owner in case when a non-owning
    /// sharing handle was used.
    /// </summary>
    Task ImportCompleted { get; }
    
    /// <inheritdoc cref="ImportCompleted"/>
    /// <seealso cref="ImportCompleted">ImportCompleted (recommended replacement)</seealso>
    [Obsolete("Please use ICompositionGpuImportedObject.ImportCompleted instead")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    Task ImportCompeted { get; }
    
    /// <summary>
    /// Indicates if the device context this instance is associated with is no longer available
    /// </summary>
    bool IsLost { get; }
}

/// <summary>
/// An imported GPU image object that's usable by composition APIs 
/// </summary>
[NotClientImplementable]
public interface ICompositionImportedGpuImage : ICompositionGpuImportedObject
{

}

/// <summary>
/// An imported GPU semaphore object that's usable by composition APIs 
/// </summary>
[NotClientImplementable]
public interface ICompositionImportedGpuSemaphore : ICompositionGpuImportedObject
{

}

/// <summary>
/// An GPU object descriptor obtained from a context from the same share group as one used by the compositor
/// </summary>
[NotClientImplementable]
public interface ICompositionImportableSharedGpuContextObject : IDisposable
{
}

/// <summary>
/// An GPU image descriptor obtained from a context from the same share group as one used by the compositor
/// </summary>
[NotClientImplementable]
public interface ICompositionImportableSharedGpuContextImage : IDisposable
{
}

/// <summary>
/// An GPU semaphore descriptor obtained from a context from the same share group as one used by the compositor
/// </summary>
[NotClientImplementable]
public interface ICompositionImportableSharedGpuContextSemaphore : IDisposable
{
}

