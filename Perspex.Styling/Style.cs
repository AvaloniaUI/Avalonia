// -----------------------------------------------------------------------
// <copyright file="Style.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Linq;

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

            if (activator.CurrentValue || !activator.HasCompleted)
            {
                IObservable<bool> observable = activator;

                // If the activator has completed, then we want its value to be true forever.
                // Because of this we can't pass the activator directly as it will complete 
                // immediately and remove the binding.
                if (activator.HasCompleted)
                {
                    observable = Observable.Never<bool>().StartWith(true);
                }

                foreach (Setter setter in this.Setters)
                {
                    StyleBinding binding = new StyleBinding(observable, setter.Value, description);
                    control.Bind(setter.Property, binding, this.Selector.Priority);
                }
            }
        }

        public override string ToString()
        {
            return "Style: " + this.Selector.ToString();
        }
    }
}
