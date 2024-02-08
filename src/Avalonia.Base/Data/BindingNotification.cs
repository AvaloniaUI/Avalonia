using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
    /// <remarks>
    /// This class is very similar to <see cref="BindingValue{T}"/>, but where <see cref="BindingValue{T}"/>
    /// is used by typed bindings, this class is used to hold binding and data validation errors in
    /// untyped bindings. As Avalonia moves towards using typed bindings by default we may want to remove
    /// this class.
    /// </remarks>
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

        private object? _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="BindingNotification"/> class.
        /// </summary>
        /// <param name="value">The binding value.</param>
        public BindingNotification(object? value)
        {
            Debug.Assert(value is not BindingNotification);
            _value = value;
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
            _value = AvaloniaProperty.UnsetValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BindingNotification"/> class.
        /// </summary>
        /// <param name="error">The binding error.</param>
        /// <param name="errorType">The type of the binding error.</param>
        /// <param name="fallbackValue">The fallback value.</param>
        public BindingNotification(Exception error, BindingErrorType errorType, object? fallbackValue)
            : this(error, errorType)
        {
            _value = fallbackValue;
        }

        /// <summary>
        /// Gets the value that should be passed to the target when <see cref="HasValue"/>
        /// is true.
        /// </summary>
        /// <remarks>
        /// If this property is read when <see cref="HasValue"/> is false then it will return
        /// <see cref="AvaloniaProperty.UnsetValue"/>.
        /// </remarks>
        public object? Value => _value;

        /// <summary>
        /// Gets a value indicating whether <see cref="Value"/> should be pushed to the target.
        /// </summary>
        public bool HasValue => _value != AvaloniaProperty.UnsetValue;

        /// <summary>
        /// Gets the error that occurred on the source, if any.
        /// </summary>
        public Exception? Error { get; set; }

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
        public static bool operator ==(BindingNotification? a, BindingNotification? b)
        {
            if (object.ReferenceEquals(a, b))
            {
                return true;
            }

            if (a is null || b is null)
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
        public static bool operator !=(BindingNotification? a, BindingNotification? b)
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
        public static object? ExtractValue(object? o)
        {
            var notification = o as BindingNotification;
            return notification is not null ? notification.Value : o;
        }

        /// <summary>
        /// Updates the value of an object that may be a <see cref="BindingNotification"/>.
        /// </summary>
        /// <param name="o">The object that may be a binding notification.</param>
        /// <param name="value">The new value.</param>
        /// <returns>
        /// The updated binding notification if <paramref name="o"/> is a binding notification;
        /// otherwise <paramref name="value"/>.
        /// </returns>
        /// <remarks>
        /// If <paramref name="o"/> is a <see cref="BindingNotification"/> then sets its value
        /// to <paramref name="value"/>. If <paramref name="value"/> is a
        /// <see cref="BindingNotification"/> then the value will first be extracted.
        /// </remarks>
        public static object? UpdateValue(object o, object value)
        {
            if (o is BindingNotification n)
            {
                n.SetValue(ExtractValue(value));
                return n;
            }

            return value;
        }

        /// <summary>
        /// Gets an exception from an object that may be a <see cref="BindingNotification"/>.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <returns>The value.</returns>
        /// <remarks>
        /// If <paramref name="o"/> is a <see cref="BindingNotification"/> then returns the binding
        /// notification's <see cref="Error"/>. If not, returns the object unchanged.
        /// </remarks>
        public static object? ExtractError(object? o)
        {
            return o is BindingNotification notification ? notification.Error : o;
        }

        /// <summary>
        /// Compares an object to an instance of <see cref="BindingNotification"/> for equality.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>true if the two instances are equal; otherwise false.</returns>
        public override bool Equals(object? obj)
        {
            return Equals(obj as BindingNotification);
        }

        /// <summary>
        /// Compares a value to an instance of <see cref="BindingNotification"/> for equality.
        /// </summary>
        /// <param name="other">The value to compare.</param>
        /// <returns>true if the two instances are equal; otherwise false.</returns>
        public bool Equals(BindingNotification? other)
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
            _ = e ?? throw new ArgumentNullException(nameof(e));

            if (type == BindingErrorType.None)
                throw new ArgumentException("BindingErrorType may not be None", nameof(type));

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
            _value = AvaloniaProperty.UnsetValue;
        }

        /// <summary>
        /// Sets the <see cref="Value"/>.
        /// </summary>
        public void SetValue(object? value)
        {
            _value = value;
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

        private static bool ExceptionEquals(Exception? a, Exception? b)
        {
            return a?.GetType() == b?.GetType() &&
                   a?.Message == b?.Message;
        }
    }

    internal static class BindingErrorTypeExtensions
    {
        public static BindingValueType ToBindingValueType(this BindingErrorType type)
        {
            return type switch
            {
                BindingErrorType.Error => BindingValueType.BindingError,
                BindingErrorType.DataValidationError => BindingValueType.DataValidationError,
                _ => BindingValueType.Value,
            };
        }
    }
}
