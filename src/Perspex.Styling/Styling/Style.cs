// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Perspex.Metadata;

namespace Perspex.Styling
{
    /// <summary>
    /// Defines a style.
    /// </summary>
    public class Style : IStyle
    {
        private static readonly IObservable<bool> True = Observable.Never<bool>().StartWith(true);
        private Dictionary<string, object> _resources;

        /// <summary>
        /// Initializes a new instance of the <see cref="Style"/> class.
        /// </summary>
        public Style()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Style"/> class.
        /// </summary>
        /// <param name="selector">The style selector.</param>
        public Style(Func<Selector, Selector> selector)
        {
            Selector = selector(null);
        }

        /// <summary>
        /// Gets or sets a dictionary of style resources.
        /// </summary>
        public IDictionary<string, object> Resources
        {
            get
            {
                if (_resources == null)
                {
                    _resources = new Dictionary<string, object>();
                }

                return _resources;
            }

            set
            {
                var resources = Resources;

                foreach (var i in value)
                {
                    resources.Add(i);
                }
            }
        }

        /// <summary>
        /// Gets or sets style's selector.
        /// </summary>
        public Selector Selector { get; set; }

        /// <summary>
        /// Gets or sets style's setters.
        /// </summary>
        [Content]
        public IEnumerable<ISetter> Setters { get; set; } = new List<ISetter>();

        /// <summary>
        /// Attaches the style to a control if the style's selector matches.
        /// </summary>
        /// <param name="control">The control to attach to.</param>
        /// <param name="container">
        /// The control that contains this style. May be null.
        /// </param>
        public void Attach(IStyleable control, IStyleHost container)
        {
            if (Selector != null)
            {
                var description = "Style " + Selector.ToString();
                var match = Selector.Match(control);

                if (match.ImmediateResult != false)
                {
                    var activator = (match.ObservableResult ?? True)
                        .TakeUntil(control.StyleDetach);

                    foreach (var setter in Setters)
                    {
                        setter.Apply(this, control, activator);
                    }
                }
            }
            else if (control == container)
            {
                foreach (var setter in Setters)
                {
                    setter.Apply(this, control, null);
                }
            }
        }

        /// <summary>
        /// Tries to find a named resource within the style.
        /// </summary>
        /// <param name="name">The resource name.</param>
        /// <returns>
        /// The resource if found, otherwise <see cref="PerspexProperty.UnsetValue"/>.
        /// </returns>
        public object FindResource(string name)
        {
            object result = null;

            if (_resources?.TryGetValue(name, out result) == true)
            {
                return result;
            }
            else
            {
                return PerspexProperty.UnsetValue;
            }
        }

        /// <summary>
        /// Returns a string representation of the style.
        /// </summary>
        /// <returns>A string representation of the style.</returns>
        public override string ToString()
        {
            if (Selector != null)
            {
                return "Style: " + Selector.ToString();
            }
            else
            {
                return "Style";
            }
        }
    }
}
