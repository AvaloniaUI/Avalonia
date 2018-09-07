// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Animation
{
    /// <summary>
    /// Interface for Transition objects.
    /// </summary>
    public interface ITransition
    {
        /// <summary>
        /// Applies the transition to the specified <see cref="Animatable"/>.
        /// </summary>
        IDisposable Apply(Animatable control, IClock clock, object oldValue, object newValue);

        /// <summary>
        /// Gets the property to be animated.
        /// </summary>
        AvaloniaProperty Property { get; set; }
    
    }
}
