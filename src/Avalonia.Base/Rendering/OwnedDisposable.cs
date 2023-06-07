using System;

namespace Avalonia.Rendering;

struct OwnedDisposable<T> :IDisposable where T : class, IDisposable
{
    private readonly bool _owns;
    private T? _value;

    public T Value => _value ?? throw new ObjectDisposedException("OwnedDisposable");

    public OwnedDisposable(T value, bool owns)
    {
        _owns = owns;
        _value = value;
    }

    public void Dispose()
    {
        if(_owns)
            _value?.Dispose();
        _value = null;
    }
}