using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server;

internal class ServerCompositionDrawingSurface : ServerCompositionSurface, IDisposable
{
    private IRef<IBitmapImpl>? _bitmap;
    private IPlatformRenderInterfaceContext? _createdWithContext;
    // The surface can be shared between multiple client-side proxies, each of them owns one reference
    // and enqueues one dispose job via its own compositor
    private int _refCount = 1;
    public override IRef<IBitmapImpl>? Bitmap
    {
        get
        {
            // Failsafe to avoid consuming an image imported with a different context
            if (Compositor.RenderInterface.Value != _createdWithContext)
                return null;
            return _bitmap;
        }
    }

    public ServerCompositionDrawingSurface(ServerCompositor compositor) : base(compositor)
    {
    }

    void PerformSanityChecks(CompositionImportedGpuImage image)
    {
        // Failsafe to avoid consuming an image imported with a different context
        if (!image.IsUsable)
            throw new PlatformGraphicsContextLostException();

        // This should never happen, but check for it anyway to avoid a deadlock
        if (!image.ImportCompleted.IsCompleted)
            throw new InvalidOperationException("The import operation is not completed yet");

        // Rethrow the import here exception
        if (image.ImportCompleted.IsFaulted)
            image.ImportCompleted.GetAwaiter().GetResult();
    }

    void Update(IBitmapImpl newImage, IPlatformRenderInterfaceContext context)
    {
        if (Volatile.Read(ref _refCount) <= 0)
        {
            newImage.Dispose();
            throw new ObjectDisposedException(nameof(ServerCompositionDrawingSurface));
        }
        _bitmap?.Dispose();
        _bitmap = RefCountable.Create(newImage);
        _createdWithContext = context;
        Changed?.Invoke();
    }

    /// <summary>
    /// Adds a reference for a new client-side proxy. Must be called while the caller
    /// holds a not-yet-disposed client-side reference
    /// </summary>
    public void AddRef()
    {
        if (Interlocked.Increment(ref _refCount) <= 1)
        {
            Interlocked.Decrement(ref _refCount);
            throw new ObjectDisposedException(nameof(ServerCompositionDrawingSurface));
        }
    }

    public void UpdateWithAutomaticSync(CompositionImportedGpuImage image)
    {
        using (Compositor.RenderInterface.EnsureCurrent())
        {
            PerformSanityChecks(image);
            Update(image.Image.SnapshotWithAutomaticSync(), image.Context);
        }
    }
    
    public void UpdateWithKeyedMutex(CompositionImportedGpuImage image, uint acquireIndex, uint releaseIndex)
    {
        using (Compositor.RenderInterface.EnsureCurrent())
        {
            PerformSanityChecks(image);
            Update(image.Image.SnapshotWithKeyedMutex(acquireIndex, releaseIndex), image.Context);
        }
    }

    public void UpdateWithSemaphores(CompositionImportedGpuImage image, CompositionImportedGpuSemaphore wait, CompositionImportedGpuSemaphore signal)
    {
        using (Compositor.RenderInterface.EnsureCurrent())
        {
            PerformSanityChecks(image);
            if (!wait.IsUsable || !signal.IsUsable)
                throw new PlatformGraphicsContextLostException();
            Update(image.Image.SnapshotWithSemaphores(wait.Semaphore, signal.Semaphore), image.Context);
        }
    }
    
    public void UpdateWithTimelineSemaphores(CompositionImportedGpuImage image, 
        CompositionImportedGpuSemaphore wait, ulong waitForValue,
        CompositionImportedGpuSemaphore signal, ulong signalValue)
    {
        using (Compositor.RenderInterface.EnsureCurrent())
        {
            PerformSanityChecks(image);
            if (!wait.IsUsable || !signal.IsUsable)
                throw new PlatformGraphicsContextLostException();
            Update(image.Image.SnapshotWithTimelineSemaphores(wait.Semaphore, waitForValue, signal.Semaphore, signalValue), image.Context);
        }
    }

    public void Dispose()
    {
        if (Interlocked.Decrement(ref _refCount) == 0)
        {
            _bitmap?.Dispose();
            _bitmap = null;
        }
    }
}
