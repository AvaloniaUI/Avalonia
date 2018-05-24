using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Avalonia.Metadata;
using Avalonia.Collections;

namespace Avalonia.Animation
{
    
    public class AnimatorKeyFrame
    {
        public Type Handler;
        public Cue Cue;
        public TimeSpan KeyTime;
        internal bool timeSpanSet, cueSet;
        internal AvaloniaProperty Property;
        public object Value;
    }

}
