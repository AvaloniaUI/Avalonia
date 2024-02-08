using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Animation.Animators;
using Avalonia.Data;
using Avalonia.Reactive;

namespace Avalonia.Animation
{
    /// <summary>
    /// Defines a KeyFrame that is used for
    /// <see cref="Animator{T}"/> objects.
    /// </summary>
    internal class AnimatorKeyFrame : AvaloniaObject
    {
        public static readonly DirectProperty<AnimatorKeyFrame, object?> ValueProperty =
            AvaloniaProperty.RegisterDirect<AnimatorKeyFrame, object?>(nameof(Value), k => k.Value, (k, v) => k.Value = v);

        public AnimatorKeyFrame(Type? animatorType, Func<IAnimator>? animatorFactory, Cue cue, KeySpline? keySpline)
        {
            AnimatorType = animatorType;
            AnimatorFactory = animatorFactory;
            Cue = cue;
            KeySpline = keySpline;
        }

        public Type? AnimatorType { get; }
        public Func<IAnimator>? AnimatorFactory { get; }
        public Cue Cue { get; }
        public KeySpline? KeySpline { get; }
        public bool FillBefore { get; set; }
        public bool FillAfter { get; set; }
        public AvaloniaProperty? Property { get; private set; }

        private object? _value;

        public object? Value
        {
            get => _value;
            set => SetAndRaise(ValueProperty, ref _value, value);
        }

        public IDisposable BindSetter(IAnimationSetter setter, Animatable targetControl)
        {
            Property = setter.Property;
            var value = setter.Value;

            if (value is IBinding binding)
            {
                return Bind(ValueProperty, binding, targetControl);
            }
            else
            {
                return Bind(ValueProperty, Observable.SingleValue(value).ToBinding(), targetControl);
            }
        }

        [RequiresUnreferencedCode(TrimmingMessages.TypeConversionRequiresUnreferencedCodeMessage)]
        public T GetTypedValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>()
        {
            var typeConv = TypeDescriptor.GetConverter(typeof(T));

            if (Value == null)
            {
                throw new ArgumentNullException($"KeyFrame value can't be null.");
            }
            if(Value is T typedValue)
            {
                return typedValue;
            }
            if (!typeConv.CanConvertTo(Value.GetType()))
            {
                throw new InvalidCastException($"KeyFrame value doesnt match property type.");
            }

            return (T)typeConv.ConvertTo(Value, typeof(T))!;
        }
    }
}
