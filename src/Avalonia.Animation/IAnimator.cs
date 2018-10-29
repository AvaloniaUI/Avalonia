﻿// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Avalonia.Animation
{
    /// <summary>
    /// Interface for Animator objects
    /// </summary>
    public interface IAnimator : IList<AnimatorKeyFrame>
    {
        /// <summary>
        /// The target property.
        /// </summary>
        AvaloniaProperty Property {get; set;}

        /// <summary>
        /// Applies the current KeyFrame group to the specified control.
        /// </summary>
        IDisposable Apply(Animation animation, Animatable control, IClock clock, IObservable<bool> match, Action onComplete);
    }
}
