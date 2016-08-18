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

        // Null cannot be held in WeakReference as it's indistinguishable from an expired value so
        // use this value in its place.
        private static readonly object NullValue = new object();

        private WeakReference<object> _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="BindingNotification"/> class.
        /// </summary>
        /// <param name="value">The binding value.</param>
        public BindingNotification(object value)
        {
            _value = new WeakReference<object>(value ?? NullValue);
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
            _value = new WeakReference<object>(fallbackValue ?? NullValue);
        }

        /// <summary>
        /// Gets the value that should be passed to the target when <see cref="HasValue"/>
        /// is true.
        /// </summary>
        /// <remarks>
        /// If this property is read when <see cref="HasValue"/> is false then it will return
        /// <see cref="AvaloniaProperty.UnsetValue"/>.
        /// </remarks>
        public object Value
        {
            get
            {
                if (_value != null)
                {
                    object result;

                    if (_value.TryGetTarget(out result))
                    {
                        return result == NullValue ? null : result;
                    }
                }

                // There's the possibility of a race condition in that HasValue can return true,
                // and then the value is GC'd before Value is read. We should be ok though as
                // we return UnsetValue which should be a safe alternative.
                return AvaloniaProperty.UnsetValue;
            }
        }

        /// <summary>
        /// Gets a value indicating whether <see cref="Value"/> should be pushed to the target.
        /// </summary>
        public bool HasValue => _value != null;

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
        /// Gets a value from an object that may be a <see cref="BindingNotification"/>.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <returns>The value.</returns>
        /// <remarks>
        /// If <paramref name="o"/> is a <see cref="BindingNotification"/> then returns the binding
        /// notification's <see cref="Value"/>. If not, returns the object unchanged.
        /// </remarks>
        public static object ExtractValue(object o)
        {
            var notification = o as BindingNotification;
            return notification != null ? notification.Value : o;
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

            Error = Error != null ? new AggregateException(Error, e) : e;

            if (type == BindingErrorType.Error || ErrorType == BindingErrorType.Error)
            {
                ErrorType = BindingErrorType.Error;
            }
        }

        /// <summary>
        /// Removes the <see cref="Value"/> and makes <see cref="HasValue"/> return null.
        /// </summary>
        public void ClearValue()
        {
            _value = null;
        }

        /// <summary>
        /// Sets the <see cref="Value"/>.
        /// </summary>
        public void SetValue(object value)
        {
            _value = new WeakReference<object>(value ?? NullValue);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            switch (ErrorType)
            {
                case BindingErrorType.None:
                    return $"{{Value: {Value}}}";
                default:
                    return HasValue ? 
                        $"{{{ErrorType}: {Error}, Fallback: {Value}}}" : 
                        $"{{{ErrorType}: {Error}}}";
            }
        }

        private static bool ExceptionEquals(Exception a, Exception b)
        {
            return a?.GetType() == b?.GetType() &&
                   a?.Message == b?.Message;
        }
    }
}
