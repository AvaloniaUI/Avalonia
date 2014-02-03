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
        public static Match Select<T>(this IStyleable control)
        {
            Contract.Requires<ArgumentNullException>(control != null);

            if (control is T)
            {
                return new Match
                {
                    Control = control,
                    Observable = Observable.Return(true),
                    Token = typeof(T).Name,
                };
            }
            else
            {
                return null;
            }
        }

        public static Match Class(this Match selector, string name)
        {
            Contract.Requires<ArgumentNullException>(name != null);

            if (selector != null)
            {
                IObservable<bool> match = Observable
                    .Return(selector.Control.Classes.Contains(name))
                    .Concat(selector.Control.Classes.Changed.Select(e => selector.Control.Classes.Contains(name)));

                return new Match
                {
                    Control = selector.Control,
                    Observable = match,
                    Previous = selector,
                    Token = (name[0] == ':') ? name : '.' + name,
                };
            }
            else
            {
                return null;
            }
        }
    }
}
