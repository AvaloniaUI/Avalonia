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
        private bool stopTraversal;

        private Func<IStyleable, IObservable<bool>> observable;

        public Selector()
        {
        }

        public Selector(Selector previous, bool stopTraversal = false)
        {
            this.Previous = previous;
            this.stopTraversal = stopTraversal;
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
            private set;
        }

        public string SelectorString
        {
            get;
            set;
        }

        public Selector MovePrevious()
        {
            return this.stopTraversal ? null : this.Previous;
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

                selector = selector.MovePrevious();
            }

            return new Activator(inputs);
        }

        public override string ToString()
        {
            string result = string.Empty;

            if (this.Previous != null)
            {
                result = this.Previous.ToString();
            }

            return result + this.SelectorString;
        }
    }
}
