using System;
using System.Threading;

namespace Avalonia.Platform;

public interface IScopedResource<T> : IDisposable
{
    public T Value { get; }
}

public class ScopedResource<T> : IScopedResource<T>
{
    private int _disposed = 0;
    private T _value;
    private Action? _dispose;
    private ScopedResource(T value, Action dispose)
    {
        _value = value;
        _dispose = dispose;
    }
    
    public static IScopedResource<T> Create(T value, Action dispose) => new ScopedResource<T>(value, dispose);

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
        {
            var disp = _dispose!;
            _value = default!;
            _dispose = null;
            disp();
        }
    }

    public T Value
    {
        get
        {
            if (_disposed == 1)
                throw new ObjectDisposedException(this.GetType().FullName);
            return _value;
        }
    }
}