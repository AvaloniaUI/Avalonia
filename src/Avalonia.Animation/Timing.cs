// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Reactive;
using Avalonia.Threading;

namespace Avalonia.Animation
{
    /// <summary>
    /// Provides global timing functions for animations.
    /// </summary>
    public static class Timing
    {
        static TimerObservable _timer = new TimerObservable();

        /// <summary>
        /// Initializes static members of the <see cref="Timing"/> class.
        /// </summary>
        static Timing()
        { 
            AnimationsTimer = _timer
                .Publish()
                .RefCount();
        }

        public static bool HasSubscriptions => _timer.HasSubscriptions;

        internal static TimeSpan GetTickCount() => TimeSpan.FromMilliseconds(Environment.TickCount);

        /// <summary>
        /// Gets the animation timer.
        /// </summary>
        internal static IObservable<TimeSpan> AnimationsTimer
        {
            get;
        }

        public static void Pulse(long tickCount) => _timer.Pulse(tickCount);

        private class TimerObservable : LightweightObservableBase<TimeSpan>
        {
            public bool HasSubscriptions { get; private set; }
            public void Pulse(long tickCount) => PublishNext(TimeSpan.FromMilliseconds(tickCount));
            protected override void Initialize() => HasSubscriptions = true;
            protected override void Deinitialize() => HasSubscriptions = false;
        }
    }
}
