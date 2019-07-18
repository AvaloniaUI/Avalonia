// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Data.Converters;
using Avalonia.Logging;
using Avalonia.Reactive;
using Avalonia.Utilities;

namespace Avalonia.Data.Core
{
    /// <summary>
    /// Binds to an expression on an object using a type value converter to convert the values
    /// that are sent and received.
    /// </summary>
    public class BindingExpression : LightweightObservableBase<object>, ISubject<object>, IDescription
    {
        private readonly ExpressionObserver _inner;
        private readonly Type _targetType;
        private readonly object _fallbackValue;
        private readonly BindingPriority _priority;
        InnerListener _innerListener;
        WeakReference<object> _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionObserver"/> class.
        /// </summary>
        /// <param name="inner">The <see cref="ExpressionObserver"/>.</param>
        /// <param name="targetType">The type to convert the value to.</param>
        public BindingExpression(ExpressionObserver inner, Type targetType)
            : this(inner, targetType, DefaultValueConverter.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionObserver"/> class.
        /// </summary>
        /// <param name="inner">The <see cref="ExpressionObserver"/>.</param>
        /// <param name="targetType">The type to convert the value to.</param>
        /// <param name="converter">The value converter to use.</param>
        /// <param name="converterParameter">
        /// A parameter to pass to <paramref name="converter"/>.
        /// </param>
        /// <param name="priority">The binding priority.</param>
        public BindingExpression(
            ExpressionObserver inner,
            Type targetType,
            IValueConverter converter,
            object converterParameter = null,
            BindingPriority priority = BindingPriority.LocalValue)
            : this(inner, targetType, AvaloniaProperty.UnsetValue, converter, converterParameter, priority)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionObserver"/> class.
        /// </summary>
        /// <param name="inner">The <see cref="ExpressionObserver"/>.</param>
        /// <param name="targetType">The type to convert the value to.</param>
        /// <param name="fallbackValue">
        /// The value to use when the binding is unable to produce a value.
        /// </param>
        /// <param name="converter">The value converter to use.</param>
        /// <param name="converterParameter">
        /// A parameter to pass to <paramref name="converter"/>.
        /// </param>
        /// <param name="priority">The binding priority.</param>
        public BindingExpression(
            ExpressionObserver inner, 
            Type targetType,
            object fallbackValue,
            IValueConverter converter,
            object converterParameter = null,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            Contract.Requires<ArgumentNullException>(inner != null);
            Contract.Requires<ArgumentNullException>(targetType != null);
            Contract.Requires<ArgumentNullException>(converter != null);

            _inner = inner;
            _targetType = targetType;
            Converter = converter;
            ConverterParameter = converterParameter;
            _fallbackValue = fallbackValue;
            _priority = priority;
        }

        /// <summary>
        /// Gets the converter to use on the expression.
        /// </summary>
        public IValueConverter Converter { get; }

        /// <summary>
        /// Gets a parameter to pass to <see cref="Converter"/>.
        /// </summary>
        public object ConverterParameter { get; }

        /// <inheritdoc/>
        string IDescription.Description => _inner.Expression;

        /// <inheritdoc/>
        public void OnCompleted()
        {
        }

        /// <inheritdoc/>
        public void OnError(Exception error)
        {
        }

        /// <inheritdoc/>
        public void OnNext(object value)
        {
            if (value == BindingOperations.DoNothing)
            {
                return;
            }

            using (_inner.Subscribe(_ => { }))
            {
                var type = _inner.ResultType;

                if (type != null)
                {
                    var converted = Converter.ConvertBack(
                        value,
                        type,
                        ConverterParameter,
                        CultureInfo.CurrentCulture);

                    if (converted == BindingOperations.DoNothing)
                    {
                        return;
                    }

                    if (converted == AvaloniaProperty.UnsetValue)
                    {
                        converted = TypeUtilities.Default(type);
                        _inner.SetValue(converted, _priority);
                    }
                    else if (converted is BindingNotification)
                    {
                        var notification = converted as BindingNotification;

                        if (notification.ErrorType == BindingErrorType.None)
                        {
                            throw new AvaloniaInternalException(
                                "IValueConverter should not return non-errored BindingNotification.");
                        }

                        PublishNext(notification);

                        if (_fallbackValue != AvaloniaProperty.UnsetValue)
                        {
                            if (TypeUtilities.TryConvert(
                                type,
                                _fallbackValue,
                                CultureInfo.InvariantCulture,
                                out converted))
                            {
                                _inner.SetValue(converted, _priority);
                            }
                            else
                            {
                                Logger.Error(
                                    LogArea.Binding,
                                    this,
                                    "Could not convert FallbackValue {FallbackValue} to {Type}",
                                    _fallbackValue,
                                    type);
                            }
                        }
                    }
                    else
                    {
                        _inner.SetValue(converted, _priority);
                    }
                }
            }
        }

        protected override void Initialize() => _innerListener = new InnerListener(this);
        protected override void Deinitialize() => _innerListener.Dispose();

        protected override void Subscribed(IObserver<object> observer, bool first)
        {
            if (!first && _value != null && _value.TryGetTarget(out var val))
            {
                observer.OnNext(val);
            }
        }

        /// <inheritdoc/>
        private object ConvertValue(object value)
        {
            if (value == BindingOperations.DoNothing)
            {
                return value;
            }

            var notification = value as BindingNotification;

            if (notification == null)
            {
                var converted = Converter.Convert(
                    value,
                    _targetType,
                    ConverterParameter,
                    CultureInfo.CurrentCulture);

                if (converted == BindingOperations.DoNothing)
                {
                    return converted;
                }

                notification = converted as BindingNotification;

                if (notification?.ErrorType == BindingErrorType.None)
                {
                    converted = notification.Value;
                }

                if (_fallbackValue != AvaloniaProperty.UnsetValue &&
                    (converted == AvaloniaProperty.UnsetValue || converted is BindingNotification))
                {
                    var fallback = ConvertFallback();
                    converted = Merge(converted, fallback);
                }

                return converted;
            }
            else
            {
                return ConvertValue(notification);
            }
        }

        private BindingNotification ConvertValue(BindingNotification notification)
        {
            if (notification.HasValue)
            {
                var converted = ConvertValue(notification.Value);
                notification = Merge(notification, converted);
            }
            else if (_fallbackValue != AvaloniaProperty.UnsetValue)
            {
                var fallback = ConvertFallback();
                notification = Merge(notification, fallback);
            }

            return notification;
        }

        private BindingNotification ConvertFallback()
        {
            object converted;

            if (_fallbackValue == AvaloniaProperty.UnsetValue)
            {
                throw new AvaloniaInternalException("Cannot call ConvertFallback with no fallback value");
            }

            if (TypeUtilities.TryConvert(
                _targetType,
                _fallbackValue,
                CultureInfo.InvariantCulture,
                out converted))
            {
                return new BindingNotification(converted);
            }
            else
            { 
                return new BindingNotification(
                    new InvalidCastException(
                        $"Could not convert FallbackValue '{_fallbackValue}' to '{_targetType}'"),
                    BindingErrorType.Error);
            }
        }

        private static BindingNotification Merge(object a, BindingNotification b)
        {
            var an = a as BindingNotification;

            if (an != null)
            {
                Merge(an, b);
                return an;
            }
            else
            {
                return b;
            }
        }

        private static BindingNotification Merge(BindingNotification a, object b)
        {
            var bn = b as BindingNotification;

            if (bn != null)
            {
                Merge(a, bn);
            }
            else
            {
                a.SetValue(b);
            }

            return a;
        }

        private static BindingNotification Merge(BindingNotification a, BindingNotification b)
        {
            if (b.HasValue)
            {
                a.SetValue(b.Value);
            }
            else
            {
                a.ClearValue();
            }

            if (b.Error != null)
            {
                a.AddError(b.Error, b.ErrorType);
            }

            return a;
        }

        public class InnerListener : IObserver<object>, IDisposable
        {
            private readonly BindingExpression _owner;
            private readonly IDisposable _dispose;

            public InnerListener(BindingExpression owner)
            {
                _owner = owner;
                _dispose = owner._inner.Subscribe(this);
            }

            public void Dispose() => _dispose.Dispose();
            public void OnCompleted() => _owner.PublishCompleted();
            public void OnError(Exception error) => _owner.PublishError(error);

            public void OnNext(object value)
            {
                if (value == BindingOperations.DoNothing)
                {
                    return;
                }

                var converted = _owner.ConvertValue(value);

                if (converted == BindingOperations.DoNothing)
                {
                    return;
                }

                _owner._value = new WeakReference<object>(converted);
                _owner.PublishNext(converted);
            }
        }
    }
}
