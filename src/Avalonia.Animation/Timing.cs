// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Diagnostics;
using System.Reactive.Linq;
using Avalonia.Reactive;
using Avalonia.Threading;
using System.Collections.Generic;

namespace Avalonia.Animation
{
    /// <summary>
    /// Provides global timing functions for animations.
    /// </summary>
    public static class Timing
    {
        static TimerObservable _timer = new TimerObservable();
        static PlayState _globalPlayState = PlayState.Run;

        /// <summary>
        /// The number of frames per second.
        /// </summary>
        public const int FramesPerSecond = 60;

        /// <summary>
        /// The time span of each frame.
        /// </summary>
        internal static readonly TimeSpan FrameTick = 
                TimeSpan.FromTicks((long)((1000d / FramesPerSecond) * TimeSpan.TicksPerMillisecond));

        public static bool HasSubscriptions => _timer.HasSubscriptions;

        /// <summary>
        /// Sets the animation play state for all animations
        /// </summary>
        public static void SetGlobalPlayState(PlayState playState)
        {
            Dispatcher.UIThread.VerifyAccess();
            _globalPlayState = playState;
        }

        /// <summary>
        /// Gets the animation play state for all animations
        /// </summary>
        public static PlayState GetGlobalPlayState()
        {
            Dispatcher.UIThread.VerifyAccess();
            return _globalPlayState;
        }

        /// <summary>
        /// Gets the animation timer.
        /// </summary>
        /// <remarks>
        /// The animation timer triggers usually at 60 times per second or as
        /// defined in <see cref="FramesPerSecond"/>.
        /// The parameter passed to a subsciber is the current playstate of the animation.
        /// </remarks>
        internal static IObservable<long> AnimationStateTimer => _timer;

        /// <summary>
        /// Gets a timer that fires every frame for the specified duration with delay.
        /// </summary>
        /// <returns>
        /// An observable that notifies the subscriber of the progress along the transition.
        /// </returns>
        /// <remarks>
        /// The parameter passed to the subscriber is the progress along the transition, with
        /// 0 being the start and 1 being the end. The observable is guaranteed to fire 0
        /// immediately on subscribe and 1 at the end of the duration.
        /// </remarks>
        public static IObservable<double> GetTransitionsTimer(TimeSpan duration)
        {
            var startTime = _timer.FrameCount;
            var durationNumFrames = duration.Ticks / FrameTick.Ticks;
            var endTime = startTime + durationNumFrames;

            return _timer
                .TakeWhile(x => x < endTime)
                .Select(x => (double)(x - startTime) / durationNumFrames)
                .StartWith(0.0)
                .Concat(Observable.Return(1.0));
        }

        public static void Pulse(long frameCount) => _timer.Pulse(frameCount);

        private class TimerObservable : LightweightObservableBase<long>
        {
            public bool HasSubscriptions { get; private set; }
            public long FrameCount { get; private set; }
            protected override void Initialize() => HasSubscriptions = true;
            protected override void Deinitialize() => HasSubscriptions = false;

            public void Pulse(long frameCount)
            {
                FrameCount = frameCount;
                PublishNext(FrameCount);
            }
        }
    }
}
