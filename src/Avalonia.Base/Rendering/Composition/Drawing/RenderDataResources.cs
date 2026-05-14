using System;
using System.Collections.Generic;
using Avalonia.Collections.Pooled;

namespace Avalonia.Rendering.Composition.Drawing;

internal struct RenderDataResources : IDisposable
{
    public const int NullHandle = -1;

    private PooledList<object?>? _resources;
    private Dictionary<object, int>? _internMap;

    public int Count => _resources?.Count ?? 0;

    public int Intern(object? resource)
    {
        if (resource is null)
            return NullHandle;

        _resources ??= new PooledList<object?>();
        _internMap ??= new Dictionary<object, int>(ReferenceEqualityComparer.Instance);

        if (_internMap.TryGetValue(resource, out var handle))
            return handle;

        handle = _resources.Count;
        _resources.Add(resource);
        _internMap.Add(resource, handle);
        return handle;
    }

    public int Add(object? resource)
    {
        if (resource is null)
            return NullHandle;

        _resources ??= new PooledList<object?>();
        var handle = _resources.Count;
        _resources.Add(resource);
        return handle;
    }

    public object? this[int handle] => handle == NullHandle ? null : _resources![handle];

    public void Dispose()
    {
        _resources?.Dispose();
        _resources = null;
        _internMap = null;
    }
}
