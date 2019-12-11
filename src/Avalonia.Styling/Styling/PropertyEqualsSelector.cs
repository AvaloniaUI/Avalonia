// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Text;

namespace Avalonia.Styling
{
    /// <summary>
    /// A selector that matches the common case of a type and/or name followed by a collection of
    /// style classes and pseudoclasses.
    /// </summary>
    internal class PropertyEqualsSelector : Selector
    {
        private readonly Selector _previous;
        private readonly AvaloniaProperty _property;
        private readonly object _value;
        private string _selectorString;

        public PropertyEqualsSelector(Selector previous, AvaloniaProperty property, object value)
        {
            Contract.Requires<ArgumentNullException>(property != null);

            _previous = previous;
            _property = property;
            _value = value;
        }

        /// <inheritdoc/>
        public override bool InTemplate => _previous?.InTemplate ?? false;

        /// <inheritdoc/>
        public override bool IsCombinator => false;

        /// <summary>
        /// Gets the name of the control to match.
        /// </summary>
        public string Name { get; private set; }

        /// <inheritdoc/>
        public override Type TargetType => _previous?.TargetType;

        /// <inheritdoc/>
        public override string ToString()
        {
            if (_selectorString == null)
            {
                var builder = new StringBuilder();

                if (_previous != null)
                {
                    builder.Append(_previous.ToString());
                }

                builder.Append('[');

                if (_property.IsAttached)
                {
                    builder.Append(_property.OwnerType.Name);
                    builder.Append('.');
                }

                builder.Append(_property.Name);
                builder.Append('=');
                builder.Append(_value ?? string.Empty);
                builder.Append(']');

                _selectorString = builder.ToString();
            }

            return _selectorString;
        }

        /// <inheritdoc/>
        protected override SelectorMatch Evaluate(IStyleable control, bool subscribe)
        {
            if (subscribe)
            {
                return new SelectorMatch(control.GetObservable(_property).Select(v => Equals(v ?? string.Empty, _value)));
            }
            else
            {
                var result = (control.GetValue(_property) ?? string.Empty).Equals(_value);
                return result ? SelectorMatch.AlwaysThisInstance : SelectorMatch.NeverThisInstance;
            }
        }

        protected override Selector MovePrevious() => _previous;
    }
}
