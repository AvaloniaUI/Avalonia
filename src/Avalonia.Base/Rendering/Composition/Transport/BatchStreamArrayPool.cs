using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Platform;
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
    readonly bool _reclaimImmediately;

    public int CurrentUsage => _usage;
    public int CurrentPool => _pool.Count;

    public BatchStreamPoolBase(bool needsFinalize, bool reclaimImmediately, Action<Func<bool>>? startTimer = null)
    {
        if(!needsFinalize)
            GC.SuppressFinalize(needsFinalize);

        var updateRef = new WeakReference<BatchStreamPoolBase<T>>(this);
        if (
            reclaimImmediately 
            || (
            AvaloniaLocator.Current.GetService<IPlatformThreadingInterface>() == null
            && AvaloniaLocator.Current.GetService<IDispatcherImpl>() == null))
            _reclaimImmediately = true;
        else
            StartUpdateTimer(startTimer, updateRef);
    }

    static void StartUpdateTimer(Action<Func<bool>>? startTimer, WeakReference<BatchStreamPoolBase<T>> updateRef)
    {
        Func<bool> timerProc = () =>
        {
            if (updateRef.TryGetTarget(out var target))
            {
                target.UpdateStatistics();
                return true;
            }

            return false;
        };
        if (startTimer != null)
            startTimer(timerProc);
        else
            DispatcherTimer.Run(timerProc, TimeSpan.FromSeconds(1));
    }

    private void UpdateStatistics()
    {
        lock (_pool)
        {
            var maximumUsage = _usageStatistics.Max();
            var recentlyUsedPooledSlots = maximumUsage - _usage;
            var keepSlots = Math.Max(recentlyUsedPooledSlots, 10);
            while (keepSlots < _pool.Count) 
                DestroyItem(_pool.Pop());

            _usageStatisticsSlot = (_usageStatisticsSlot + 1) % _usageStatistics.Length;
            _usageStatistics[_usageStatisticsSlot] = 0;
        }
    }

    protected abstract T CreateItem();

    protected virtual void ClearItem(T item)
    {
        
    }

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
        ClearItem(item);
        lock (_pool)
        {
            _usage--;
            if (!_disposed && !_reclaimImmediately)
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

internal sealed class BatchStreamObjectPool<T> : BatchStreamPoolBase<T[]> where T : class?
{
    public int ArraySize { get; }

    public BatchStreamObjectPool(bool reclaimImmediately = false, int arraySize = 128, Action<Func<bool>>? startTimer = null) 
        : base(false, reclaimImmediately, startTimer)
    {
        ArraySize = arraySize;
    }
    
    protected override T[] CreateItem()
    {
        return new T[ArraySize];
    }

    protected override void ClearItem(T[] item)
    {
        Array.Clear(item, 0, item.Length);
    }
}

internal sealed class BatchStreamMemoryPool : BatchStreamPoolBase<IntPtr>
{
    public int BufferSize { get; }

    public BatchStreamMemoryPool(bool reclaimImmediately, int bufferSize = 1024, Action<Func<bool>>? startTimer = null) 
        : base(true, reclaimImmediately, startTimer)
    {
        BufferSize = bufferSize;
    }
    
    protected override IntPtr CreateItem() => Marshal.AllocHGlobal(BufferSize);

    protected override void DestroyItem(IntPtr item) => Marshal.FreeHGlobal(item);
}
