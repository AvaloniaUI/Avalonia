// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Data
{
    /// <summary>
    /// Represents a recoverable binding error.
    /// </summary>
    /// <remarks>
    /// When produced by a binding source observable, informs the binding system that an error
    /// occurred. It can also provide an optional fallback value to be pushed to the binding
    /// target. 
    /// 
    /// Instead of using <see cref="BindingError"/>, one could simply not push a value (in the
    /// case of a no fallback value) or push a fallback value, but BindingError also causes an
    /// error to be logged with the correct binding target.
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
        /// Initializes a new instance of the <see cref="BindingError"/> class.
        /// </summary>
        /// <param name="exception">An exception describing the binding error.</param>
        /// <param name="fallbackValue">The fallback value.</param>
        public BindingError(Exception exception, object fallbackValue)
        {
            Exception = exception;
            FallbackValue = fallbackValue;
            UseFallbackValue = true;
        }

        /// <summary>
        /// Gets the exception describing the binding error.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Get the fallback value.
        /// </summary>
        public object FallbackValue { get; }

        /// <summary>
        /// Get a value indicating whether the fallback value should be pushed to the binding
        /// target.
        /// </summary>
        public bool UseFallbackValue { get; }
    }
}
