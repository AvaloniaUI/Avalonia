using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Avalonia.Metadata;
using Avalonia.Collections;
using Avalonia.Data;
using Avalonia.Reactive;

namespace Avalonia.Animation
{
    /// <summary>
    /// Defines a KeyFrame that is used for
    /// <see cref="Animator{T}"/> objects.
    /// </summary>
    public class AnimatorKeyFrame : AvaloniaObject
    {
        public static readonly DirectProperty<AnimatorKeyFrame, object> ValueProperty =
            AvaloniaProperty.RegisterDirect<AnimatorKeyFrame, object>(nameof(Value), k => k._value, (k, v) => k._value = v);

        public AnimatorKeyFrame()
        {

        }

        public AnimatorKeyFrame(Type animatorType, Cue cue)
        {
            AnimatorType = animatorType;
            Cue = cue;
        }

        public Type AnimatorType { get; }
        public Cue Cue { get; }
        public AvaloniaProperty Property { get; private set; }

        private object _value;

        public object Value
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
                return this.Bind(ValueProperty, binding, targetControl);
            }
            else
            {
                return this.Bind(ValueProperty, ObservableEx.SingleValue(value).ToBinding(), targetControl);
            }
        }

        public T GetTypedValue<T>()
        {
            var typeConv = TypeDescriptor.GetConverter(typeof(T));

            if (Value == null)
            {
                throw new ArgumentNullException($"KeyFrame value can't be null.");
            }
            if (!typeConv.CanConvertTo(Value.GetType()))
            {
                throw new InvalidCastException($"KeyFrame value doesnt match property type.");
            }

            return (T)typeConv.ConvertTo(Value, typeof(T));
        }
    }
}
