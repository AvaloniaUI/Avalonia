using System;
using System.Collections.Generic;
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
        private bool inTick;

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

        private async void TimerTick(long tickCount)
        {
            if (!inTick)
            {
                inTick = true;

                try
                {
                    var needsUpdate = Animation.Timing.HasSubscriptions;

                    foreach (var i in _items)
                    {
                        if (i.NeedsUpdate)
                        {
                            needsUpdate = true;
                            break;
                        }
                    }

                    if (needsUpdate)
                    {
                        await _dispatcher.InvokeAsync(() =>
                        {
                            Animation.Timing.Pulse(tickCount);

                            foreach (var i in _items)
                            {
                                i.Update();
                            }
                        });
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
                    inTick = false;
                }
            }
        }
    }
}
