using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Styling.Activators;
using Avalonia.Utilities;

namespace Avalonia.Styling
{
    /// <summary>
    /// A selector that matches the common case of a type and/or name followed by a collection of
    /// style classes and pseudoclasses.
    /// </summary>
    internal class PropertyEqualsSelector : Selector
    {
        private readonly Selector? _previous;
        private readonly AvaloniaProperty _property;
        private readonly object? _value;
        private string? _selectorString;

        public PropertyEqualsSelector(Selector? previous, AvaloniaProperty property, object? value)
        {
            property = property ?? throw new ArgumentNullException(nameof(property));

            _previous = previous;
            _property = property;
            _value = value;
        }

        /// <inheritdoc/>
        internal override bool InTemplate => _previous?.InTemplate ?? false;

        /// <inheritdoc/>
        internal override bool IsCombinator => false;

        /// <inheritdoc/>
        internal override Type? TargetType => _previous?.TargetType;

        /// <inheritdoc/>
        public override string ToString(Style? owner)
        {
            if (_selectorString == null)
            {
                var builder = StringBuilderCache.Acquire();

                if (_previous != null)
                {
                    builder.Append(_previous.ToString(owner));
                }

                builder.Append('[');

                if (_property.IsAttached)
                {
                    builder.Append('(');
                    builder.Append(_property.OwnerType.Name);
                    builder.Append('.');
                }

                builder.Append(_property.Name);
                if (_property.IsAttached)
                {
                    builder.Append(')');
                }
                builder.Append('=');
                builder.Append(_value ?? string.Empty);
                builder.Append(']');

                _selectorString = StringBuilderCache.GetStringAndRelease(builder);
            }

            return _selectorString;
        }

        /// <inheritdoc/>
        private protected override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe)
        {
            if (subscribe)
            {
                return new SelectorMatch(new PropertyEqualsActivator(control, _property, _value));
            }
            else
            {
                return Compare(_property.PropertyType, control.GetValue(_property), _value)
                    ? SelectorMatch.AlwaysThisInstance
                    : SelectorMatch.NeverThisInstance;
            }
            
        }

        private protected override Selector? MovePrevious() => _previous;
        private protected override Selector? MovePreviousOrParent() => _previous;

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = TrimmingMessages.TypeConversionSupressWarningMessage)]
        [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = TrimmingMessages.TypeConversionSupressWarningMessage)]
        internal static bool Compare(Type propertyType, object? propertyValue, object? value)
        {
            if (propertyType == typeof(object) &&
                propertyValue?.GetType() is Type inferredType)
            {
                propertyType = inferredType;
            }

            var valueType = value?.GetType();

            if (valueType is null || propertyType.IsAssignableFrom(valueType))
            {
                return Equals(propertyValue, value);
            }

            var converter = TypeDescriptor.GetConverter(propertyType);
            if (converter?.CanConvertFrom(valueType) == true)
            {
                return Equals(propertyValue, converter.ConvertFrom(null, CultureInfo.InvariantCulture, value!));
            }

            return false;
        }
    }
}
