using System;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Threading;

namespace Avalonia.Rendering.Composition;

/// <summary>
/// Provides a <see cref="Rendering.Composition.Compositor"/> that is attached to the same render thread as an
/// existing compositor, but is bound to a different dispatcher and is not driven by the framework's scheduler.
/// </summary>
/// <remarks>
/// Created via <see cref="Rendering.Composition.Compositor.CreateCompositorController"/>.
/// The controlled compositor is intended for feeding composition surfaces from non-UI threads,
/// see <see cref="CompositionDrawingSurface.CreateProxyForCompositor"/>.
/// Batches are committed either explicitly via <see cref="Commit"/> or by a job posted to the compositor's
/// dispatcher, so the dispatcher's thread is expected to run a dispatcher loop. The loop also installs the
/// synchronization context that keeps continuations of awaited composition APIs on the compositor's thread;
/// without it those continuations resume on the thread pool.
/// All <see cref="Rendering.Composition.Compositor"/> APIs are bound to the dispatcher's thread.
/// Composition objects created for the compositor, including surface proxies, should be disposed before
/// the dispatcher is shut down: jobs posted to a shut down dispatcher are discarded, so cleanup requested
/// afterwards never reaches the render thread.
/// </remarks>
public sealed class CompositorController : ICompositorScheduler
{
    /// <summary>
    /// The compositor managed by this controller
    /// </summary>
    public Compositor Compositor { get; }

    // Only accessed from the compositor's dispatcher thread
    private bool _commitJobScheduled;
    private readonly Action _commitJob;

    internal CompositorController(Compositor shareServerWith, Dispatcher dispatcher)
    {
        _commitJob = ExecuteScheduledCommit;
        Compositor = new Compositor(shareServerWith, this, dispatcher);
        // Flush pending batches so already requested server-side jobs and disposals aren't lost.
        // Commit callbacks can request another commit, hence the bounded loop
        dispatcher.ShutdownStarted += (_, _) =>
        {
            for (var c = 0; c < 16 && Compositor.HasPendingCommit; c++)
                Compositor.Commit();
        };
    }

    /// <summary>
    /// Synchronously serializes pending changes and submits them to the render thread.
    /// Can only be called on the compositor's dispatcher thread.
    /// </summary>
    /// <returns>
    /// The submitted batch. Note that this method returns once the batch is submitted, without waiting for it
    /// to be applied, use <see cref="CompositionBatch.Processed"/> or <see cref="CompositionBatch.Rendered"/>
    /// for synchronization with the render thread.
    /// </returns>
    /// <exception cref="InvalidOperationException">The method is called on a wrong thread</exception>
    public CompositionBatch Commit() => Compositor.Commit();

    void ICompositorScheduler.CommitRequested(Compositor compositor)
    {
        // Invoked on the compositor's dispatcher thread.
        // At most one job is kept in flight, so manual Commit calls on a never-pumped dispatcher
        // don't accumulate stale jobs
        if (_commitJobScheduled)
            return;
        _commitJobScheduled = true;
        Compositor.Dispatcher.Post(_commitJob, DispatcherPriority.Render);
    }

    private void ExecuteScheduledCommit()
    {
        _commitJobScheduled = false;
        if (Compositor.HasPendingCommit)
            Compositor.Commit();
    }
}
