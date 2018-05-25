using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reactive.Linq;
using System.Diagnostics;
using Avalonia.Animation.Utils;
using Avalonia.Data;
using Avalonia.Media;

namespace Avalonia.Animation
{
    /// <summary>
    /// Setter that handles <see cref="Transform"/> objects
    /// in the target.
    /// </summary>
    [Animator(typeof(TransformAnimator))]
    public class TransformSetter : AnimationSetter
    { 
        
    }
}
