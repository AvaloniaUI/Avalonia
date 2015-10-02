// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Perspex.Markup.Binding
{
    /// <summary>
    /// Turns an <see cref="ExpressionObserver"/> into a subject that can be bound two-ways.
    /// </summary>
    public class ExpressionSubject : ISubject<object>
    {
        private ExpressionObserver _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionObserver"/> class.
        /// </summary>
        /// <param name="inner">The <see cref="ExpressionObserver"/>.</param>
        public ExpressionSubject(ExpressionObserver inner)
        {
            _inner = inner;
        }

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
            _inner.SetValue(value);
        }

        /// <inheritdoc/>
        public IDisposable Subscribe(IObserver<object> observer)
        {
            return _inner.Select(x => x.Value).Subscribe(observer);
        }
    }
}
