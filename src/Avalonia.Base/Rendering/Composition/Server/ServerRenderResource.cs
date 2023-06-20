using System;
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

internal class SimpleServerRenderResource : SimpleServerObject, IServerRenderResource, IDisposable
{
    private bool _pendingInvalidation;
    private bool _disposed;
    public bool IsDisposed => _disposed;
    private RefCountingSmallDictionary<IServerRenderResourceObserver> _observers;
    
    public SimpleServerRenderResource(ServerCompositor compositor) : base(compositor)
    {
    }

    protected new void SetValue<T>(CompositionProperty prop, ref T field, T value) => SetValue(ref field, value);
    
    protected void SetValue<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        
        if (_disposed)
        {
            field = value;
            return;
        }

        if (field is IServerRenderResource oldChild)
            oldChild.RemoveObserver(this);
        else if (field is IServerRenderResource[] oldChildren)
        {
            foreach (var ch in oldChildren)
                ch?.RemoveObserver(this);
        }
        field = value;
        if (field is IServerRenderResource newChild)
            newChild.AddObserver(this);
        else if (field is IServerRenderResource[] newChildren)
        {
            foreach (var ch in newChildren)
                ch.AddObserver(this);
        }
        Invalidated();
    }

    protected void Invalidated()
    {
        // This is needed to avoid triggering on multiple property changes
        if (!_pendingInvalidation)
        {
            _pendingInvalidation = true;
            Compositor.EnqueueRenderResourceForInvalidation(this);
            PropertyChanged();
        }
    }

    protected override void ValuesInvalidated()
    {
        Invalidated();
        base.ValuesInvalidated();
    }

    protected void RemoveObserversFromProperty<T>(ref T field)
    {
        (field as IServerRenderResource)?.RemoveObserver(this);
    }

    public virtual void Dispose()
    {
        _disposed = true;
        // TODO: dispose once we implement pooling
        _observers = default;
    }

    public virtual void DependencyQueuedInvalidate(IServerRenderResource sender) =>
        Compositor.EnqueueRenderResourceForInvalidation(this);

    protected virtual void PropertyChanged()
    {
        
    }
    
    public void AddObserver(IServerRenderResourceObserver observer)
    {
        Debug.Assert(!_disposed);
        if(_disposed)
            return;
        _observers.Add(observer);
    }

    public void RemoveObserver(IServerRenderResourceObserver observer)
    {
        if (_disposed)
            return;
        _observers.Remove(observer);
    }

    public virtual void QueuedInvalidate()
    {
        _pendingInvalidation = false;

        foreach (var observer in _observers)
            observer.Key.DependencyQueuedInvalidate(this);

    }
}