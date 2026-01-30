using System;
using Avalonia.Animation.Easings;
using Avalonia.Data;
using Avalonia.Metadata;

namespace Avalonia.Animation
{
    public class ExpressionKeyFrame : CompositionKeyFrame
    {
    }

    public class CompositionKeyFrame : AvaloniaObject
    {
        public static readonly StyledProperty<object?> ValueProperty =
            AvaloniaProperty.Register<CompositionKeyFrame, object?>(
                nameof(Value));

        public static readonly StyledProperty<Easing?> EasingProperty =
            AvaloniaProperty.Register<CompositionKeyFrame, Easing?>(
                nameof(Easing));

        [Content]
        [AssignBinding]
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

        internal CompositionKeyFrameInstance Instance(Visual target)
        {
            var instance = new CompositionKeyFrameInstance(this);
            if (Value is BindingBase bindingBase)
            {
                // var expression = bindingBase
                //     .CreateInstance(target, CompositionKeyFrameInstance.InstanceValueProperty, null);
                instance.Expression = instance.Bind(
                    CompositionKeyFrameInstance.InstanceValueProperty, bindingBase, target);  
                //
                // expression.Attach(
                //     instance.GetValueStore(), null, instance,
                //     CompositionKeyFrameInstance.InstanceValueProperty, BindingPriority.LocalValue);
                // instance.Expression = expression;
            }
            else
            {
                instance.SetValue(CompositionKeyFrameInstance.InstanceValueProperty, Value);
            }

            instance.NormalizedProgressKey = NormalizedProgressKey;
            instance.Easing = Easing;

            return instance;
        }
    }

    internal class CompositionKeyFrameInstance(CompositionKeyFrame keyFrame) : AvaloniaObject, IDisposable
    {
        public CompositionKeyFrame KeyFrame { get; } = keyFrame;

        public static readonly StyledProperty<object?> InstanceValueProperty
            = AvaloniaProperty.Register<CompositionKeyFrameInstance, object?>(
                nameof(Value));

        public object? Value => GetValue(InstanceValueProperty);

        public float NormalizedProgressKey { get; set; }
        public Easing? Easing { get; set; }

        internal BindingExpressionBase? Expression { get; set; }

        public void Dispose()
        {
            Expression?.Dispose();
        }
    }
}
