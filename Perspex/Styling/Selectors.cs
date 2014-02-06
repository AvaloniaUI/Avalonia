// -----------------------------------------------------------------------
// <copyright file="Selectors.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling
{
    using System;
    using System.Reactive.Linq;
    using Perspex.Controls;

    public static class Selectors
    {
        public static Selector Class(this Selector match, string name)
        {
            Contract.Requires<ArgumentNullException>(match != null);
            Contract.Requires<ArgumentNullException>(name != null);

            return new Selector(match)
            {
                Observable = control => Observable
                    .Return(control.Classes.Contains(name))
                    .Concat(control.Classes.Changed.Select(e => control.Classes.Contains(name))),
                SelectorString = (name[0] == ':') ? name : '.' + name,
            };
        }

        public static Selector Descendent(this Selector match)
        {
            throw new NotImplementedException();
        }

        public static Selector Id(this Selector match, string id)
        {
            Contract.Requires<ArgumentNullException>(match != null);

            return new Selector(match)
            {
                Observable = control => Observable.Return(control.Id == id),
                SelectorString = '#' + id,
            };
        }

        public static Selector OfType<T>(this Selector match) where T : IStyleable
        {
            Contract.Requires<ArgumentNullException>(match != null);

            return new Selector(match)
            {
                Observable = control => Observable.Return(control is T),
                SelectorString = typeof(T).Name,
            };
        }

        public static Selector Template(this Selector match)
        {
            Contract.Requires<ArgumentNullException>(match != null);

            return new Selector(match)
            {
                SelectorString = " $ ",
            };
        }
    }
}
