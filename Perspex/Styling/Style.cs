// -----------------------------------------------------------------------
// <copyright file="Style.cs" company="Tricycle">
// Copyright 2014 Tricycle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using Perspex.Controls;

    public class Style : IStyle
    {
        public Style()
        {
            this.Setters = new List<Setter>();
        }

        public Style(Func<Selector, Selector> selector)
            : this()
        {
            this.Selector = selector(new Selector());
        }

        public Selector Selector
        {
            get;
            set;
        }

        public IEnumerable<Setter> Setters
        {
            get;
            set;
        }

        public void Attach(IStyleable control)
        {
            string description = "Style " + this.Selector.ToString();
            StyleActivator activator = this.Selector.GetActivator(control);

            if (!(activator.CurrentValue == false && activator.HasCompleted))
            {
                foreach (Setter setter in this.Setters)
                {
                    StyleBinding binding = new StyleBinding(activator, setter.Value, description);
                    control.Bind(setter.Property, binding, this.Selector.Priority);
                }
            }
        }
    }
}
