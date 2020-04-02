using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using Avalonia.Reactive;

namespace Avalonia.Animation
{
    public class Clock : ClockBase
    {
        public static IClock GlobalClock => AvaloniaLocator.Current.GetService<IGlobalClock>();

        private IDisposable _parentSubscription;

        public Clock()
            :this(GlobalClock)
        {
        }
        
        public Clock(IClock parent)
        {
            _parentSubscription = parent.Subscribe(Pulse);
        }

        protected override void Stop()
        {
            _parentSubscription?.Dispose();
        }
    }
}
