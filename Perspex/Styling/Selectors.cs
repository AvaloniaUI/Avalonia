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
        public static Match Select(this IStyleable control)
        {
            Contract.Requires<ArgumentNullException>(control != null);

            return new Match(control);
        }

        public static Match Class(this Match match, string name)
        {
            Contract.Requires<ArgumentNullException>(match != null);
            Contract.Requires<ArgumentNullException>(name != null);

            return new Match(match)
            {
                Observable = Observable
                    .Return(match.Control.Classes.Contains(name))
                    .Concat(match.Control.Classes.Changed.Select(e => match.Control.Classes.Contains(name))),
                SelectorString = (name[0] == ':') ? name : '.' + name,
            };
        }

        public static Match Id(this Match match, string id)
        {
            Contract.Requires<ArgumentNullException>(match != null);

            return new Match(match)
            {
                Observable = Observable.Return(match.Control.TemplatedParent == null && match.Control.Id == id),
                SelectorString = '#' + id,
            };
        }

        public static Match OfType<T>(this Match match) where T : IStyleable
        {
            Contract.Requires<ArgumentNullException>(match != null);

            return new Match(match)
            {
                Observable = Observable.Return(match.Control is T),
                SelectorString = typeof(T).Name,
            };
        }
    }
}
