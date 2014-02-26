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

    public class Match
    {
        private IObservable<bool> observable;

        public Match(IStyleable control)
        {
            this.Control = control;
        }

        public Match(Match previous)
        {
            this.Control = previous.Control;
            this.InTemplate = previous.InTemplate;
            this.Previous = previous;
        }

        public IStyleable Control
        {
            get;
            private set;
        }

        public bool InTemplate
        {
            get;
            set;
        }

        public IObservable<bool> Observable
        {
            get
            {
                return this.observable;
            }
            
            set
            {
                if ((!InTemplate && Control.TemplatedParent == null) ||
                    (InTemplate && Control.TemplatedParent != null))
                {
                    this.observable = value;
                }
                else
                {
                    this.observable = System.Reactive.Linq.Observable.Return(false);
                }
            }
        }

        public Match Previous
        {
            get;
            set;
        }

        public string SelectorString
        {
            get;
            set;
        }

        public Activator GetActivator()
        {
            List<IObservable<bool>> inputs = new List<IObservable<bool>>();
            Match match = this;
            
            while (match != null)
            {
                if (match.Observable != null)
                {
                    inputs.Add(match.Observable);
                }

                match = match.Previous;
            }

            return new Activator(inputs);
        }

        public override string ToString()
        {
            Match match = this;
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
