// -----------------------------------------------------------------------
// <copyright file="Selectors.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Text;
    using System.Threading.Tasks;
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

            match.Observables.Add(Observable
                .Return(match.Control.Classes.Contains(name))
                .Concat(match.Control.Classes.Changed.Select(e => match.Control.Classes.Contains(name))));
            match.SelectorString += (name[0] == ':') ? name : '.' + name;

            return match;
        }

        public static Match Id(this Match match, string id)
        {
            Contract.Requires<ArgumentNullException>(match != null);

            if (!match.InTemplate)
            {
                match.Observables.Add(Observable.Return(
                    match.Control.TemplatedParent == null &&
                    match.Control.Id == id));
            }
            else
            {
                match.Observables.Add(Observable.Return(
                    match.Control.TemplatedParent != null &&
                    match.Control.Id == id));
            }

            match.SelectorString += '#' + id;
            return match;
        }

        public static Match InTemplateOf<T>(this Match match)
        {
            Contract.Requires<ArgumentNullException>(match != null);

            match.Observables.Add(Observable.Return(match.Control.TemplatedParent is T));
            match.InTemplate = true;
            match.SelectorString += '%' + typeof(T).Name;
            return match;
        }

        public static Match OfType<T>(this Match match)
        {
            Contract.Requires<ArgumentNullException>(match != null);

            match.Observables.Add(Observable.Return(match.Control is T));
            match.SelectorString += typeof(T).Name;
            return match;
        }
    }
}
