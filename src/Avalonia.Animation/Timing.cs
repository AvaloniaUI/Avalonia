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
        static long _transitionsFrameCount;
        static PlayState _globalState = PlayState.Run;

        /// <summary>
        /// The number of frames per second.
        /// </summary>
        public const int FramesPerSecond = 60;

        /// <summary>
        /// The time span of each frame.
        /// </summary>
        internal static readonly TimeSpan FrameTick = TimeSpan.FromSeconds(1.0 / FramesPerSecond);

        /// <summary>
        /// Initializes static members of the <see cref="Timing"/> class.
        /// </summary>
        static Timing()
        {
            AnimationStateTimer = _timer
                .Select(_ =>
                {
                    return _globalState;
                })
                .Publish()
                .RefCount();
        }

        public static bool HasSubscriptions => _timer.HasSubscriptions;

        /// <summary>
        /// Sets the animation play state for all animations
        /// </summary>
        public static void SetGlobalPlayState(PlayState playState)
        {
            Dispatcher.UIThread.VerifyAccess();
            _globalState = playState;
        }

        /// <summary>
        /// Gets the animation play state for all animations
        /// </summary>
        public static PlayState GetGlobalPlayState()
        {
            Dispatcher.UIThread.VerifyAccess();
            return _globalState;
        }

        /// <summary>
        /// Gets the animation timer.
        /// </summary>
        /// <remarks>
        /// The animation timer triggers usually at 60 times per second or as
        /// defined in <see cref="FramesPerSecond"/>.
        /// The parameter passed to a subsciber is the current playstate of the animation.
        /// </remarks>
        internal static IObservable<PlayState> AnimationStateTimer
        {
            get;
        }

        /// <summary>
        /// Gets the transitions timer.
        /// </summary>
        /// <remarks>
        /// The transitions timer increments usually 60 times per second as
        /// defined in <see cref="FramesPerSecond"/>.
        /// The parameter passed to a subsciber is the number of frames since the animation system was
        /// initialized.
        /// </remarks>
        public static IObservable<long> TransitionsTimer => _timer;

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
        public static IObservable<double> GetTransitionsTimer(Animatable control, TimeSpan duration, TimeSpan delay = default(TimeSpan))
        {
            var startTime = _transitionsFrameCount;
            var _duration = (long)(duration.Ticks / FrameTick.Ticks);
            var endTime = startTime + _duration;

            return TransitionsTimer
                .TakeWhile(x => x < endTime)
                .Select(x => (double)(x - startTime) / _duration)
                .StartWith(0.0)
                .Concat(Observable.Return(1.0));
        }

        public static void Pulse(long tickCount) => _timer.Pulse(tickCount);

        private class TimerObservable : LightweightObservableBase<long>
        {
            public bool HasSubscriptions { get; private set; }
            public void Pulse(long tickCount) => PublishNext(tickCount);
            protected override void Initialize() => HasSubscriptions = true;
            protected override void Deinitialize() => HasSubscriptions = false;
        }
    }
}
