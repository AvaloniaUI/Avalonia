using System;
using System.Collections.Generic;
using System.Threading;

namespace Avalonia.Rendering.Composition.Drawing.Nodes;

internal interface IRenderDataNodePool
{
    bool Reduce();
}

/// <summary>
/// Manages a single cleanup timer shared by all <see cref="RenderDataNodePool{T}"/> instances.
/// Uses weak references so pools that go out of scope can be garbage collected.
/// </summary>
internal static class RenderDataNodePoolCleanup
{
    private static readonly List<WeakReference<IRenderDataNodePool>> s_pools = new();
    private static readonly Timer s_timer = new(_ => RunCleanup(), null, Timeout.Infinite, Timeout.Infinite);

    public static void Register(IRenderDataNodePool pool)
    {
        lock (s_pools)
        {
            s_pools.Add(new WeakReference<IRenderDataNodePool>(pool));
            s_timer.Change(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }
    }

    public static void StartTimer()
    {
        lock (s_pools)
        {
            s_timer.Change(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }
    }

    private static void RunCleanup()
    {
        lock (s_pools)
        {
            var anyActive = false;
            for (var i = s_pools.Count - 1; i >= 0; i--)
            {
                if (s_pools[i].TryGetTarget(out var pool))
                    anyActive |= pool.Reduce();
                else
                    s_pools.RemoveAt(i);
            }

            if (!anyActive)
                s_timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }
}

/// <summary>
/// Object pool for render data nodes with gradual reclamation during idle periods.
/// While the pool is actively used, items are retained for reuse. Once activity stops,
/// a shared timer gradually releases a third of pooled items per cycle until empty.
/// </summary>
internal sealed class RenderDataNodePool<T> : IRenderDataNodePool where T : class, new()
{
    private T[] _items = Array.Empty<T>();
    private int _count;
    private bool _active;

    public RenderDataNodePool()
    {
        RenderDataNodePoolCleanup.Register(this);
    }

    public T Get()
    {
        lock (_items)
        {
            SetActive();

            if (_count > 0)
                return _items[--_count];
        }

        return new T();
    }

    public void Return(T item)
    {
        lock (_items)
        {
            SetActive();

            if (_count == _items.Length)
                Array.Resize(ref _items, Math.Max(4, _items.Length * 2));

            _items[_count++] = item;
        }
    }

    private void SetActive()
    {
        if (!_active)
        {
            _active = true;
            RenderDataNodePoolCleanup.StartTimer();
        }
    }

    public bool Reduce()
    {
        lock (_items)
        {
            if (_active)
            {
                _active = false;
                return true;
            }

            if (_count == 0)
                return false;

            var release = Math.Max(1, _count / 3);
            var newCount = _count - release;
            Array.Clear(_items, newCount, release);
            _count = newCount;
            return _count > 0;
        }
    }
}
