using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="SolidColorBrush"/> values. 
    /// </summary>
    public class SolidColorBrushAnimator : Animator<IBrush>
    {
        public override IBrush Interpolate(double progress, IBrush oldValue, IBrush newValue)
        {
            if (!(oldValue is ISolidColorBrush oldSCB) || !(newValue is ISolidColorBrush newSCB))
                return Brushes.Transparent;

            return new ImmutableSolidColorBrush(ColorAnimator.InterpolateCore(progress, oldSCB.Color, newSCB.Color));
        }
    }
}
