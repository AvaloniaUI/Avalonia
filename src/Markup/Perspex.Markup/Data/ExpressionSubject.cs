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
        private IValueConverter _converter;
        private ExpressionObserver _inner;
        private Type _targetType;

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
        public ExpressionSubject(ExpressionObserver inner, Type targetType, IValueConverter converter)
        {
            _converter = converter;
            _inner = inner;
            _targetType = targetType;
        }

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
                object converted;

                if (ConvertBack(value, type, out converted))
                {
                    _inner.SetValue(converted);
                }
            }
        }

        /// <inheritdoc/>
        public IDisposable Subscribe(IObserver<object> observer)
        {
            return _inner
                .Select(x => Convert(x, _targetType))
                .Subscribe(observer);
        }

        private object Convert(object value, Type type)
        {
            try
            {
                if (value == null || value == PerspexProperty.UnsetValue)
                {
                    return TypeUtilities.Default(type);
                }
                else
                {
                    return _converter.Convert(value, type, null, CultureInfo.CurrentUICulture);
                }
            }
            catch
            {
                // TODO: Log something.
                return PerspexProperty.UnsetValue;
            }
        }

        private bool ConvertBack(object value, Type type, out object result)
        {
            try
            {
                if (value == null || value == PerspexProperty.UnsetValue)
                {
                    result = TypeUtilities.Default(type);
                    return true;
                }
                else
                {
                    result = _converter.ConvertBack(value, type, null, CultureInfo.CurrentUICulture);
                    return true;
                }
            }
            catch
            {
                // TODO: Log something.
                result = null;
                return false;
            }
        }
    }
}
