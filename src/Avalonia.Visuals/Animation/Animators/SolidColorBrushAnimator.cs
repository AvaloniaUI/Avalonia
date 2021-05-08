using System;
using System.Reactive.Disposables;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="SolidColorBrush"/>. 
    /// </summary>
    public class SolidColorBrushAnimator : Animator<IBrush>
    {
        public override IBrush Interpolate(double progress, IBrush oldValue, IBrush newValue)
        {
            if (oldValue is not ISolidColorBrush oldValS || newValue is not ISolidColorBrush newValS)
                return Brushes.Transparent;
            
            return new ImmutableSolidColorBrush(ColorAnimator.InterpolateCore(progress, oldValS.Color, newValS.Color));
        }
    }
}
