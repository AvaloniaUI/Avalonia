using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Threading;

public partial class Dispatcher
{
    private List<DispatcherTimer> _timers = new();
    private long _timersVersion;
    private bool _dueTimeFound;
    private int _dueTimeInMs;
    private bool _isOsTimerSet;

    internal void UpdateOSTimer()
    {
        if (!CheckAccess())
        {
            Post(UpdateOSTimer, DispatcherPriority.Send);
            return;
        }

        lock (InstanceLock)
        {
            if (!_hasShutdownFinished) // Dispatcher thread, does not technically need the lock to read
            {
                bool oldDueTimeFound = _dueTimeFound;
                int oldDueTimeInTicks = _dueTimeInMs;
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
                    if (!_isOsTimerSet || !oldDueTimeFound || (oldDueTimeInTicks != _dueTimeInMs))
                    {
                        _impl.UpdateTimer(Math.Max(1, _dueTimeInMs));
                        _isOsTimerSet = true;
                    }
                }
                else if (oldDueTimeFound)
                {
                    _impl.UpdateTimer(null);
                    _isOsTimerSet = false;
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

        UpdateOSTimer();
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

        UpdateOSTimer();
    }

    private void OnOSTimer()
    {
        lock (InstanceLock)
        {
            _impl.UpdateTimer(null);
            _isOsTimerSet = false;
        }
        PromoteTimers();
    }
    
    internal void PromoteTimers()
    {
        int currentTimeInTicks = Clock.TickCount;
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
            UpdateOSTimer();
        }
    }

    internal static List<DispatcherTimer> SnapshotTimersForUnitTests() =>
        s_uiThread!._timers.Where(t => t != s_uiThread._backgroundTimer).ToList();
}