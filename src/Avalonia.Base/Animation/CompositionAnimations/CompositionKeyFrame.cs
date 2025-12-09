using System.Numerics;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Media;

namespace Avalonia.Animation
{
    public class ExpressionKeyFrame : CompositionKeyFrame
    {
    }

    public class CompositionKeyFrame : AvaloniaObject
    {
        public static readonly StyledProperty<object?> ValueProperty = AvaloniaProperty.Register<CompositionKeyFrame, object?>(
            nameof(Value));

        public static readonly StyledProperty<Easing?> EasingProperty = AvaloniaProperty.Register<CompositionKeyFrame, Easing?>(
            nameof(Easing));

        public object? Value
        {
            get => GetValue(ValueProperty); 
            set => SetValue(ValueProperty, value);
        }

        public Easing? Easing
        {
            get => GetValue(EasingProperty);
            set => SetValue(EasingProperty, value);
        }

        public float NormalizedProgressKey { get; set; }
    }
}
