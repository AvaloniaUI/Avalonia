// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Text;

namespace Perspex.Styling
{
    /// <summary>
    /// A selector that matches the common case of a type and/or name followed by a collection of
    /// style classes and pseudoclasses.
    /// </summary>
    internal class PropertyEqualsSelector : Selector
    {
        private readonly Selector _previous;
        private readonly PerspexProperty _property;
        private readonly object _value;

        public PropertyEqualsSelector(Selector previous, PerspexProperty property, object value)
        {
            Contract.Requires<ArgumentNullException>(property != null);

            _previous = previous;
            _property = property;
            _value = value;
        }

        /// <inheritdoc/>
        public override bool InTemplate => _previous?.InTemplate ?? false;

        /// <summary>
        /// Gets the name of the control to match.
        /// </summary>
        public string Name { get; private set; }

        /// <inheritdoc/>
        public override Type TargetType => _previous?.TargetType;

        /// <inheritdoc/>
        public override string ToString()
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
            builder.Append(_value);
            builder.Append(']');

            return builder.ToString();
        }

        /// <inheritdoc/>
        protected override SelectorMatch Evaluate(IStyleable control, bool subscribe)
        {
            if (!PerspexPropertyRegistry.Instance.IsRegistered(control, _property))
            {
                return SelectorMatch.False;
            }
            else if (subscribe)
            {
                return new SelectorMatch(control.GetObservable(_property).Select(v => Equals(v, _value)));
            }
            else
            {
                return new SelectorMatch(control.GetValue(_property).Equals(_value));
            }
        }

        protected override Selector MovePrevious() => _previous;
    }
}
