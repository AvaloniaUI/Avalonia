// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Animation.Easings;
using Avalonia.Collections;

namespace Avalonia.Animation
{
    /// <summary>
    /// Tracks the progress of an animation.
    /// </summary>
    public class Animation : AvaloniaList<KeyFrame>, IAnimation
    {
        private readonly static List<(Func<AvaloniaProperty, bool> Condition, Type Animator)> Animators = new List<(Func<AvaloniaProperty, bool>, Type)>
        {
            ( prop => typeof(double).IsAssignableFrom(prop.PropertyType), typeof(DoubleAnimator) )
        };

        public static void RegisterAnimator<TAnimator>(Func<AvaloniaProperty, bool> condition)
            where TAnimator : IAnimator
        {
            Animators.Insert(0, (condition, typeof(TAnimator)));
        }

        private static Type GetAnimatorType(AvaloniaProperty property)
        {
            foreach (var (condition, type) in Animators)
            {
                if (condition(property))
                {
                    return type;
                }
            }
            return null;
        }

        public AvaloniaList<IAnimator> _animators { get; set; } = new AvaloniaList<IAnimator>();

        /// <summary>
        /// Run time of this animation.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Delay time for this animation.
        /// </summary>
        public TimeSpan Delay { get; set; }

        /// <summary>
        /// The repeat count for this animation.
        /// </summary>
        public RepeatCount RepeatCount { get; set; }

        /// <summary>
        /// The playback direction for this animation.
        /// </summary>
        public PlaybackDirection PlaybackDirection { get; set; }

        /// <summary>
        /// The value fill mode for this animation.
        /// </summary>
        public FillMode FillMode { get; set; }

        /// <summary>
        /// Easing function to be used.
        /// </summary>
        public Easing Easing { get; set; } = new LinearEasing();

        private (IList<IAnimator> Animators, IList<IDisposable> subscriptions) InterpretKeyframes(Animatable control)
        {
            var handlerList = new List<(Type type, AvaloniaProperty property)>();
            var animatorKeyFrames = new List<AnimatorKeyFrame>();
            var subscriptions = new List<IDisposable>();

            foreach (var keyframe in this)
            {
                foreach (var setter in keyframe)
                {
                    var handler = GetAnimatorType(setter.Property);

                    if (handler == null)
                    {
                        throw new InvalidOperationException($"No animator registered for the property {setter.Property}. Add an animator to the Animation.Animators collection that matches this property to animate it.");
                    }

                    if (!handlerList.Contains((handler, setter.Property)))
                        handlerList.Add((handler, setter.Property));

                    var cue = keyframe.Cue;

                    if (keyframe.TimingMode == KeyFrameTimingMode.TimeSpan)
                    {
                        cue = new Cue(keyframe.KeyTime.Ticks / Duration.Ticks);
                    }

                    var newKF = new AnimatorKeyFrame(handler, cue);

                    subscriptions.Add(newKF.BindSetter(setter, control));

                    animatorKeyFrames.Add(newKF);
                }
            }

            var newAnimatorInstances = new List<IAnimator>();

            foreach (var (handlerType, property) in handlerList)
            {
                var newInstance = (IAnimator)Activator.CreateInstance(handlerType);
                newInstance.Property = property;
                newAnimatorInstances.Add(newInstance);
            }

            foreach (var keyframe in animatorKeyFrames)
            {
                var animator = newAnimatorInstances.First(a => a.GetType() == keyframe.AnimatorType &&
                                                             a.Property == keyframe.Property);
                animator.Add(keyframe);
            }

            return (newAnimatorInstances, subscriptions);
        }

        /// <inheritdocs/>
        public IDisposable Apply(Animatable control, IObservable<bool> match, Action onComplete)
        {
            var (animators, subscriptions) = InterpretKeyframes(control);
            if (animators.Count == 1)
            {
                subscriptions.Add(animators[0].Apply(this, control, match, onComplete));
            }
            else
            {
                var completionTasks = onComplete != null ? new List<Task>() : null;
                foreach (IAnimator animator in animators)
                {
                    Action animatorOnComplete = null;
                    if (onComplete != null)
                    {
                        var tcs = new TaskCompletionSource<object>();
                        animatorOnComplete = () => tcs.SetResult(null);
                        completionTasks.Add(tcs.Task);
                    }
                    subscriptions.Add(animator.Apply(this, control, match, animatorOnComplete));
                }

                if (onComplete != null)
                {
                    Task.WhenAll(completionTasks).ContinueWith(_ => onComplete());
                }
            }
            return new CompositeDisposable(subscriptions);
        }

        /// <inheritdocs/>
        public Task RunAsync(Animatable control)
        {
            var run = new TaskCompletionSource<object>();

            if (this.RepeatCount == RepeatCount.Loop)
                run.SetException(new InvalidOperationException("Looping animations must not use the Run method."));

            IDisposable subscriptions = null;
            subscriptions = this.Apply(control, Observable.Return(true), () =>
            {
                run.SetResult(null);
                subscriptions?.Dispose();
            });

            return run.Task;
        }
    }
}
