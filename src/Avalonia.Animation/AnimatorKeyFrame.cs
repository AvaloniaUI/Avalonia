using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Avalonia.Metadata;
using Avalonia.Collections;

namespace Avalonia.Animation
{
    /// <summary>
    /// Defines a KeyFrame that is used for
    /// <see cref="Animator{T}"/> objects.
    /// </summary>
    public class AnimatorKeyFrame
    {
        public Type Handler;
        public Cue Cue;
        public TimeSpan KeyTime;
        internal bool timeSpanSet, cueSet;
        public AvaloniaProperty Property;
        public object Value;
    }
}
