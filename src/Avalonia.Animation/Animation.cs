// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Animation.Easings;
using Avalonia.Animation;
using Avalonia.Collections;
using Avalonia.Metadata;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Linq;

namespace Avalonia.Animation
{
    /// <summary>
    /// Tracks the progress of an animation.
    /// </summary>
    public class Animation : AvaloniaList<KeyFrame>, IDisposable, IAnimation
    {
        private readonly static List<(Func<AvaloniaProperty, bool> Condition, Type Animator)> Animators = new List<(Func<AvaloniaProperty, bool>, Type)>
        {
            ( prop => typeof(double).IsAssignableFrom(prop.PropertyType), typeof(DoubleAnimator) )
        };

        public static void RegisterAnimator<TAnimator>(Func<AvaloniaProperty, bool> condition)
            where TAnimator: IAnimator
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

        private bool _isChildrenChanged = false;
        private List<IDisposable> _subscription = new List<IDisposable>();
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

        public Animation()
        {
            this.CollectionChanged += delegate { _isChildrenChanged = true; };
        }
 
        private void InterpretKeyframes()
        {
            var handlerList = new List<(Type type, AvaloniaProperty property)>();
            var animatorKeyFrames = new List<AnimatorKeyFrame>();

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

                    var newKF = new AnimatorKeyFrame()
                    {
                        AnimatorType = handler,
                        Property = setter.Property,
                        Cue = cue,
                        Value = setter.Value
                    };

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

            foreach(var instance in newAnimatorInstances)
                _animators.Add(instance);

        }

        /// <summary>
        /// Cancels the animation.
        /// </summary>
        public void Dispose()
        {
            foreach (var sub in _subscription)
            {
                sub.Dispose();
            }
        }

        /// <inheritdocs/>
        public IDisposable Apply(Animatable control, IObservable<bool> matchObs)
        {
            if (_isChildrenChanged)
            {
                InterpretKeyframes();
                _isChildrenChanged = false;
            }

            foreach (IAnimator keyframes in _animators)
            {
                _subscription.Add(keyframes.Apply(this, control, matchObs));
            }
            return this;
        }
    }
}
