using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reactive.Linq;
using System.Diagnostics;
using Avalonia.Animation.Utils;
using Avalonia.Data;

namespace Avalonia.Animation
{
    public abstract class AnimationSetter : IAnimationSetter
    {
        public AvaloniaProperty Property { get; set; }
        public object Value { get; set; }
    }
}
