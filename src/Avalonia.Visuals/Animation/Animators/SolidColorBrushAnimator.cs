using System;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="SolidColorBrush"/> values. 
    /// </summary>
    public class ISolidColorBrushAnimator : Animator<ISolidColorBrush>
    {
        public override ISolidColorBrush Interpolate(double progress, ISolidColorBrush oldValue, ISolidColorBrush newValue)
        {
            return new ImmutableSolidColorBrush(ColorAnimator.InterpolateCore(progress, oldValue.Color, newValue.Color));
        }
    }
    
    [Obsolete]    
    public class SolidColorBrushAnimator : Animator<SolidColorBrush>
    {    
        public override SolidColorBrush Interpolate(double progress, SolidColorBrush oldValue, SolidColorBrush newValue)
        {
            return new SolidColorBrush(ColorAnimator.InterpolateCore(progress, oldValue.Color, newValue.Color));
        }
    }
}
