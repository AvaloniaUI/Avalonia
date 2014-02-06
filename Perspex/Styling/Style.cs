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

        public Style(Selector selector)
            : this()
        {
            this.Selector = selector;
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
            IObservable<bool> activator = this.Selector.GetActivator(control);

            foreach (Setter setter in this.Setters)
            {
                control.SetValue(setter.Property, setter.Value, activator);
            }
        }
    }
}
