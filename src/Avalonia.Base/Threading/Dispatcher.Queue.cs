using System;
using System.Diagnostics;

namespace Avalonia.Threading;

public partial class Dispatcher
{
    private readonly DispatcherPriorityQueue _queue = new();
    private bool _signaled;
    private DispatcherTimer? _backgroundTimer;
    private const int MaximumTimeProcessingBackgroundJobs = 50;
    
    void RequestBackgroundProcessing()
    {
        if (_backgroundTimer == null)
        {
            _backgroundTimer =
                new DispatcherTimer(this, DispatcherPriority.Send,
                    TimeSpan.FromMilliseconds(1));
            _backgroundTimer.Tick += delegate
            {
                _backgroundTimer.Stop();
            };
        }

        _backgroundTimer.IsEnabled = true;
    }

    /// <summary>
    /// Force-runs all dispatcher operations ignoring any pending OS events, use with caution
    /// </summary>
    public void RunJobs(DispatcherPriority? priority = null)
    {
        priority ??= DispatcherPriority.MinimumActiveValue;
        if (priority < DispatcherPriority.MinimumActiveValue)
            priority = DispatcherPriority.MinimumActiveValue;
        while (true)
        {
            DispatcherOperation? job;
            lock (InstanceLock)
                job = _queue.Peek();
            if (job == null)
                return;
            if (priority != null && job.Priority < priority.Value)
                return;
            ExecuteJob(job);
        }
    }

    internal static void ResetForUnitTests()
    {
        if (s_uiThread == null)
            return;
        var st = Stopwatch.StartNew();
        while (true)
        {
            if (st.Elapsed.TotalSeconds > 5)
                throw new InvalidProgramException("You've caused dispatcher loop");
            
            DispatcherOperation? job;
            lock (s_uiThread.InstanceLock)
                job = s_uiThread._queue.Peek();
            if (job == null || job.Priority <= DispatcherPriority.Inactive)
            {
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
        try
        {
            ExecuteJobsCore();
        }
        finally
        {
            lock (InstanceLock)
                _signaled = false;
        }
    }

    void ExecuteJobsCore()
    {
        int? backgroundJobExecutionStartedAt = null;
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
                backgroundJobExecutionStartedAt = null;
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
            // We can't check if there is pending input, but still need to enforce interactivity
            // so we stop processing background jobs after some timeout and start a timer to continue later
            else
            {
                if (backgroundJobExecutionStartedAt == null)
                    backgroundJobExecutionStartedAt = Clock.TickCount;
                
                if (Clock.TickCount - backgroundJobExecutionStartedAt.Value > MaximumTimeProcessingBackgroundJobs)
                {
                    _signaled = true;
                    RequestBackgroundProcessing();
                    return;
                }
                else
                    ExecuteJob(job);
            }
        }
    }

    private bool RequestProcessing()
    {
        lock (InstanceLock)
        {
            if (_queue.MaxPriority <= DispatcherPriority.Input)
                RequestBackgroundProcessing();
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
