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
        private IRenderTimer _timer;
        private int inTick;

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
                if (_timer == null)
                {
                    _timer = AvaloniaLocator.Current.GetService<IRenderTimer>();
                }

                return _timer;
            }
        }

        /// <inheritdoc/>
        public void Add(IRenderLoopTask i)
        {
            Contract.Requires<ArgumentNullException>(i != null);
            Dispatcher.UIThread.VerifyAccess();

            _items.Add(i);

            if (_items.Count == 1)
            {
                Timer.Tick += TimerTick;
            }
        }

        /// <inheritdoc/>
        public void Remove(IRenderLoopTask i)
        {
            Contract.Requires<ArgumentNullException>(i != null);
            Dispatcher.UIThread.VerifyAccess();

            _items.Remove(i);

            if (_items.Count == 0)
            {
                Timer.Tick -= TimerTick;
            }
        }

        private async void TimerTick(TimeSpan time)
        {
            if (Interlocked.CompareExchange(ref inTick, 1, 0) == 0)
            {
                try
                {
                    if (_items.Any(item => item.NeedsUpdate))
                    {
                        await _dispatcher.InvokeAsync(() =>
                        {
                            foreach (var i in _items)
                            {
                                i.Update(time);
                            }
                        }, DispatcherPriority.Render).ConfigureAwait(false);
                    }

                    foreach (var i in _items)
                    {
                        i.Render();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(LogArea.Visual, this, "Exception in render loop: {Error}", ex);
                }
                finally
                {
                    Interlocked.Exchange(ref inTick, 0);
                }
            }
        }
    }
}
