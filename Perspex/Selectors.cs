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
        public static IObservable<bool> OfType<T>(this Control control)
        {
            Contract.Requires<ArgumentNullException>(control != null);
            
            return Observable.Return(control is T);
        }
    }
}
