using System;
using System.Threading.Tasks;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Threading;

namespace Avalonia.Rendering.Composition;

public sealed class CompositionDrawingSurface : CompositionSurface, IDisposable
{
    internal new ServerCompositionDrawingSurface Server => (ServerCompositionDrawingSurface)base.Server!;
    internal CompositionDrawingSurface(Compositor compositor) : base(compositor, new ServerCompositionDrawingSurface(compositor.Server))
    {
    }

    /// <summary>
    /// Updates the surface contents using an imported memory image using a keyed mutex as the means of synchronization
    /// </summary>
    /// <param name="image">GPU image with new surface contents</param>
    /// <param name="acquireIndex">The mutex key to wait for before accessing the image</param>
    /// <param name="releaseIndex">The mutex key to release for after accessing the image </param>
    /// <returns>A task that completes when update operation is completed and user code is free to destroy or dispose the image</returns>
    public Task UpdateWithKeyedMutexAsync(ICompositionImportedGpuImage image, uint acquireIndex, uint releaseIndex)
    {
        var img = (CompositionImportedGpuImage)image;
        return Compositor.InvokeServerJobAsync(() => Server.UpdateWithKeyedMutex(img, acquireIndex, releaseIndex));
    }

    /// <summary>
    /// Updates the surface contents using an imported memory image using a semaphore pair as the means of synchronization
    /// </summary>
    /// <param name="image">GPU image with new surface contents</param>
    /// <param name="waitForSemaphore">The semaphore to wait for before accessing the image</param>
    /// <param name="signalSemaphore">The semaphore to signal after accessing the image</param>
    /// <returns>A task that completes when update operation is completed and user code is free to destroy or dispose the image</returns>
    public Task UpdateWithSemaphoresAsync(ICompositionImportedGpuImage image,
        ICompositionImportedGpuSemaphore waitForSemaphore,
        ICompositionImportedGpuSemaphore signalSemaphore)
    {
        var img = (CompositionImportedGpuImage)image;
        var wait = (CompositionImportedGpuSemaphore)waitForSemaphore;
        var signal = (CompositionImportedGpuSemaphore)signalSemaphore;
        return Compositor.InvokeServerJobAsync(() => Server.UpdateWithSemaphores(img, wait, signal));
    }

    /// <summary>
    /// Updates the surface contents using an unspecified automatic means of synchronization
    /// provided by the underlying platform
    /// </summary>
    /// <param name="image">GPU image with new surface contents</param>
    /// <returns>A task that completes when update operation is completed and user code is free to destroy or dispose the image</returns>
    public Task UpdateAsync(ICompositionImportedGpuImage image)
    {
        var img = (CompositionImportedGpuImage)image;
        return Compositor.InvokeServerJobAsync(() => Server.UpdateWithAutomaticSync(img));
    }

    ~CompositionDrawingSurface()
    {
        Compositor.Dispatcher.Post(Dispose);
    }

    public new void Dispose() => base.Dispose();
}
