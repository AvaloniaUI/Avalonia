// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Perspex.Utilities;

namespace Perspex.Markup.Data
{
    /// <summary>
    /// Turns an <see cref="ExpressionObserver"/> into a subject that can be bound two-way with
    /// a value converter.
    /// </summary>
    public class ExpressionSubject : ISubject<object>, IDescription
    {
        private readonly ExpressionObserver _inner;
        private readonly Type _targetType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionObserver"/> class.
        /// </summary>
        /// <param name="inner">The <see cref="ExpressionObserver"/>.</param>
        /// <param name="targetType">The type to convert the value to.</param>
        public ExpressionSubject(ExpressionObserver inner, Type targetType)
            : this(inner, targetType, DefaultValueConverter.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionObserver"/> class.
        /// </summary>
        /// <param name="inner">The <see cref="ExpressionObserver"/>.</param>
        /// <param name="targetType">The type to convert the value to.</param>
        /// <param name="converter">The value converter to use.</param>
        /// <param name="converterParameter">A parameter to pass to <paramref name="converter"/>.</param>
        public ExpressionSubject(
            ExpressionObserver inner, 
            Type targetType, 
            IValueConverter converter,
            object converterParameter = null)
        {
            Contract.Requires<ArgumentNullException>(inner != null);
            Contract.Requires<ArgumentNullException>(targetType != null);
            Contract.Requires<ArgumentNullException>(converter != null);

            _inner = inner;
            _targetType = targetType;
            Converter = converter;
            ConverterParameter = converterParameter;
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
            var type = _inner.ResultType;

            if (type != null)
            {
                var converted = Converter.ConvertBack(
                    value, 
                    type, 
                    ConverterParameter, 
                    CultureInfo.CurrentUICulture);

                if (converted == PerspexProperty.UnsetValue)
                {
                    converted = TypeUtilities.Default(type);
                }

                _inner.SetValue(converted);
            }
        }

        /// <inheritdoc/>
        public IDisposable Subscribe(IObserver<object> observer)
        {
            return _inner
                .Select(x => Converter.Convert(
                    x, 
                    _targetType, 
                    ConverterParameter, 
                    CultureInfo.CurrentUICulture))
                .Subscribe(observer);
        }
    }
}
