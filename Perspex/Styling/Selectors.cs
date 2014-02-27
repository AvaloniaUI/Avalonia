// -----------------------------------------------------------------------
// <copyright file="Selectors.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Linq;
    using Perspex.Controls;

    public static class Selectors
    {
        public static Selector Class(this Selector previous, string name)
        {
            Contract.Requires<ArgumentNullException>(previous != null);
            Contract.Requires<ArgumentNullException>(name != null);

            return new Selector(previous)
            {
                Observable = control => Observable
                    .Return(control.Classes.Contains(name))
                    .Concat(control.Classes.Changed.Select(e => control.Classes.Contains(name))),
                SelectorString = (name[0] == ':') ? name : '.' + name,
            };
        }

        public static Selector Descendent(this Selector previous)
        {
            return new Selector(previous, stopTraversal: true)
            {
                SelectorString = " ",
                Observable = control =>
                {
                    ILogical c = (ILogical)control;
                    List<IObservable<bool>> descendentMatches = new List<IObservable<bool>>();

                    while (c != null)
                    {
                        c = c.LogicalParent;

                        if (c is IStyleable)
                        {
                            descendentMatches.Add(previous.Observable((IStyleable)c));
                        }
                    }

                    return new Activator(descendentMatches, ActivatorMode.Or);
                },
            };
        }

        public static Selector Id(this Selector previous, string id)
        {
            Contract.Requires<ArgumentNullException>(previous != null);

            return new Selector(previous)
            {
                Observable = control => Observable.Return(control.Id == id),
                SelectorString = '#' + id,
            };
        }

        public static Selector OfType<T>(this Selector previous) where T : IStyleable
        {
            Contract.Requires<ArgumentNullException>(previous != null);

            return new Selector(previous)
            {
                Observable = control => Observable.Return(control is T),
                SelectorString = typeof(T).Name,
            };
        }

        public static Selector Template(this Selector previous)
        {
            Contract.Requires<ArgumentNullException>(previous != null);

            return new Selector(previous)
            {
                Observable = control => Observable.Return(control.TemplatedParent != null),
                SelectorString = " $ ",
            };
        }
    }
}
