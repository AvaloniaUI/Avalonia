using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server;

internal interface IServerRenderResourceObserver
{
    void DependencyQueuedInvalidate(IServerRenderResource sender);
}

internal interface IServerRenderResource : IServerRenderResourceObserver
{
    void AddObserver(IServerRenderResourceObserver observer);
    void RemoveObserver(IServerRenderResourceObserver observer);
    void QueuedInvalidate();
}

internal interface IServerRenderResourceHost : IServerRenderResource
{
    ServerCompositor Compositor { get; }
    void ResourcePropertyChanged();
}

internal class SimpleServerRenderResource : SimpleServerObject, IServerRenderResourceHost, IDisposable
{
    private ServerRenderResourceCore _core;
    public bool IsDisposed => _core.IsDisposed;

    public SimpleServerRenderResource(ServerCompositor compositor) : base(compositor) { }

    protected new void SetValue<T>(CompositionProperty prop, ref T field, T value) => SetValue(ref field, value);

    protected void SetValue<T>(ref T field, T value) => _core.SetValue(this, ref field, value);

    protected void Invalidated() => _core.Invalidated(this);

    protected override void ValuesInvalidated()
    {
        Invalidated();
        base.ValuesInvalidated();
    }

    protected void RemoveObserversFromProperty<T>(ref T field) => _core.RemoveObserversFromProperty(this, ref field);

    public virtual void Dispose() => _core.Dispose();

    public virtual void DependencyQueuedInvalidate(IServerRenderResource sender) => _core.DependencyQueuedInvalidate(this);

    protected virtual void PropertyChanged()
    {
    }

    public void AddObserver(IServerRenderResourceObserver observer) => _core.AddObserver(observer);

    public void RemoveObserver(IServerRenderResourceObserver observer) => _core.RemoveObserver(observer);

    public virtual void QueuedInvalidate() => _core.QueuedInvalidate(this);
    void IServerRenderResourceHost.ResourcePropertyChanged() => PropertyChanged();
}

internal class ServerRenderResource : ServerObject, IServerRenderResourceHost, IDisposable
{
    private ServerRenderResourceCore _core;
    public bool IsDisposed => _core.IsDisposed;

    public ServerRenderResource(ServerCompositor compositor) : base(compositor) { }

    protected new void SetValue<T>(CompositionProperty prop, ref T field, T value) => SetValue(ref field, value);

    protected void SetValue<T>(ref T field, T value) => _core.SetValue(this, ref field, value);

    protected void Invalidated() => _core.Invalidated(this);

    protected override void ValuesInvalidated()
    {
        Invalidated();
        base.ValuesInvalidated();
    }

    protected void RemoveObserversFromProperty<T>(ref T field) => _core.RemoveObserversFromProperty(this, ref field);

    public virtual void Dispose() => _core.Dispose();

    public virtual void DependencyQueuedInvalidate(IServerRenderResource sender) => _core.DependencyQueuedInvalidate(this);

    protected virtual void PropertyChanged()
    {
    }

    public void AddObserver(IServerRenderResourceObserver observer) => _core.AddObserver(observer);

    public void RemoveObserver(IServerRenderResourceObserver observer) => _core.RemoveObserver(observer);

    public virtual void QueuedInvalidate() => _core.QueuedInvalidate(this);

    void IServerRenderResourceHost.ResourcePropertyChanged() => PropertyChanged();
}

internal struct ServerRenderResourceCore
{
    private bool _pendingInvalidation;
    private bool _disposed;
    public bool IsDisposed => _disposed;
    private RefCountingSmallDictionary<IServerRenderResourceObserver> _observers;

    public void SetValue<T>(IServerRenderResourceHost self, ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        if (_disposed)
        {
            field = value;
            return;
        }

        if (field is IServerRenderResource oldChild)
            oldChild.RemoveObserver(self);
        else if (field is IEnumerable oldChildren)
        {
            foreach (var ch in oldChildren)
                (ch as IServerRenderResource)?.RemoveObserver(self);
        }

        field = value;

        if (field is IServerRenderResource newChild)
            newChild.AddObserver(self);
        else if (field is IEnumerable newChildren)
        {
            foreach (var ch in newChildren)
                (ch as IServerRenderResource)?.AddObserver(self);
        }

        Invalidated(self);
    }

    public void Invalidated(IServerRenderResourceHost self)
    {
        // This is needed to avoid triggering on multiple property changes
        if (!_pendingInvalidation)
        {
            _pendingInvalidation = true;
            self.Compositor.EnqueueRenderResourceForInvalidation(self);
            self.ResourcePropertyChanged();
        }
    }

    public void RemoveObserversFromProperty<T>(IServerRenderResource self, ref T field)
    {
        (field as IServerRenderResource)?.RemoveObserver(self);
    }

    public void Dispose()
    {
        _disposed = true;
        // TODO: dispose once we implement pooling
        _observers = default;
    }

    public void DependencyQueuedInvalidate(IServerRenderResourceHost self)
    {
        self.Compositor.EnqueueRenderResourceForInvalidation(self);
    }

    public void AddObserver(IServerRenderResourceObserver observer)
    {
        Debug.Assert(!_disposed);
        if (_disposed)
            return;
        _observers.Add(observer);
    }

    public void RemoveObserver(IServerRenderResourceObserver observer)
    {
        if (_disposed)
            return;
        _observers.Remove(observer);
    }

    public void QueuedInvalidate(IServerRenderResource self)
    {
        _pendingInvalidation = false;

        foreach (var observer in _observers)
            observer.Key.DependencyQueuedInvalidate(self);
    }
}
