// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Data
{
    /// <summary>
    /// Defines the types of binding errors for a <see cref="BindingNotification"/>.
    /// </summary>
    public enum BindingErrorType
    {
        /// <summary>
        /// There was no error.
        /// </summary>
        None,

        /// <summary>
        /// There was a binding error.
        /// </summary>
        Error,

        /// <summary>
        /// There was a data validation error.
        /// </summary>
        DataValidationError,
    }

    /// <summary>
    /// Represents a binding notification that can be a valid binding value, or a binding or
    /// data validation error.
    /// </summary>
    public class BindingNotification
    {
        /// <summary>
        /// A binding notification representing the null value.
        /// </summary>
        public static readonly BindingNotification Null =
            new BindingNotification(null);

        /// <summary>
        /// A binding notification representing <see cref="AvaloniaProperty.UnsetValue"/>.
        /// </summary>
        public static readonly BindingNotification UnsetValue =
            new BindingNotification(AvaloniaProperty.UnsetValue);

        /// <summary>
        /// Initializes a new instance of the <see cref="BindingNotification"/> class.
        /// </summary>
        /// <param name="value">The binding value.</param>
        public BindingNotification(object value)
        {
            Value = value;
            HasValue = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BindingNotification"/> class.
        /// </summary>
        /// <param name="error">The binding error.</param>
        /// <param name="errorType">The type of the binding error.</param>
        public BindingNotification(Exception error, BindingErrorType errorType)
        {
            if (errorType == BindingErrorType.None)
            {
                throw new ArgumentException($"'errorType' may not be None");
            }

            Value = AvaloniaProperty.UnsetValue;
            Error = error;
            ErrorType = errorType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BindingNotification"/> class.
        /// </summary>
        /// <param name="error">The binding error.</param>
        /// <param name="errorType">The type of the binding error.</param>
        /// <param name="fallbackValue">The fallback value.</param>
        public BindingNotification(Exception error, BindingErrorType errorType, object fallbackValue)
            : this(error, errorType)
        {
            Value = fallbackValue;
            HasValue = true;
        }

        /// <summary>
        /// Gets the value that should be passed to the target when <see cref="HasValue"/>
        /// is true.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Gets a value indicating whether <see cref="Value"/> should be pushed to the target.
        /// </summary>
        public bool HasValue { get; set; }

        /// <summary>
        /// Gets the error that occurred on the source, if any.
        /// </summary>
        public Exception Error { get; set; }

        /// <summary>
        /// Gets the type of error that <see cref="Error"/> represents, if any.
        /// </summary>
        public BindingErrorType ErrorType { get; set; }

        /// <summary>
        /// Compares two instances of <see cref="BindingNotification"/> for equality.
        /// </summary>
        /// <param name="a">The first instance.</param>
        /// <param name="b">The second instance.</param>
        /// <returns>true if the two instances are equal; otherwise false.</returns>
        public static bool operator ==(BindingNotification a, BindingNotification b)
        {
            if (object.ReferenceEquals(a, b))
            {
                return true;
            }

            if ((object)a == null || (object)b == null)
            {
                return false;
            }

            return a.HasValue == b.HasValue &&
                   a.ErrorType == b.ErrorType &&
                   (!a.HasValue || object.Equals(a.Value, b.Value)) &&
                   (a.ErrorType == BindingErrorType.None || ExceptionEquals(a.Error, b.Error));
        }

        /// <summary>
        /// Compares two instances of <see cref="BindingNotification"/> for inequality.
        /// </summary>
        /// <param name="a">The first instance.</param>
        /// <param name="b">The second instance.</param>
        /// <returns>true if the two instances are unequal; otherwise false.</returns>
        public static bool operator !=(BindingNotification a, BindingNotification b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Compares an object to an instance of <see cref="BindingNotification"/> for equality.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>true if the two instances are equal; otherwise false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as BindingNotification);
        }

        /// <summary>
        /// Compares a value to an instance of <see cref="BindingNotification"/> for equality.
        /// </summary>
        /// <param name="other">The value to compare.</param>
        /// <returns>true if the two instances are equal; otherwise false.</returns>
        public bool Equals(BindingNotification other)
        {
            return this == other;
        }

        /// <summary>
        /// Gets the hash code for this instance of <see cref="BindingNotification"/>. 
        /// </summary>
        /// <returns>A hash code.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Adds an error to the <see cref="BindingNotification"/>.
        /// </summary>
        /// <param name="e">The error to add.</param>
        /// <param name="type">The error type.</param>
        public void AddError(Exception e, BindingErrorType type)
        {
            Contract.Requires<ArgumentNullException>(e != null);
            Contract.Requires<ArgumentException>(type != BindingErrorType.None);

            if (Error != null)
            {
                Error = new AggregateException(Error, e);
            }
            else
            {
                Error = e;
            }

            if (type == BindingErrorType.Error || ErrorType == BindingErrorType.Error)
            {
                ErrorType = BindingErrorType.Error;
            }
        }

        private static bool ExceptionEquals(Exception a, Exception b)
        {
            return a?.GetType() == b?.GetType() &&
                   a.Message == b.Message;
        }
    }
}
