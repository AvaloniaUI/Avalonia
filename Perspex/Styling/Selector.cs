// -----------------------------------------------------------------------
// <copyright file="Match.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Text;
    using Perspex.Controls;

    public class Selector
    {
        private Func<IStyleable, IObservable<bool>> observable;

        public Selector()
        {
        }

        public Selector(Selector previous)
        {
            this.Previous = previous;
        }

        public Func<IStyleable, IObservable<bool>> Observable
        {
            get
            {
                return this.observable;
            }
            
            set
            {
                this.observable = value;
            }
        }

        public Selector Previous
        {
            get;
            set;
        }

        public string SelectorString
        {
            get;
            set;
        }

        public Activator GetActivator(IStyleable control)
        {
            List<IObservable<bool>> inputs = new List<IObservable<bool>>();
            Selector selector = this;
            
            while (selector != null)
            {
                if (selector.Observable != null)
                {
                    inputs.Add(selector.Observable(control));
                }

                selector = selector.Previous;
            }

            return new Activator(inputs);
        }

        public override string ToString()
        {
            Selector match = this;
            StringBuilder b = new StringBuilder();

            while (match != null)
            {
                b.Append(match.SelectorString);
                match = match.Previous;
            }

            return b.ToString();
        }
    }
}
