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
    /// <summary>
    /// Setter that handles <see cref="double"/> properties
    /// in the target.
    /// </summary>
    [Animator(typeof(DoubleAnimator))]
    public class DoubleSetter : AnimationSetter
    {

    }
}