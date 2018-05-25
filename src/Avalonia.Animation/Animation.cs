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
            var handlerList = new List<(Type, AvaloniaProperty)>();
            var kfList = new List<AnimatorKeyFrame>();

            foreach (var keyframe in this)
            {
                foreach (var setter in keyframe)
                {
                    var custAttr = setter.GetType()
                                         .GetCustomAttributes()
                                         .Where(p => p.GetType() == typeof(AnimatorAttribute));

                    if (!custAttr.Any())
                        throw new InvalidProgramException($"Type {setter.GetType()} doesn't have Animator attribute.");

                    var handler = ((AnimatorAttribute)custAttr.First()).HandlerType;
                    if (!handlerList.Contains((handler, setter.Property)))
                        handlerList.Add((handler, setter.Property));

                    var newKF = new AnimatorKeyFrame()
                    {
                        Handler = handler,
                        Property = setter.Property,
                        Cue = keyframe.Cue,
                        KeyTime = keyframe.KeyTime,
                        timeSpanSet = keyframe.timeSpanSet,
                        cueSet = keyframe.cueSet,
                        Value = setter.Value
                    };

                    kfList.Add(newKF);
                }
            }

            var newAnimatorInstances = new List<(Type handler, AvaloniaProperty prop, IAnimator inst)>();

            foreach (var handler in handlerList)
            {
                var newInstance = (IAnimator)Activator.CreateInstance(handler.Item1);
                newInstance.Property = handler.Item2;
                newAnimatorInstances.Add((handler.Item1, handler.Item2, newInstance));
            }

            foreach (var kf in kfList)
            {
                var parent = newAnimatorInstances.Where(p => p.handler == kf.Handler &&
                                                             p.prop == kf.Property)
                                                 .First();
                parent.inst.Add(kf);
            }

            foreach(var instance in newAnimatorInstances)
                _animators.Add(instance.inst);

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