using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace Avalonia.Animation.Keyframes
{

    /// <summary>
    /// Stores data regarding a specific key
    /// point and value in an animation.
    /// </summary>
    public class KeyFrame
    {
        internal bool timeSpanSet, cueSet;

        private TimeSpan _ktimeSpan;
        private Cue _kCue;

        /// <summary>
        /// Gets or sets the key time of this <see cref="KeyFrame"/>.
        /// </summary>
        /// <value>The key time.</value>
        public TimeSpan KeyTime
        {
            get
            {

                return _ktimeSpan;
            }
            set
            {
                if (cueSet)
                {
                    throw new InvalidOperationException($"You can only set either {nameof(KeyTime)} or {nameof(Cue)}.");
                }
                timeSpanSet = true;
                _ktimeSpan = value;
            }
        }

        /// <summary>
        /// Gets or sets the cue of this <see cref="KeyFrame"/>.
        /// </summary>
        /// <value>The cue.</value>
        public Cue Cue
        {
            get
            {

                return _kCue;
            }
            set
            {
                if (timeSpanSet)
                {
                    throw new InvalidOperationException($"You can only set either {nameof(KeyTime)} or {nameof(Cue)}.");
                }
                cueSet = true;
                _kCue = value;
            }
        }


        public object Value { get; set; }

        
        ///// <summary>
        ///// Initializes a new instance of the <see cref="KeyFrame"/> class.
        ///// </summary>
        //public KeyFrame()
        //{

        //}

    }



}
