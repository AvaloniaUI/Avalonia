using System;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;

#nullable enable

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="SolidColorBrush"/> values. 
    /// </summary>
    internal class ISolidColorBrushAnimator : Animator<ISolidColorBrush?>
    {
        private static readonly DoubleAnimator s_doubleAnimator = new DoubleAnimator();

        public override ISolidColorBrush? Interpolate(double progress, ISolidColorBrush? oldValue, ISolidColorBrush? newValue)
        {
            if (oldValue is null || newValue is null)
            {
                return progress >= 0.5 ? newValue : oldValue;
            }

            return new ImmutableSolidColorBrush(
                ColorAnimator.InterpolateCore(progress, oldValue.Color, newValue.Color),
                s_doubleAnimator.Interpolate(progress, oldValue.Opacity, newValue.Opacity));
        }

        public override IDisposable BindAnimation(Animatable control, IObservable<ISolidColorBrush?> instance)
        {
            if (Property is null)
            {
                throw new InvalidOperationException("Animator has no property specified.");
            }

            return control.Bind((AvaloniaProperty<IBrush?>)Property, instance, BindingPriority.Animation);
        }
    }
    
}
