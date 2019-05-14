// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Metadata;
using System;
using System.Reactive.Linq;
using Avalonia.Animation.Easings;
using Avalonia.Animation.Utils;
using Avalonia.Reactive;

namespace Avalonia.Animation
{
    /// <summary>
    /// Handles the timing and lifetime of a <see cref="Transition{T}"/>.
    /// </summary>
    internal class TransitionInstance : SingleSubscriberObservableBase<double>
    {
        private IDisposable _timerSubscription;
        private TimeSpan _duration;
        private readonly IClock _baseClock;
        private IClock _clock;

        public TransitionInstance(IClock clock, TimeSpan Duration)
        {
            _duration = Duration;
            _baseClock = clock;
        }

        private void TimerTick(TimeSpan t)
        {
            var interpVal = (double)t.Ticks / _duration.Ticks;

            // Clamp interpolation value.
            if (interpVal >= 1d | interpVal < 0d)
            {
                PublishNext(1d);
                PublishCompleted();
            }
            else
            {
                PublishNext(interpVal);
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
