using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Text;
using Avalonia.Rendering;

namespace Avalonia.Animation
{
    public class RenderLoopClock : ClockBase, IRenderLoopTask, IGlobalClock
    {
        private int _subCount;

        protected override void Stop()
        {
            AvaloniaLocator.Current.GetRequiredService<IRenderLoop>().Remove(this);
        }

        bool IRenderLoopTask.NeedsUpdate => HasSubscriptions;

        void IRenderLoopTask.Render()
        {
        }

        void IRenderLoopTask.Update(TimeSpan time)
        {
            Pulse(time);
        }
        
        public override IDisposable Subscribe(IObserver<TimeSpan> observer)
        {
            var disposable = base.Subscribe(observer);
            if (_subCount++ == 0)
            {
                Start();
            }
            return Disposable.Create(() =>
            {
                disposable.Dispose();
                if (--_subCount == 0)
                {
                    Stop();
                }
            });
        }

        void Start()
        {
            AvaloniaLocator.CurrentMutable.GetService<IRenderLoop>()?.Add(this);
        }
    }
}
