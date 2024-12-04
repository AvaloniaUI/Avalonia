using System;
using System.Linq;
using Avalonia.Reactive;

namespace Avalonia.Data.Core.Plugins;

internal class ObservableStreamPlugin<T> : IStreamPlugin
{
    public bool Match(WeakReference<object?> reference)
    {
        return reference.TryGetTarget(out var target) && target is IObservable<T>;
    }

    public IObservable<object?> Start(WeakReference<object?> reference)
    {
        if (!(reference.TryGetTarget(out var target) && target is IObservable<T> obs))
        {
            return Observable.Empty<object?>();
        }
        else if (target is IObservable<object?> obj)
        {
            return obj;
        }

        return obs.Select(x => (object?)x);
    }
}
