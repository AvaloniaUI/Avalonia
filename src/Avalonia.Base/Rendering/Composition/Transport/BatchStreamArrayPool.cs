using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using Avalonia.Threading;

namespace Avalonia.Rendering.Composition.Transport;

/// <summary>
/// A pool that keeps a number of elements that was used in the last 10 seconds 
/// </summary>
internal abstract class BatchStreamPoolBase<T> : IDisposable
{
    readonly Stack<T> _pool = new();
    bool _disposed;
    int _usage;
    readonly int[] _usageStatistics = new int[10];
    int _usageStatisticsSlot;

    public BatchStreamPoolBase(bool needsFinalize = false)
    {
        if(!needsFinalize)
            GC.SuppressFinalize(needsFinalize);

        var updateRef = new WeakReference<BatchStreamPoolBase<T>>(this);
        StartUpdateTimer(updateRef);
    }

    static void StartUpdateTimer(WeakReference<BatchStreamPoolBase<T>> updateRef)
    {
        DispatcherTimer.Run(() =>
        {
            if (updateRef.TryGetTarget(out var target))
            {
                target.UpdateStatistics();
                return true;
            }
            return false;

        }, TimeSpan.FromSeconds(1));
    }

    private void UpdateStatistics()
    {
        lock (_pool)
        {
            var maximumUsage = _usageStatistics.Max();
            var recentlyUsedPooledSlots = maximumUsage - _usage;
            while (recentlyUsedPooledSlots < _pool.Count) 
                DestroyItem(_pool.Pop());

            _usageStatistics[_usage] = 0;
            _usageStatisticsSlot = (_usageStatisticsSlot + 1) % _usageStatistics.Length;
        }
    }

    protected abstract T CreateItem();

    protected virtual void DestroyItem(T item)
    {
        
    }

    public T Get()
    {
        lock (_pool)
        {
            _usage++;
            if (_usageStatistics[_usageStatisticsSlot] < _usage)
                _usageStatistics[_usageStatisticsSlot] = _usage;
            
            if (_pool.Count != 0)
                return _pool.Pop();
        }

        return CreateItem();
    }

    public void Return(T item)
    {
        lock (_pool)
        {
            _usage--;
            if (!_disposed)
            {
                _pool.Push(item);
                return;
            }
        }
        
        DestroyItem(item);
    }

    public void Dispose()
    {
        lock (_pool)
        {
            _disposed = true;
            foreach (var item in _pool)
                DestroyItem(item);
            _pool.Clear();
        }
    }

    ~BatchStreamPoolBase()
    {
        Dispose();
    }
}

internal sealed class BatchStreamObjectPool<T> : BatchStreamPoolBase<T[]> where T : class
{
    private readonly int _arraySize;

    public BatchStreamObjectPool(int arraySize = 1024)
    {
        _arraySize = arraySize;
    }
    
    protected override T[] CreateItem()
    {
        return new T[_arraySize];
    }

    protected override void DestroyItem(T[] item)
    {
        Array.Clear(item, 0, item.Length);
    }
}

internal sealed class BatchStreamMemoryPool : BatchStreamPoolBase<IntPtr>
{
    public int BufferSize { get; }

    public BatchStreamMemoryPool(int bufferSize = 16384)
    {
        BufferSize = bufferSize;
    }
    
    protected override IntPtr CreateItem() => Marshal.AllocHGlobal(BufferSize);

    protected override void DestroyItem(IntPtr item) => Marshal.FreeHGlobal(item);
}