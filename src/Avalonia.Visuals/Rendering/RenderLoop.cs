using System;
using System.Collections.Generic;
using Avalonia.Logging;
using Avalonia.Threading;

namespace Avalonia.Rendering
{
    public class RenderLoop : IRenderLoop
    {
        private readonly IDispatcher _dispatcher;
        private List<IRenderLoopTask> _items = new List<IRenderLoopTask>();
        private IRenderTimer _timer;
        private bool inTick;

        public RenderLoop()
        {
            _dispatcher = Dispatcher.UIThread;
        }

        public RenderLoop(IRenderTimer timer, IDispatcher dispatcher)
        {
            _timer = timer;
            _dispatcher = dispatcher;
        }

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
