using System;
using System.Collections.Generic;

namespace Avalonia.Data
{
    /// <summary>
    /// An optional typed value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <remarks>
    /// This struct is similar to <see cref="Nullable{T}"/> except it also accepts reference types:
    /// note that null is a valid value for reference types. It is also similar to
    /// <see cref="BindingValue{T}"/> but has only two states: "value present" and "value missing".
    /// 
    /// To create a new optional value you can:
    /// 
    /// - For a simple value, call the <see cref="Optional{T}"/> constructor or use an implicit
    ///   conversion from <typeparamref name="T"/>
    /// - For an missing value, use <see cref="Empty"/> or simply `default`
    /// </remarks>
    public readonly struct Optional<T> : IEquatable<Optional<T>>
    {
        private readonly T _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="Optional{T}"/> struct with value.
        /// </summary>
        /// <param name="value">The value.</param>
        public Optional(T value)
        {
            _value = value;
            HasValue = true;
        }

        /// <summary>
        /// Gets a value indicating whether a value is present.
        /// </summary>
        public bool HasValue { get; }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// <see cref="HasValue"/> is false.
        /// </exception>
        public T Value => HasValue ? _value : throw new InvalidOperationException("Optional has no value.");

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is Optional<T> o && this == o;

        /// <inheritdoc/>
        public bool Equals(Optional<T> other) => this == other;

        /// <inheritdoc/>
        public override int GetHashCode() => HasValue ? _value?.GetHashCode() ?? 0 : 0;

        /// <summary>
        /// Casts the value (if any) to an <see cref="object"/>.
        /// </summary>
        /// <returns>The cast optional value.</returns>
        public Optional<object?> ToObject() => HasValue ? new Optional<object?>(_value) : default;

        /// <inheritdoc/>
        public override string ToString() => HasValue ? _value?.ToString() ?? "(null)" : "(empty)";

        /// <summary>
        /// Gets the value if present, otherwise the default value.
        /// </summary>
        /// <returns>The value.</returns>
        public T? GetValueOrDefault() => HasValue ? _value : default;

        /// <summary>
        /// Gets the value if present, otherwise a default value.
        /// </summary>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The value.</returns>
        public T? GetValueOrDefault(T defaultValue) => HasValue ? _value : defaultValue;

        /// <summary>
        /// Gets the value if present, otherwise the default value.
        /// </summary>
        /// <returns>
        /// The value if present and of the correct type, `default(TResult)` if the value is
        /// not present or of an incorrect type.
        /// </returns>
        public TResult? GetValueOrDefault<TResult>()
        {
            return HasValue ?
                _value is TResult result ? result : default
                : default;
        }

        /// <summary>
        /// Gets the value if present, otherwise a default value.
        /// </summary>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>
        /// The value if present and of the correct type, `default(TResult)` if the value is
        /// present but not of the correct type or null, or <paramref name="defaultValue"/> if the
        /// value is not present.
        /// </returns>
        public TResult? GetValueOrDefault<TResult>(TResult defaultValue)
        {
            return HasValue ?
                _value is TResult result ? result : default
                : defaultValue;
        }

        /// <summary>
        /// Creates an <see cref="Optional{T}"/> from an instance of the underlying value type.
        /// </summary>
        /// <param name="value">The value.</param>
        public static implicit operator Optional<T>(T value) => new Optional<T>(value);

        /// <summary>
        /// Compares two <see cref="Optional{T}"/>s for inequality.
        /// </summary>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <returns>True if the values are unequal; otherwise false.</returns>
        public static bool operator !=(Optional<T> x, Optional<T> y) => !(x == y);

        /// <summary>
        /// Compares two <see cref="Optional{T}"/>s for equality.
        /// </summary>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <returns>True if the values are equal; otherwise false.</returns>
        public static bool operator ==(Optional<T> x, Optional<T> y)
        {
            if (!x.HasValue && !y.HasValue)
            {
                return true;
            }
            else if (x.HasValue && y.HasValue)
            {
                return EqualityComparer<T>.Default.Equals(x.Value, y.Value);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns an <see cref="Optional{T}"/> without a value.
        /// </summary>
        public static Optional<T> Empty => default;
    }

    public static class OptionalExtensions
    {
        /// <summary>
        /// Casts the type of an <see cref="Optional{T}"/> using only the C# cast operator.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="value">The binding value.</param>
        /// <returns>The cast value.</returns>
        public static Optional<T> Cast<T>(this Optional<object?> value)
        {
            return value.HasValue ? new Optional<T>((T)value.Value!) : Optional<T>.Empty;
        }
    }
}
