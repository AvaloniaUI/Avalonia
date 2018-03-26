// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Defines the interface for easing classes.
    /// </summary>
    public interface IEasing
    {
        /// <summary>
        /// Returns the value of the transition for the specified progress.
        /// </summary>
        double Ease(double progress);
    }
}
