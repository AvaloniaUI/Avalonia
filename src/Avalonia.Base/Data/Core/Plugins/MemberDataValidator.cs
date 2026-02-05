using System;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Data.Core.Plugins;

public abstract class MemberDataValidator(object source)
{
    private readonly WeakReference<object> _source = new(source);
    public event EventHandler? _dataValidationChanged;

    public abstract bool RaisesEvents { get; }

    public abstract Exception? GetDataValidationError();

    public event EventHandler? DataValidationChanged
    {
        add
        {
            if (!RaisesEvents)
                return;
            if (_dataValidationChanged is null && TryGetSource(out var source))
                Subscribe(source);
            _dataValidationChanged += value;
        }

        remove
        {
            if (!RaisesEvents)
                return;
            _dataValidationChanged -= value;
            if (_dataValidationChanged is null && TryGetSource(out var source))
                Unsubscribe(source);
        }
    }

    protected abstract void Subscribe(object source);
    protected abstract void Unsubscribe(object source);

    protected void RaiseDataValidationChanged()
    {
        _dataValidationChanged?.Invoke(this, EventArgs.Empty);
    }

    protected bool TryGetSource([NotNullWhen(true)] out object? source)
    {
        return _source.TryGetTarget(out source);
    }

    protected bool TryGetSource<T>([NotNullWhen(true)] out T? source) where T : class
    {
        if (_source.TryGetTarget(out var obj) && obj is T t)
        {
            source = t;
            return true;
        }
        source = null;
        return false;
    }
}
