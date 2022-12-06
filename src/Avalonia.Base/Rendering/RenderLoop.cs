using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia.Logging;
using Avalonia.Threading;

namespace Avalonia.Rendering
{
    /// <summary>
    /// The application render loop.
    /// </summary>
    /// <remarks>
    /// The render loop is responsible for advancing the animation timer and updating the scene
    /// graph for visible windows.
    /// </remarks>
    public class RenderLoop : IRenderLoop
    {
        private readonly IDispatcher _dispatcher;
        private List<IRenderLoopTask> _items = new List<IRenderLoopTask>();
        private List<IRenderLoopTask> _itemsCopy = new List<IRenderLoopTask>();
        private List<IRenderLoopTask> _updateItemsCopy = new List<IRenderLoopTask>();
        private IRenderTimer? _timer;
        private int _inTick;
        private int _inUpdate;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderLoop"/> class.
        /// </summary>
        public RenderLoop()
        {
            _dispatcher = Dispatcher.UIThread;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderLoop"/> class.
        /// </summary>
        /// <param name="timer">The render timer.</param>
        /// <param name="dispatcher">The UI thread dispatcher.</param>
        public RenderLoop(IRenderTimer timer, IDispatcher dispatcher)
        {
            _timer = timer;
            _dispatcher = dispatcher;
        }

        /// <summary>
        /// Gets the render timer.
        /// </summary>
        protected IRenderTimer Timer
        {
            get
            {
                return _timer ??= AvaloniaLocator.Current.GetService<IRenderTimer>() ??
                    throw new InvalidOperationException("Cannot locate IRenderTimer.");
            }
        }

        /// <inheritdoc/>
        public void Add(IRenderLoopTask i)
        {
            _ = i ?? throw new ArgumentNullException(nameof(i));
            Dispatcher.UIThread.VerifyAccess();

            lock (_items)
            {
                _items.Add(i);

                if (_items.Count == 1)
                {
                    Timer.Tick += TimerTick;
                }
            }
        }

        /// <inheritdoc/>
        public void Remove(IRenderLoopTask i)
        {
            _ = i ?? throw new ArgumentNullException(nameof(i));
            Dispatcher.UIThread.VerifyAccess();
            lock (_items)
            {
                _items.Remove(i);

                if (_items.Count == 0)
                {
                    Timer.Tick -= TimerTick;
                }
            }
        }

        public bool RunsInBackground => Timer.RunsInBackground;

        private void TimerTick(TimeSpan time)
        {
            if (Interlocked.CompareExchange(ref _inTick, 1, 0) == 0)
            {
                try
                {
                    bool needsUpdate = false;

                    lock (_items)
                    {
                        _itemsCopy.Clear();
                        foreach (var i in _items)
                            _itemsCopy.Add(i);
                    }
                    
                    foreach (IRenderLoopTask item in _itemsCopy)
                    {
                        if (item.NeedsUpdate)
                        {
                            needsUpdate = true;

                            break;
                        }
                    }

                    if (needsUpdate &&
                        Interlocked.CompareExchange(ref _inUpdate, 1, 0) == 0)
                    {
                        _dispatcher.Post(() =>
                        {
                            lock (_items)
                            {
                                _updateItemsCopy.Clear();
                                _updateItemsCopy.AddRange(_items);
                            }
                            foreach (var item in _updateItemsCopy)
                            {
                                if (item.NeedsUpdate)
                                {
                                    try
                                    {
                                        item.Update(time);
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(this, "Exception in render update: {Error}", ex);
                                    }
                                }
                            }
                            _updateItemsCopy.Clear();

                            Interlocked.Exchange(ref _inUpdate, 0);
                        }, DispatcherPriority.Render);
                    }

                    for (int i = 0; i < _itemsCopy.Count; i++)
                    {
                        _itemsCopy[i].Render();
                    }
                    
                    _itemsCopy.Clear();

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
