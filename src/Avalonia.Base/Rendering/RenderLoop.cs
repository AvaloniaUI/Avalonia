using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Logging;
using Avalonia.Metadata;
using Avalonia.Threading;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Provides factory methods for creating <see cref="IRenderLoop"/> instances.
    /// </summary>
    [PrivateApi]
    public static class RenderLoop
    {
        /// <summary>
        /// Creates an <see cref="IRenderLoop"/> from an <see cref="IRenderTimer"/>.
        /// </summary>
        public static IRenderLoop FromTimer(IRenderTimer timer) => new DefaultRenderLoop(timer);
    }

    /// <summary>
    /// Default implementation of the application render loop.
    /// </summary>
    /// <remarks>
    /// The render loop is responsible for advancing the animation timer and updating the scene
    /// graph for visible windows. It owns the sleep/wake state machine: calling
    /// <see cref="IRenderTimer.Start"/> and <see cref="IRenderTimer.Stop"/> under a lock
    /// so that timer implementations never see unbalanced or concurrent calls.
    /// </remarks>
    internal class DefaultRenderLoop : IRenderLoop
    {
        private readonly List<IRenderLoopTask> _items = new List<IRenderLoopTask>();
        private readonly List<IRenderLoopTask> _itemsCopy = new List<IRenderLoopTask>();
        private readonly IRenderTimer _timer;
        private readonly object _timerLock = new();
        private int _inTick;
        private bool _running;
        private bool _wakeupPending;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultRenderLoop"/> class.
        /// </summary>
        /// <param name="timer">The render timer.</param>
        public DefaultRenderLoop(IRenderTimer timer)
        {
            _timer = timer;
        }

        /// <inheritdoc/>
        public void Add(IRenderLoopTask i)
        {
            _ = i ?? throw new ArgumentNullException(nameof(i));
            Dispatcher.UIThread.VerifyAccess();

            bool shouldStart;
            lock (_items)
            {
                _items.Add(i);
                shouldStart = _items.Count == 1;
            }

            if (shouldStart)
            {
                _timer.Tick = TimerTick;
                Wakeup();
            }
        }

        /// <inheritdoc/>
        public void Remove(IRenderLoopTask i)
        {
            _ = i ?? throw new ArgumentNullException(nameof(i));
            Dispatcher.UIThread.VerifyAccess();

            bool shouldStop;
            lock (_items)
            {
                _items.Remove(i);
                shouldStop = _items.Count == 0;
            }

            if (shouldStop)
            {
                _timer.Tick = null;
                lock (_timerLock)
                {
                    if (_running)
                    {
                        _running = false;
                        _wakeupPending = false;
                        _timer.Stop();
                    }
                }
            }
        }

        /// <inheritdoc />
        public bool RunsInBackground => _timer.RunsInBackground;

        /// <inheritdoc />
        public void Wakeup()
        {
            lock (_timerLock)
            {
                if (_timer.Tick != null && !_running)
                {
                    _running = true;
                    _timer.Start();
                }
                else
                {
                    _wakeupPending = true;
                }
            }
        }

        private void TimerTick(TimeSpan time)
        {
            if (Interlocked.CompareExchange(ref _inTick, 1, 0) == 0)
            {
                try
                {
                    // Consume any pending wakeup — this tick will process its work.
                    // Only wakeups arriving during task execution will keep the timer running.
                    lock (_timerLock)
                    {
                        _wakeupPending = false;
                    }

                    lock (_items)
                    {
                        _itemsCopy.Clear();
                        _itemsCopy.AddRange(_items);
                    }

                    var wantsNextTick = false;
                    for (int i = 0; i < _itemsCopy.Count; i++)
                    {
                        wantsNextTick |= _itemsCopy[i].Render();
                    }
                    
                    _itemsCopy.Clear();

                    if (!wantsNextTick)
                    {
                        lock (_timerLock)
                        {
                            if (_wakeupPending)
                            {
                                _wakeupPending = false;
                            }
                            else
                            {
                                _running = false;
                                _timer.Stop();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(this, "Exception in render loop: {Error}", ex);
                }
                finally
                {
                    Interlocked.Exchange(ref _inTick, 0);
                }
            }
        }
    }
}
