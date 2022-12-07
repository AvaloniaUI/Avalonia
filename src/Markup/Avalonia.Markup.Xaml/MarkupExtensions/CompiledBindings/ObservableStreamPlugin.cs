using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Text;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings
{
    class ObservableStreamPlugin<T> : IStreamPlugin
    {
        [RequiresUnreferencedCode(TrimmingMessages.StreamPluginRequiresUnreferencedCodeMessage)]
        public bool Match(WeakReference<object> reference)
        {
            return reference.TryGetTarget(out var target) && target is IObservable<T>;
        }

        [RequiresUnreferencedCode(TrimmingMessages.StreamPluginRequiresUnreferencedCodeMessage)]
        public IObservable<object> Start(WeakReference<object> reference)
        {
            if (!(reference.TryGetTarget(out var target) && target is IObservable<T> obs))
            {
                return Observable.Empty<object>();
            }
            else if (target is IObservable<object> obj)
            {
                return obj;
            }

            return obs.Select(x => (object)x);
        }
    }
}
