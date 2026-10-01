using System;
using System.Collections.Generic;
using Avalonia.Collections.Pooled;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Drawing;

internal struct RenderDataResources : IDisposable
{
    public const int NullHandle = -1;

    private PooledList<object?>? _resources;
    private Dictionary<object, int>? _internMap;

    public int Count => _resources?.Count ?? 0;

    // Recording path: dedupes by reference equality so a resource reused across many draws gets one slot.
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

    // Takes this table's own reference to a shared resource, once per distinct resource, so the
    // resource stays alive for as long as this table does even after its creator releases it.
    public int InternShared<T>(IRef<T> reference) where T : class
    {
        _resources ??= new PooledList<object?>();
        _internMap ??= new Dictionary<object, int>(ReferenceEqualityComparer.Instance);

        var key = reference.Item;
        if (_internMap.TryGetValue(key, out var handle))
            return handle;

        handle = _resources.Count;
        _resources.Add(reference.Clone());
        _internMap.Add(key, handle);
        return handle;
    }

    // Deserialize path: appends without deduping since the wire format is already deduped.
    public int AppendDeserialized(object? resource)
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
