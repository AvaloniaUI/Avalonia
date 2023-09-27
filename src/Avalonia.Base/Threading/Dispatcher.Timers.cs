using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Threading;

public partial class Dispatcher
{
    private readonly List<DispatcherTimer> _timers = new();
    private long _timersVersion;
    private bool _dueTimeFound;
    private long _dueTimeInMs;

    private long? _dueTimeForTimers;
    private long? _dueTimeForBackgroundProcessing;
    private long? _osTimerSetTo;

    internal long Now => _impl.Now;

    private void UpdateOSTimer()
    {
        VerifyAccess();
        var nextDueTime =
            (_dueTimeForTimers.HasValue && _dueTimeForBackgroundProcessing.HasValue) ?
                Math.Min(_dueTimeForTimers.Value, _dueTimeForBackgroundProcessing.Value) :
                _dueTimeForTimers ?? _dueTimeForBackgroundProcessing;
        if (_osTimerSetTo == nextDueTime)
            return;
        _impl.UpdateTimer(_osTimerSetTo = nextDueTime);
    }

    internal void RescheduleTimers()
    {
        if (!CheckAccess())
        {
            Post(RescheduleTimers, DispatcherPriority.Send);
            return;
        }

        lock (InstanceLock)
        {
            if (!_hasShutdownFinished) // Dispatcher thread, does not technically need the lock to read
            {
                bool oldDueTimeFound = _dueTimeFound;
                long oldDueTimeInTicks = _dueTimeInMs;
                _dueTimeFound = false;
                _dueTimeInMs = 0;

                if (_timers.Count > 0)
                {
                    // We could do better if we sorted the list of timers.
                    for (int i = 0; i < _timers.Count; i++)
                    {
                        var timer = _timers[i];

                        if (!_dueTimeFound || timer.DueTimeInMs - _dueTimeInMs < 0)
                        {
                            _dueTimeFound = true;
                            _dueTimeInMs = timer.DueTimeInMs;
                        }
                    }
                }

                if (_dueTimeFound)
                {
                    if (_dueTimeForTimers == null || !oldDueTimeFound || (oldDueTimeInTicks != _dueTimeInMs))
                    {
                        _dueTimeForTimers = _dueTimeInMs;
                        UpdateOSTimer();
                    }
                }
                else if (oldDueTimeFound)
                {
                    _dueTimeForTimers = null;
                    UpdateOSTimer();
                }
            }
        }
    }

    internal void AddTimer(DispatcherTimer timer)
    {
        lock (InstanceLock)
        {
            if (!_hasShutdownFinished) // Could be a non-dispatcher thread, lock to read
            {
                _timers.Add(timer);
                _timersVersion++;
            }
        }

        RescheduleTimers();
    }

    internal void RemoveTimer(DispatcherTimer timer)
    {
        lock (InstanceLock)
        {
            if (!_hasShutdownFinished) // Could be a non-dispatcher thread, lock to read
            {
                _timers.Remove(timer);
                _timersVersion++;
            }
        }

        RescheduleTimers();
    }

    private void OnOSTimer()
    {
        _impl.UpdateTimer(null);
        _osTimerSetTo = null;
        bool needToPromoteTimers = false;
        bool needToProcessQueue = false;
        lock (InstanceLock)
        {
            _impl.UpdateTimer(_osTimerSetTo = null);
            needToPromoteTimers = _dueTimeForTimers.HasValue && _dueTimeForTimers.Value <= Now;
            if (needToPromoteTimers)
                _dueTimeForTimers = null;
            needToProcessQueue = _dueTimeForBackgroundProcessing.HasValue &&
                                 _dueTimeForBackgroundProcessing.Value <= Now;
            if (needToProcessQueue)
                _dueTimeForBackgroundProcessing = null;
        }

        if (needToPromoteTimers)
            PromoteTimers();
        if (needToProcessQueue)
            ExecuteJobsCore(false);
        UpdateOSTimer();
    }
    
    internal void PromoteTimers()
    {
        long currentTimeInTicks = Now;
        try
        {
            List<DispatcherTimer>? timers = null;
            long timersVersion = 0;

            lock (InstanceLock)
            {
                if (!_hasShutdownFinished) // Could be a non-dispatcher thread, lock to read
                {
                    if (_dueTimeFound && _dueTimeInMs - currentTimeInTicks <= 0)
                    {
                        timers = _timers;
                        timersVersion = _timersVersion;
                    }
                }
            }

            if (timers != null)
            {
                DispatcherTimer? timer = null;
                int iTimer = 0;

                do
                {
                    lock (InstanceLock)
                    {
                        timer = null;

                        // If the timers collection changed while we are in the middle of
                        // looking for timers, start over.
                        if (timersVersion != _timersVersion)
                        {
                            timersVersion = _timersVersion;
                            iTimer = 0;
                        }

                        while (iTimer < _timers.Count)
                        {
                            // WARNING: this is vulnerable to wrapping
                            if (timers[iTimer].DueTimeInMs - currentTimeInTicks <= 0)
                            {
                                // Remove this timer from our list.
                                // Do not increment the index.
                                timer = timers[iTimer];
                                timers.RemoveAt(iTimer);
                                break;
                            }
                            else
                            {
                                iTimer++;
                            }
                        }
                    }

                    // Now that we are outside of the lock, promote the timer.
                    if (timer != null)
                    {
                        timer.Promote();
                    }
                } while (timer != null);
            }
        }
        finally
        {
            RescheduleTimers();
        }
    }

    internal static List<DispatcherTimer> SnapshotTimersForUnitTests() =>
        s_uiThread!._timers.ToList();
}
