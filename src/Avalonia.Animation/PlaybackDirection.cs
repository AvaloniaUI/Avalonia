// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Animation
{
    /// <summary>
    /// Determines the playback direction of an animation.
    /// </summary>
    public enum PlaybackDirection
    {
        /// <summary>
        /// The animation is played normally.
        /// </summary>
        Normal,

        /// <summary>
        /// The animation is played in reverse direction.
        /// </summary>
        Reverse,

        /// <summary>
        /// The animation is played forwards first, then backwards.
        /// </summary>
        Alternate,

        /// <summary>
        /// The animation is played backwards first, then forwards.
        /// </summary>
        AlternateReverse
    }
}
