// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Animation.Easings;
using Avalonia.Animation.Keyframes;
using Avalonia.Collections;
using Avalonia.Metadata;
using System;
using System.Collections.Generic;

namespace Avalonia.Animation
{
    /// <summary>
    /// Tracks the progress of an animation.
    /// </summary>
    public class Animation : IDisposable, IAnimation
    {
        private List<IDisposable>_subscription = new List<IDisposable>();

        /// <summary>
        /// Run time of this animation.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Delay time for animation.
        /// </summary>
        public TimeSpan Delay { get; set; }

        /// <summary>
        /// Easing function to be used.
        /// </summary> 
        public Easing Easing { get; set; } = new LinearEasing();

        /// <summary>
        /// A list of <see cref="IKeyFrames"/> objects.
        /// </summary>
        [Content]
        public AvaloniaList<IKeyFrames> Children { get; set; } = new AvaloniaList<IKeyFrames>();

        /// <summary>
        /// Cancels the animation.
        /// </summary>
        public void Dispose()
        {
            foreach(var sub in _subscription) sub.Dispose();
        }

        /// <inheritdocs/>
        public IDisposable Apply(Animatable control, IObservable<bool> matchObs)
        {
            foreach (IKeyFrames keyframes in Children)
            {
                _subscription.Add(keyframes.Apply(this, control, matchObs));
            }
            return this;
        }

    }
}
