namespace Perspex
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Perspex.Controls;

    public static class Selectors
    {
        public static Match Select<T>(this Control control)
        {
            Contract.Requires<ArgumentNullException>(control != null);

            if (control is T)
            {
                return new Match
                {
                    Control = control,
                };
            }
            else
            {
                return null;
            }
        }

        public static Match PropertyEquals<T>(this Match selector, PerspexProperty<T> property, T value)
        {
            Contract.Requires<ArgumentNullException>(property != null);

            if (selector != null)
            {
                IObservable<bool> match = selector.Control.GetObservable(property).Select(x => object.Equals(x, value));

                return new Match
                {
                    Control = selector.Control,
                    Observable = match,
                    Previous = selector,
                };
            }
            else
            {
                return null;
            }
        }
    }
}
