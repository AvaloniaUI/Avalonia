// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Data;
using Avalonia.Threading;

namespace Avalonia.Animation
{
    /// <summary>
    /// Utilities for creating animations.
    /// </summary>
    public static class Animate
    {
        /// <summary>
        /// The number of frames per second.
        /// </summary>
        public const int FramesPerSecond = 60;

        /// <summary>
        /// The time span of each frame.
        /// </summary>
        private static readonly TimeSpan Tick = TimeSpan.FromSeconds(1.0 / FramesPerSecond);

        /// <summary>
        /// Initializes static members of the <see cref="Animate"/> class.
        /// </summary>
        static Animate()
        {
            Stopwatch = new Stopwatch();
            Stopwatch.Start();
            Timer = Observable.Interval(Tick, AvaloniaScheduler.Instance)
                .Select(_ => Stopwatch.Elapsed)
                .Publish()
                .RefCount();
        }

        /// <summary>
        /// The stopwatch used to track time.
        /// </summary>
        /// <value>
        /// The stopwatch used to track time.
        /// </value>
        public static Stopwatch Stopwatch
        {
            get; }

        /// <summary>
        /// Gets the animation timer.
        /// </summary>
        /// <remarks>
        /// The animation timer ticks <see cref="FramesPerSecond"/> times per second. The
        /// parameter passed to a subsciber is the time span since the animation system was
        /// initialized.
        /// </remarks>
        /// <value>
        /// The animation timer.
        /// </value>
        public static IObservable<TimeSpan> Timer
        {
            get; }

        /// <summary>
        /// Gets a timer that fires every frame for the specified duration.
        /// </summary>
        /// <param name="duration">The duration of the animation.</param>
        /// <returns>
        /// An observable that notifies the subscriber of the progress along the animation.
        /// </returns>
        /// <remarks>
        /// The parameter passed to the subscriber is the progress along the animation, with
        /// 0 being the start and 1 being the end. The observable is guaranteed to fire 0
        /// immediately on subscribe and 1 at the end of the duration.
        /// </remarks>
        public static IObservable<double> GetTimer(TimeSpan duration)
        {
            var startTime = Stopwatch.Elapsed.Ticks;
            var endTime = startTime + duration.Ticks;
            return Timer
                .TakeWhile(x => x.Ticks < endTime)
                .Select(x => (x.Ticks - startTime) / (double)duration.Ticks)
                .StartWith(0.0)
                .Concat(Observable.Return(1.0));
        }

        /// <summary>
        /// Animates a <see cref="AvaloniaProperty"/>.
        /// </summary>
        /// <param name="target">The target object.</param>
        /// <param name="property">The target property.</param>
        /// <param name="start">The value of the property at the start of the animation.</param>
        /// <param name="finish">The value of the property at the end of the animation.</param>
        /// <param name="easing">The easing function to use.</param>
        /// <param name="duration">The duration of the animation.</param>
        /// <returns>An <see cref="Animation"/> that can be used to track or stop the animation.</returns>
        public static Animation Property(
            IAvaloniaObject target,
            AvaloniaProperty property,
            object start,
            object finish,
            IEasing easing,
            TimeSpan duration)
        {
            var o = GetTimer(duration).Select(progress => easing.Ease(progress, start, finish));
            return new Animation(o, target.Bind(property, o, BindingPriority.Animation));
        }

        /// <summary>
        /// Animates a <see cref="AvaloniaProperty"/>.
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="target">The target object.</param>
        /// <param name="property">The target property.</param>
        /// <param name="start">The value of the property at the start of the animation.</param>
        /// <param name="finish">The value of the property at the end of the animation.</param>
        /// <param name="easing">The easing function to use.</param>
        /// <param name="duration">The duration of the animation.</param>
        /// <returns>An <see cref="Animation"/> that can be used to track or stop the animation.</returns>
        public static Animation<T> Property<T>(
            IAvaloniaObject target,
            AvaloniaProperty<T> property,
            T start,
            T finish,
            IEasing<T> easing,
            TimeSpan duration)
        {
            var o = GetTimer(duration).Select(progress => easing.Ease(progress, start, finish));
            return new Animation<T>(o, target.Bind(property, o, BindingPriority.Animation));
        }
    }
}
