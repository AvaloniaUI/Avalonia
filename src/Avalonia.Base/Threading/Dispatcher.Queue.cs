using System;
using System.Diagnostics;
using System.Threading;

namespace Avalonia.Threading;

public partial class Dispatcher
{
    private readonly DispatcherPriorityQueue _queue = new();
    private bool _signaled;
    private bool _explicitBackgroundProcessingRequested;
    private const int MaximumInputStarvationTimeInFallbackMode = 50;
    private const int MaximumInputStarvationTimeInExplicitProcessingExplicitMode = 50;
    private readonly int _maximumInputStarvationTime;
    
    void RequestBackgroundProcessing()
    {
        lock (InstanceLock)
        {
            if (_backgroundProcessingImpl != null)
            {
                if(_explicitBackgroundProcessingRequested)
                    return;
                _explicitBackgroundProcessingRequested = true;
                _backgroundProcessingImpl.RequestBackgroundProcessing();
            }
            else if (_dueTimeForBackgroundProcessing == null)
            {
                _dueTimeForBackgroundProcessing = Now + 1;
                UpdateOSTimer();
            }
        }
    }

    private void OnReadyForExplicitBackgroundProcessing()
    {
        lock (InstanceLock)
        {
            _explicitBackgroundProcessingRequested = false;
        }
        ExecuteJobsCore(true);
    }

    /// <summary>
    /// Force-runs all dispatcher operations ignoring any pending OS events, use with caution
    /// </summary>
    public void RunJobs(DispatcherPriority? priority = null)
    {
        RunJobs(priority, CancellationToken.None);
    }
    
    internal void RunJobs(DispatcherPriority? priority, CancellationToken cancellationToken)
    {
        if (DisabledProcessingCount > 0)
            throw new InvalidOperationException(
                "Cannot perform this operation while dispatcher processing is suspended.");
        
        priority ??= DispatcherPriority.MinimumActiveValue;
        if (priority < DispatcherPriority.MinimumActiveValue)
            priority = DispatcherPriority.MinimumActiveValue;
        while (!cancellationToken.IsCancellationRequested)
        {
            DispatcherOperation? job;
            lock (InstanceLock)
                job = _queue.Peek();
            if (job == null)
                return;
            if (job.Priority < priority.Value)
                return;
            ExecuteJob(job);
        }
    }

    private sealed class DummyShuttingDownUnitTestDispatcherImpl : IDispatcherImpl
    {
        public bool CurrentThreadIsLoopThread => true;

        public void Signal()
        {
        }

        public event Action? Signaled
        {
            add { }
            remove { }
        }

        public event Action? Timer
        {
            add { }
            remove { }
        }

        public long Now => 0;

        public void UpdateTimer(long? dueTimeInMs)
        {
        }
    }

    internal static void ResetBeforeUnitTests()
    {
        s_uiThread = null;
    }
    
    internal static void ResetForUnitTests()
    {
        if (s_uiThread == null)
            return;
        var st = Stopwatch.StartNew();
        while (true)
        {
            s_uiThread._pendingInputImpl = s_uiThread._controlledImpl = null;
            s_uiThread._impl = new DummyShuttingDownUnitTestDispatcherImpl();
            if (st.Elapsed.TotalSeconds > 5)
                throw new InvalidProgramException("You've caused dispatcher loop");
            
            DispatcherOperation? job;
            lock (s_uiThread.InstanceLock)
                job = s_uiThread._queue.Peek();
            if (job == null || job.Priority <= DispatcherPriority.Inactive)
            {
                s_uiThread.ShutdownImpl();
                s_uiThread = null;
                return;
            }

            s_uiThread.ExecuteJob(job);
        }

    }
    
    private void ExecuteJob(DispatcherOperation job)
    {
        lock (InstanceLock)
            _queue.RemoveItem(job);
        job.Execute();
        // The backend might be firing timers with a low priority,
        // so we manually check if our high priority timers are due for execution
        PromoteTimers();
    }

    private void Signaled()
    {
        lock (InstanceLock)
            _signaled = false;

        ExecuteJobsCore(false);
    }

    void ExecuteJobsCore(bool fromExplicitBackgroundProcessingCallback)
    {
        long? backgroundJobExecutionStartedAt = null;
        while (true)
        {
            DispatcherOperation? job;

            lock (InstanceLock)
                job = _queue.Peek();
            
            if (job == null || job.Priority < DispatcherPriority.MinimumActiveValue)
                return;
            
            
            // We don't stop for executing jobs queued with >Input priority
            if (job.Priority > DispatcherPriority.Input)
            {
                ExecuteJob(job);
            }
            // If platform supports pending input query, ask the platform if we can continue running low priority jobs
            else if (_pendingInputImpl?.CanQueryPendingInput == true)
            {
                if (!_pendingInputImpl.HasPendingInput)
                    ExecuteJob(job);
                else
                {
                    RequestBackgroundProcessing();
                    return;
                }
            }
            // We can't ask if the implementation has pending input, so we should let it to call us back
            // Once it thinks that input is handled
            else if (_backgroundProcessingImpl != null && !fromExplicitBackgroundProcessingCallback)
            {
                RequestBackgroundProcessing();
                return;
            }
            // We can't check if there is pending input, but still need to enforce interactivity
            // so we stop processing background jobs after some timeout and start a timer to continue later
            else
            {
                if (backgroundJobExecutionStartedAt == null)
                    backgroundJobExecutionStartedAt = Now;
                
                if (Now - backgroundJobExecutionStartedAt.Value > _maximumInputStarvationTime)
                {
                    RequestBackgroundProcessing();
                    return;
                }
                else
                    ExecuteJob(job);
            }
        }
    }

    internal bool RequestProcessing()
    {
        lock (InstanceLock)
        {
            if (!CheckAccess())
            {
                RequestForegroundProcessing();
                return true;
            }

            if (_queue.MaxPriority <= DispatcherPriority.Input)
            {
                if (_pendingInputImpl is { CanQueryPendingInput: true, HasPendingInput: false })
                    RequestForegroundProcessing();
                else
                    RequestBackgroundProcessing();
            }
            else
                RequestForegroundProcessing();
        }
        return true;
    }

    private void RequestForegroundProcessing()
    {
        if (!_signaled)
        {
            _signaled = true;
            _impl.Signal();
        }
    }

    internal void Abort(DispatcherOperation operation)
    {
        lock (InstanceLock)
            _queue.RemoveItem(operation);
        operation.DoAbort();
    }
    
    // Returns whether or not the priority was set.
    internal bool SetPriority(DispatcherOperation operation, DispatcherPriority priority) // NOTE: should be Priority
    {
        bool notify = false;

        lock(InstanceLock)
        {
            if(operation.IsQueued)
            {
                _queue.ChangeItemPriority(operation, priority);
                notify = true;

                if(notify)
                {
                    // Make sure we will wake up to process this operation.
                    RequestProcessing();

                }
            }
        }
        return notify;
    }

    public bool HasJobsWithPriority(DispatcherPriority priority)
    {
        lock (InstanceLock)
            return _queue.MaxPriority >= priority;
    }
}
