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
    using Perspex.Controls;

    public class Match
    {
        public Match(IStyleable control)
        {
            this.Control = control;
            this.Observables = new List<IObservable<bool>>();
        }

        public Match(Match source)
        {
            this.Control = source.Control;
            this.InTemplate = source.InTemplate;
            this.Observables = source.Observables;
            this.SelectorString = SelectorString;
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

        public List<IObservable<bool>> Observables
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
            return new Activator(this.Observables);
        }

        public override string ToString()
        {
            return this.SelectorString;
        }
    }
}
