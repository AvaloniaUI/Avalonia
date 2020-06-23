using Avalonia.Metadata;
using System;
using System.Reactive.Linq;
using Avalonia.Animation.Easings;
using Avalonia.Animation.Utils;
using Avalonia.Reactive;
using Avalonia.Utilities;

namespace Avalonia.Animation
{
    /// <summary>
    /// Handles the timing and lifetime of a <see cref="Transition{T}"/>.
    /// </summary>
    internal class TransitionInstance : SingleSubscriberObservableBase<double>
    {
        private IDisposable _timerSubscription;
        private TimeSpan _delay;
        private TimeSpan _duration;
        private readonly IClock _baseClock;
        private IClock _clock;

        public TransitionInstance(IClock clock, TimeSpan delay, TimeSpan duration)
        {
            clock = clock ?? throw new ArgumentNullException(nameof(clock));

            _delay = delay;
            _duration = duration;
            _baseClock = clock;
        }

        private void TimerTick(TimeSpan t)
        {

            // [<------------- normalizedTotalDur ------------------>]
            // [<---- Delay ---->][<---------- Duration ------------>]
            //                   ^- normalizedDelayEnd
            //                    [<----   normalizedInterpVal   --->]

            var normalizedInterpVal = 1d;

            if (!MathUtilities.AreClose(_duration.TotalSeconds, 0d))
            {
                var normalizedTotalDur = _delay + _duration;
                var normalizedDelayEnd = _delay.TotalSeconds / normalizedTotalDur.TotalSeconds;
                var normalizedPresentationTime = t.TotalSeconds / normalizedTotalDur.TotalSeconds;

                if (normalizedPresentationTime < normalizedDelayEnd
                    || MathUtilities.AreClose(normalizedPresentationTime, normalizedDelayEnd))
                {
                    normalizedInterpVal = 0d;
                }
                else
                {
                    normalizedInterpVal = (t.TotalSeconds - _delay.TotalSeconds) / _duration.TotalSeconds;
                }
            }

            // Clamp interpolation value.
            if (normalizedInterpVal >= 1d || normalizedInterpVal < 0d)
            {
                PublishNext(1d);
                PublishCompleted();
            }
            else
            {
                PublishNext(normalizedInterpVal);
            }
        }

        protected override void Unsubscribed()
        {
            _timerSubscription?.Dispose();
            _clock.PlayState = PlayState.Stop;
        }

        protected override void Subscribed()
        {
            _clock = new Clock(_baseClock);
            _timerSubscription = _clock.Subscribe(TimerTick);
            PublishNext(0.0d);
        }
    }
}
