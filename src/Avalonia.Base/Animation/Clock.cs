using System;
using Avalonia.Reactive;

namespace Avalonia.Animation
{
    // Note: this class was always broken: it subscribes to the global clock immediately even it if
    // doesn't actually have subscriptions
    
    internal class Clock : ClockBase
    {
        public static IClock GlobalClock => AvaloniaLocator.Current.GetRequiredService<IGlobalClock>();

        private readonly IDisposable _parentSubscription;

        public Clock() : this(GlobalClock)
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
