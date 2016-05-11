// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Markup.Data.Parsers;

namespace Avalonia.Markup.Data
{
    /// <summary>
    /// Exception thrown when <see cref="ExpressionObserver"/> could not parse the provided
    /// expression string.
    /// </summary>
    public class ExpressionParseException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionParseException"/> class.
        /// </summary>
        /// <param name="column">The column position of the error.</param>
        /// <param name="message">The exception message.</param>
        public ExpressionParseException(int column, string message)
            : base(message)
        {
            Column = column;
        }

        /// <summary>
        /// Gets the column position at which the error occurred.
        /// </summary>
        public int Column { get; }
    }
}
