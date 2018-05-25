using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Animation
{
    /// <summary>
    /// Determines the playback state of an animation.
    /// </summary>
    public enum PlayState
    {
        /// <summary>
        /// The animation is running.
        /// </summary>
        Run,

        /// <summary>
        /// The animation is paused.
        /// </summary>
        Pause,
        
        /// <summary>
        /// The animation is stopped.
        /// </summary>
        Stop
    }
}