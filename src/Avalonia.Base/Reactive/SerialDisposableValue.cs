using System;
using System.Threading;

namespace Avalonia.Reactive;

/// <summary>
/// Represents a disposable resource whose underlying disposable resource can be replaced by another disposable resource, causing automatic disposal of the previous underlying disposable resource.
/// </summary>
internal sealed class SerialDisposableValue : IDisposable
{
    private IDisposable? _current;
    private bool _disposed;
    
    public IDisposable? Disposable
    {
        get => _current;
        set
        {
            _current?.Dispose();
            _current = value;
            
            if (_disposed)
            {
                _current?.Dispose();
                _current = null;
            }
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _current?.Dispose();
    }
}
