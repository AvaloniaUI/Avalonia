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
            var description = "Style " + this.Selector.ToString();
            var match = this.Selector.Match(control);

            if (match.ImmediateResult.HasValue)
            {
                if (match.ImmediateResult == true)
                {
                    foreach (Setter setter in this.Setters)
                    {
                        if (setter.Source != null && setter.Value != null)
                        {
                            throw new InvalidOperationException("Cannot set both Source and Value on a Setter.");
                        }

                        if (setter.Source == null)
                        {
                            control.SetValue(setter.Property, setter.Value, BindingPriority.Style);
                        }
                        else
                        {
                            control.Bind(setter.Property, setter.Source, BindingPriority.Style);
                        }
                    }
                }
            }
            else
            {
                foreach (Setter setter in this.Setters)
                {
                    if (setter.Source != null && setter.Value != null)
                    {
                        throw new InvalidOperationException("Cannot set both Source and Value on a Setter.");
                    }

                    StyleBinding binding;

                    if (setter.Source == null)
                    {
                        binding = new StyleBinding(match.ObservableResult, setter.Value, description);
                    }
                    else
                    {
                        binding = new StyleBinding(match.ObservableResult, setter.Source, description);
                    }

                    control.Bind(setter.Property, binding, BindingPriority.StyleTrigger);
                }
            }
        }

        public override string ToString()
        {
            return "Style: " + this.Selector.ToString();
        }
    }
}
