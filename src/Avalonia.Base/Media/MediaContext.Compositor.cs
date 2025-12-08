using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Media;

partial class MediaContext
{
    /// <summary>
    /// Actually sends the current batch to the compositor and does the required housekeeping
    /// This is the only place that should be allowed to call Commit
    /// </summary>
    private CompositionBatch CommitCompositor(Compositor compositor)
    {
        // Compositor is allowed to schedule the next batch during Commit procedure if update
        // was requested while it was doing an update
        // This can e. g. happen if InvalidateVisual is called from Visual.OnRender which is currently separate
        // from our FireInvokeOnRenderCallbacks loop, so we clean the commit request flag before rendering so
        // it can be set again
        _requestedCommits.Remove(compositor);
        var commit = compositor.Commit();
       
        _pendingCompositionBatches[compositor] = commit;
        commit.Processed.ContinueWith(_ =>
            _dispatcher.Post(() => CompositionBatchFinished(compositor, commit), DispatcherPriority.Send),
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return commit;
    }
    
    /// <summary>
    /// Handles batch completion, required to re-schedule a render pass if one was skipped due to compositor throttling
    /// </summary>
    private void CompositionBatchFinished(Compositor compositor, CompositionBatch batch)
    {
        // Check if it was the last commited batch, since sometimes we are forced to send a new
        // one without waiting for the previous one to complete  
        if (_pendingCompositionBatches.TryGetValue(compositor, out var waitingForBatch) && waitingForBatch == batch)
            _pendingCompositionBatches.Remove(compositor);

        if (_pendingCompositionBatches.Count == 0)
        {
            _animationsAreWaitingForComposition = false;

            // Check if we have requested commits or active animations and schedule a new render pass 
            if (_requestedCommits.Count != 0 || _clock.HasSubscriptions)
                ScheduleRender(false);
        }
    }


    /// <summary>
    /// Triggers a composition commit if any batches are waiting to be sent,
    /// handles throttling
    /// </summary>
    /// <returns>true if there are pending commits in-flight and there will be a "all-done" callback later</returns>
    private bool CommitCompositorsWithThrottling()
    {
        Dispatcher.UIThread.VerifyAccess();
        // Check if we are still waiting for previous composition batches
        if (_pendingCompositionBatches.Count > 0)
        {
            // Previous commit isn't handled yet
            return true;
        }
        
        if (_requestedCommits.Count == 0)
            // Nothing to do, and there are no pending commits
            return false;
        
        foreach (var c in _requestedCommits.ToArray())
            CommitCompositor(c);
        
        return true;
    }
    
    /// <summary>
    /// Executes a synchronous commit when we need to wait for composition jobs to be done
    /// Is used in resize and TopLevel destruction scenarios
    /// </summary>
    private void SyncCommit(Compositor compositor, bool waitFullRender, bool catchExceptions)
    {
        // Unit tests are assuming that they can call any API without setting up platforms
        if (AvaloniaLocator.Current.GetService<IPlatformRenderInterface>() == null)
            return;

        using var _ = NonPumpingLockHelper.Use();
        SyncWaitCompositorBatch(compositor, CommitCompositor(compositor), waitFullRender, catchExceptions);
    }

    private void SyncWaitCompositorBatch(Compositor compositor, CompositionBatch batch,
        bool waitFullRender, bool catchExceptions)
    {
        using var _ = NonPumpingLockHelper.Use();
        if (compositor is
            {
                UseUiThreadForSynchronousCommits: false,
                Loop.RunsInBackground: true
            })
        {
            (waitFullRender ? batch.Rendered : batch.Processed).Wait();
        }
        else
        {
            compositor.Server.Render(catchExceptions);
        }
    }
    
    /// <summary>
    /// This method handles synchronous rendering of a surface when requested by the OS (typically during the resize)
    /// </summary>
    // TODO: do we need to execute a render pass here too?
    // We've previously tried that and it made the resize experience worse
    public void ImmediateRenderRequested(CompositionTarget target, bool catchExceptions)
    {
        SyncCommit(target.Compositor, true, catchExceptions);
    }


    /// <summary>
    /// This method handles synchronous destruction of the composition target, so we are guaranteed
    /// to release all resources when a TopLevel is being destroyed 
    /// </summary>
    public void SyncDisposeCompositionTarget(CompositionTarget compositionTarget)
    {
        using var _ = NonPumpingLockHelper.Use();
        
        // TODO: We are sending a dispose command outside of the normal commit cycle and we might
        // want to ask the compositor to skip any actual rendering and return the control ASAP
        // Not sure if we should do that for background thread rendering since it might affect the animation
        // smoothness of other windows
        
        var oobBatch = compositionTarget.Compositor.OobDispose(compositionTarget);
        SyncWaitCompositorBatch(compositionTarget.Compositor, oobBatch, false, true);
    }
    
    /// <summary>
    /// This method schedules a render when something has called RequestCommitAsync
    /// This can be triggered by user code outside of our normal layout and rendering
    /// </summary>
    void ICompositorScheduler.CommitRequested(Compositor compositor)
    {
        if(!_requestedCommits.Add(compositor))
            return;

        // TODO: maybe skip the full render here?
        ScheduleRender(false);
    }
}
