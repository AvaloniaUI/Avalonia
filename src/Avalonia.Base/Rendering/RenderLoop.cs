using System;
using System.Collections.Generic;
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
    internal class RenderLoop : IRenderLoop
    {
        private readonly List<IRenderLoopTask> _items = new List<IRenderLoopTask>();
        private readonly List<IRenderLoopTask> _itemsCopy = new List<IRenderLoopTask>();
        private IRenderTimer? _timer;
        private int _inTick;
        
        public static IRenderLoop LocatorAutoInstance
        {
            get
            {
                var loop = AvaloniaLocator.Current.GetService<IRenderLoop>();
                if (loop == null)
                {
                    var timer = AvaloniaLocator.Current.GetRequiredService<IRenderTimer>();
                    AvaloniaLocator.CurrentMutable.Bind<IRenderLoop>()
                        .ToConstant(loop = new RenderLoop(timer));
                }

                return loop;
            }
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="RenderLoop"/> class.
        /// </summary>
        /// <param name="timer">The render timer.</param>
        public RenderLoop(IRenderTimer timer)
        {
            _timer = timer;
        }

        /// <summary>
        /// Gets the render timer.
        /// </summary>
        protected IRenderTimer Timer
        {
            get
            {
                return _timer ??= AvaloniaLocator.Current.GetRequiredService<IRenderTimer>();
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

        /// <inheritdoc />
        public bool RunsInBackground => Timer.RunsInBackground;

        private void TimerTick(TimeSpan time)
        {
            if (Interlocked.CompareExchange(ref _inTick, 1, 0) == 0)
            {
                try
                {
                    
                    lock (_items)
                    {
                        _itemsCopy.Clear();
                        _itemsCopy.AddRange(_items);
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
