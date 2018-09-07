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
        private IDisposable timerSubscription;
        private TimeSpan startTime;
        private TimeSpan duration;
        private readonly Clock _clock;

        public TransitionInstance(Clock clock, TimeSpan Duration)
        {
            duration = Duration;
            _clock = clock;
        }

        private void TimerTick(TimeSpan t)
        {
            var interpVal = (double)(t.Ticks - startTime.Ticks) / duration.Ticks;

            if (interpVal > 1d
             || interpVal < 0d)
            {
                PublishCompleted();
                return;
            }

            PublishNext(interpVal);
        }

        protected override void Unsubscribed()
        {
            timerSubscription?.Dispose();
        }

        protected override void Subscribed()
        {
            startTime = _clock.CurrentTime;
            timerSubscription = _clock.Subscribe(TimerTick);
            PublishNext(0.0d);
        }
    }
}
