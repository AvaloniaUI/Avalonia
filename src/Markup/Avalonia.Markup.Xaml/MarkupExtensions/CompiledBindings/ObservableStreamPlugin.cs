using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings
{
    class ObservableStreamPlugin<T> : IStreamPlugin
    {
        public bool Match(WeakReference reference)
        {
            return reference is IObservable<T>;
        }

        public IObservable<object> Start(WeakReference reference)
        {
            var target = reference.Target as IObservable<T>;

            if (target is IObservable<object> obj)
            {
                return obj;
            }

            return target.Select(x => (object)x);
        }
    }
}
