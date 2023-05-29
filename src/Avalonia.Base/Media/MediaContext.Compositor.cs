using System;
using System.Threading.Tasks;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Threading;

namespace Avalonia.Media;

partial class MediaContext
{
    private bool _scheduleCommitOnLastCompositionBatchCompletion;
    
    /// <summary>
    /// Actually sends the current batch to the compositor and does the required housekeeping
    /// This is the only place that should be allowed to call Commit
    /// </summary>
    private Batch CommitCompositor(Compositor compositor)
    {
        var commit = compositor.Commit();
        _requestedCommits.Remove(compositor);
        _pendingCompositionBatches[compositor] = commit;
        commit.Processed.ContinueWith(_ =>
            Dispatcher.UIThread.Post(() => CompositionBatchFinished(compositor, commit), DispatcherPriority.Send));
        return commit;
    }
    
    /// <summary>
    /// Handles batch completion, required to re-schedule a render pass if one was skipped due to compositor throttling
    /// </summary>
    private void CompositionBatchFinished(Compositor compositor, Batch batch)
    {
        // Check if it was the last commited batch, since sometimes we are forced to send a new
        // one without waiting for the previous one to complete  
        if (_pendingCompositionBatches.TryGetValue(compositor, out var waitingForBatch) && waitingForBatch == batch)
            _pendingCompositionBatches.Remove(compositor);

        if (_pendingCompositionBatches.Count == 0)
        {
            _animationsAreWaitingForComposition = false;

            // Check if we have uncommited changes
            if (_scheduleCommitOnLastCompositionBatchCompletion && _pendingCompositionBatches.Count > 0)
            {
                CommitCompositorsWithThrottling();
                _scheduleCommitOnLastCompositionBatchCompletion = false;
            }
            // Check if there are active animations and schedule the next render
            else if(_clock.HasSubscriptions) 
                ScheduleRender(false);
        }

    }

    private bool CommitCompositorsWithThrottling()
    {
        // Check if we are still waiting for previous composition batches
        if (_pendingCompositionBatches.Count > 0)
        {
            _scheduleCommitOnLastCompositionBatchCompletion = true;
            return true;
        }

        if (_requestedCommits.Count == 0)
            return false;
        
        foreach (var c in _requestedCommits)
            CommitCompositor(c);
        
        _requestedCommits.Clear();
        return true;
    }
    
    /// <summary>
    /// Executes a synchronous commit when we need to wait for composition jobs to be done
    /// Is used in resize and TopLevel destruction scenarios
    /// </summary>
    private void SyncCommit(Compositor compositor, bool waitFullRender)
    {
        // Unit tests are assuming that they can call any API without setting up platforms
        if (AvaloniaLocator.Current.GetService<IPlatformRenderInterface>() == null)
            return;
        
        if (compositor is
            {
                UseUiThreadForSynchronousCommits: false,
                Loop.RunsInBackground: true
            })
        {
            var batch = CommitCompositor(compositor);
            (waitFullRender ? batch.Rendered : batch.Processed).Wait();
        }
        else
        {
            CommitCompositor(compositor);
            compositor.Server.Render();
        }
    }
    
    /// <summary>
    /// This method handles synchronous rendering of a surface when requested by the OS (typically during the resize)
    /// </summary>
    // TODO: do we need to execute a render pass here too?
    // We've previously tried that and it made the resize experience worse
    public void ImmediateRenderRequested(CompositionTarget target)
    {
        SyncCommit(target.Compositor, true);
    }


    /// <summary>
    /// This method handles synchronous destruction of the composition target, so we are guaranteed
    /// to release all resources when a TopLevel is being destroyed 
    /// </summary>
    public void SyncDisposeCompositionTarget(CompositionTarget compositionTarget)
    {
        compositionTarget.Dispose();
        
        // TODO: introduce a way to skip any actual rendering for other targets and only do a dispose?
        SyncCommit(compositionTarget.Compositor, false);
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
        ScheduleRender(true);
    }
}