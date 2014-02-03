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
        public IStyleable Control
        {
            get;
            set;
        }

        public IObservable<bool> Observable
        {
            get;
            set;
        }

        public Match Previous
        {
            get;
            set;
        }

        public string Token
        {
            get;
            set;
        }

        public IObservable<bool> GetActivator()
        {
            List<IObservable<bool>> observables = new List<IObservable<bool>>();
            Match match = this;

            do
            {
                if (match.Observable != null)
                {
                    observables.Add(match.Observable);
                }

                match = match.Previous;
            }
            while (match != null);

            return System.Reactive.Linq.Observable.CombineLatest(observables).Select(x => x.All(b => b));
        }

        public override string ToString()
        {
            string result = (this.Previous != null) ? this.Previous.ToString() : string.Empty;
            result += this.Token;
            return result;
        }
    }
}
