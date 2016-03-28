// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Data
{
    /// <summary>
    /// Represents a recoverable binding error.
    /// </summary>
    /// <remarks>
    /// When produced by a binding source observable, informs the binding system that an error
    /// occurred. It causes a binding error to be logged: the value of the bound property will not
    /// change.
    /// </remarks>
    public class BindingError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BindingError"/> class.
        /// </summary>
        /// <param name="exception">An exception describing the binding error.</param>
        public BindingError(Exception exception)
        {
            Exception = exception;
        }

        /// <summary>
        /// Gets the exception describing the binding error.
        /// </summary>
        public Exception Exception { get; }
    }
}
