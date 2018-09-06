using System;
using System.Collections.Generic;
using Avalonia.Collections;

namespace Avalonia.Animation
{
    internal enum KeyFrameTimingMode
    {
        TimeSpan = 1,
        Cue
    }

    /// <summary>
    /// Stores data regarding a specific key
    /// point and value in an animation.
    /// </summary>
    public class KeyFrame : AvaloniaList<IAnimationSetter>
    {
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

        internal KeyFrameTimingMode TimingMode { get; private set; }

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
                if (TimingMode == KeyFrameTimingMode.Cue)
                {
                    throw new InvalidOperationException($"You can only set either {nameof(KeyTime)} or {nameof(Cue)}.");
                }
                TimingMode = KeyFrameTimingMode.TimeSpan;
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
                if (TimingMode == KeyFrameTimingMode.TimeSpan)
                {
                    throw new InvalidOperationException($"You can only set either {nameof(KeyTime)} or {nameof(Cue)}.");
                }
                TimingMode = KeyFrameTimingMode.Cue;
                _kCue = value;
            }
        }


    }

}
