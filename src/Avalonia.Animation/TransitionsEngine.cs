// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Metadata;
using System;
using System.Reactive.Linq;
using Avalonia.Animation.Easings;
using Avalonia.Animation.Utils;

namespace Avalonia.Animation
{
    public class TransitionsEngine : IObservable<double>, IDisposable
    {
        private IObserver<double> observer;
        private IDisposable timerSubscription;
        private readonly TimeSpan startTime;
        private readonly TimeSpan duration;

        public TransitionsEngine(TimeSpan Duration)
        {
            startTime = Timing.GetTickCount();
            duration = Duration;

            timerSubscription = Timing
                                .AnimationsTimer
                                .Subscribe(t => TimerTick(t));
        }

        private void TimerTick(TimeSpan t)
        {
            var interpVal = (double)(t.Ticks - startTime.Ticks) / duration.Ticks;

            if (interpVal > 1d
             || interpVal < 0d)
            {
                this.Dispose();
                return;
            }

            observer?.OnNext(interpVal);
        }

        public void Dispose()
        {
            timerSubscription?.Dispose();
            observer?.OnCompleted();
        }

        public IDisposable Subscribe(IObserver<double> Observer)
        {
            if (Observer is null)
                throw new InvalidProgramException("Can only set the subscription once.");

            observer = Observer;
            return this;
        }
    }
}