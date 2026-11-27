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

    private CompositionDrawingSurface(Compositor compositor, ServerCompositionDrawingSurface server)
        : base(compositor, server)
    {
    }

    /// <summary>
    /// Creates a proxy surface sharing the server-side surface with this one, but bound to
    /// <paramref name="compositor"/> and usable from that compositor's thread
    /// </summary>
    /// <remarks>
    /// This method can only be called from the thread of the compositor this surface belongs to.
    /// The server-side surface is kept alive until this surface and all of its proxies are disposed,
    /// so the returned proxy must be disposed too.
    /// The target compositor must share the server-side compositor with this surface's compositor,
    /// see <see cref="CompositorController"/>.
    /// </remarks>
    /// <param name="compositor">The compositor the proxy is created for</param>
    /// <returns>A proxy surface bound to <paramref name="compositor"/></returns>
    /// <exception cref="ObjectDisposedException">This surface is already disposed</exception>
    /// <exception cref="InvalidOperationException">
    /// The target compositor is the compositor of this surface or doesn't share its server-side compositor
    /// </exception>
    public CompositionDrawingSurface CreateProxyForCompositor(Compositor compositor)
    {
        Compositor.Dispatcher.VerifyAccess();
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(CompositionDrawingSurface));
        if (compositor == Compositor)
            throw new InvalidOperationException("The surface already belongs to the target compositor");
        if (compositor.Server != Compositor.Server)
            throw new InvalidOperationException(
                "The target compositor is attached to a different server-side compositor");
        Server.AddRef();
        return new CompositionDrawingSurface(compositor, Server);
    }

    private CompositionImportedGpuImage VerifyOwnership(ICompositionImportedGpuImage image)
    {
        var img = (CompositionImportedGpuImage)image;
        if (img.Compositor != Compositor)
            throw new InvalidOperationException(
                "The image was imported with an interop context belonging to a different Compositor");
        return img;
    }

    private CompositionImportedGpuSemaphore VerifyOwnership(ICompositionImportedGpuSemaphore semaphore)
    {
        var sem = (CompositionImportedGpuSemaphore)semaphore;
        if (sem.Compositor != Compositor)
            throw new InvalidOperationException(
                "The semaphore was imported with an interop context belonging to a different Compositor");
        return sem;
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
        var img = VerifyOwnership(image);
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
        var img = VerifyOwnership(image);
        var wait = VerifyOwnership(waitForSemaphore);
        var signal = VerifyOwnership(signalSemaphore);
        return Compositor.InvokeServerJobAsync(() => Server.UpdateWithSemaphores(img, wait, signal));
    }

    /// <summary>
    /// Updates the surface contents using an imported memory image using a semaphore pair as the means of synchronization
    /// </summary>
    /// <param name="image">GPU image with new surface contents</param>
    /// <param name="waitForSemaphore">The semaphore to wait for before accessing the image</param>
    /// <param name="waitForValue">The value to wait for before accessing the image</param>
    /// <param name="signalSemaphore">The semaphore to signal after accessing the image</param>
    /// <param name="signalValue">The value to signal after accessing the image</param>
    /// <returns>A task that completes when update operation is completed and user code is free to destroy or dispose the image</returns>
    public Task UpdateWithTimelineSemaphoresAsync(ICompositionImportedGpuImage image,
        ICompositionImportedGpuSemaphore waitForSemaphore, ulong waitForValue,
        ICompositionImportedGpuSemaphore signalSemaphore, ulong signalValue)
    {
        var img = VerifyOwnership(image);
        var wait = VerifyOwnership(waitForSemaphore);
        var signal = VerifyOwnership(signalSemaphore);
        return Compositor.InvokeServerJobAsync(() => Server.UpdateWithTimelineSemaphores(img, wait, waitForValue, signal, signalValue));
    }

    /// <summary>
    /// Updates the surface contents using an unspecified automatic means of synchronization
    /// provided by the underlying platform
    /// </summary>
    /// <param name="image">GPU image with new surface contents</param>
    /// <returns>A task that completes when update operation is completed and user code is free to destroy or dispose the image</returns>
    public Task UpdateAsync(ICompositionImportedGpuImage image)
    {
        var img = VerifyOwnership(image);
        return Compositor.InvokeServerJobAsync(() => Server.UpdateWithAutomaticSync(img));
    }

    ~CompositionDrawingSurface()
    {
        Compositor.Dispatcher.Post(Dispose);
    }

    public new void Dispose() => base.Dispose();
}
