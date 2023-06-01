using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Drawing;

internal class CompositorRefCountableResource<T> where T : SimpleServerObject
{
    public T Value { get; private set; }
    public int RefCount { get; private set; }

    public CompositorRefCountableResource(T value)
    {
        Value = value;
        RefCount = 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ThrowInvalidOperation() => throw new InvalidOperationException("This resource is disposed");
    
    public void AddRef()
    {
        if (RefCount <= 0)
            ThrowInvalidOperation();
        RefCount++;
    }

    public bool Release(Compositor c)
    {
        if (RefCount <= 0)
            ThrowInvalidOperation();
        RefCount--;
        if (RefCount == 0)
        {
            c.DisposeOnNextBatch(Value);
            return true;
        }

        return false;
    }
}

internal struct CompositorResourceHolder<T> where T : SimpleServerObject
{
    private InlineDictionary<Compositor, CompositorRefCountableResource<T>> _dictionary;

    public bool IsAttached => _dictionary.HasEntries;
    
    public bool CreateOrAddRef(Compositor compositor, ICompositorSerializable owner, out T resource, Func<Compositor, T> factory)
    {
        if (_dictionary.TryGetValue(compositor, out var handle))
        {
            handle.AddRef();
            resource = handle.Value;
            return false;
        }

        resource = factory(compositor);
        _dictionary.Add(compositor, new CompositorRefCountableResource<T>(resource));
        compositor.RegisterForSerialization(owner);
        return true;
    }

    public T? TryGetForCompositor(Compositor compositor)
    {
        if (_dictionary.TryGetValue(compositor, out var handle))
            return handle.Value;
        return default;
    }
    
    public T GetForCompositor(Compositor compositor)
    {
        if (_dictionary.TryGetValue(compositor, out var handle))
            return handle.Value;
        ThrowDoesNotExist();
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining), DoesNotReturn]
    static void ThrowDoesNotExist() => throw new InvalidOperationException("This resource doesn't exist on that compositor");
    
    public bool Release(Compositor compositor)
    {
        if (!_dictionary.TryGetValue(compositor, out var handle))
            ThrowDoesNotExist();
        if (handle.Release(compositor))
        {
            _dictionary.Remove(compositor);
            return true;
        }

        return false;
    }

    public void ProcessPropertyChangeNotification(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.OldValue is ICompositionRenderResource oldResource)
            TransitiveReleaseAll(oldResource);
        if (change.NewValue is ICompositionRenderResource newResource)
            TransitiveAddRefAll(newResource);
    }

    public void TransitiveReleaseAll(ICompositionRenderResource oldResource)
    {
        foreach(var kv in _dictionary)
            oldResource.ReleaseOnCompositor(kv.Key);
    }

    public void TransitiveAddRefAll(ICompositionRenderResource newResource)
    {
        foreach (var kv in _dictionary)
            newResource.AddRefOnCompositor(kv.Key);
    }

    public void RegisterForInvalidationOnAllCompositors(ICompositorSerializable serializable)
    {
        foreach (var kv in _dictionary)
            kv.Key.RegisterForSerialization(serializable);
    }
}