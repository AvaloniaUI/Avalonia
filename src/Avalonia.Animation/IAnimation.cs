// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Avalonia.Animation
{
    /// <summary>
    /// Interface for Animation objects
    /// </summary>
    public interface IAnimation
    {
        /// <summary>
        /// Apply the animation to the specified control
        /// </summary>
        IDisposable Apply(Animatable control, IObservable<bool> match, Action onComplete = null);

        /// <summary>
        /// Run the animation to the specified control
        /// </summary>
        Task RunAsync(Animatable control);
    }
}
