// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Markup.Binding
{
    /// <summary>
    /// Holds the value for an <see cref="ExpressionObserver"/>.
    /// </summary>
    public struct ExpressionValue
    {
        /// <summary>
        /// An <see cref="ExpressionValue"/> that has no value.
        /// </summary>
        public static readonly ExpressionValue None = new ExpressionValue();

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionValue"/> struct.
        /// </summary>
        /// <param name="value"></param>
        public ExpressionValue(object value)
        {
            HasValue = true;
            Value = value;
        }

        /// <summary>
        /// Gets a value indicating whether the evaluated expression resulted in a value.
        /// </summary>
        public bool HasValue { get; }

        /// <summary>
        /// Gets a the result of the expression.
        /// </summary>
        public object Value { get; }
    }
}
