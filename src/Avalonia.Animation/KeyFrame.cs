using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Avalonia.Metadata;
using Avalonia.Collections;

namespace Avalonia.Animation
{

    /// <summary>
    /// Stores data regarding a specific key
    /// point and value in an animation.
    /// </summary>
    public class KeyFrame : AvaloniaList<IAnimationSetter>
    {
        internal bool timeSpanSet, cueSet;
        private TimeSpan _ktimeSpan;
        private Cue _kCue;

        public KeyFrame()
        {
        }

        public KeyFrame(IEnumerable<IAnimationSetter> items) : base(items)
        {
        }

        public KeyFrame(params IAnimationSetter[] items) : base(items)
        {
        }

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


    }

    public class InternalKeyFrame
    {
        public Cue Cue { get; set; }
        public TimeSpan KeyTime { get; set; }
        internal bool timeSpanSet, cueSet;
        public object Value { get; set; }
    }

}
