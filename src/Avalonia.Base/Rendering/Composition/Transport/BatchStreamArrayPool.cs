using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Rendering.Composition.Transport;

/// <summary>
/// A pool that keeps a number of elements that was used in the last 10 seconds 
/// </summary>
internal abstract class BatchStreamPoolBase<T> : IDisposable
{
    private readonly Action<Func<bool>>? _startTimer;
    readonly Stack<T> _pool = new();
    bool _disposed;
    int _usage;
    readonly int[] _usageStatistics = new int[10];
    int _usageStatisticsSlot;
    private readonly WeakReference<BatchStreamPoolBase<T>> _updateRef;
    private readonly Dispatcher? _reclaimOnDispatcher;
    private bool _timerIsRunning;
    private ulong _currentUpdateTick, _lastActivityTick;

    public int CurrentUsage => _usage;
    public int CurrentPool => _pool.Count;

    public BatchStreamPoolBase(bool needsFinalize, bool reclaimImmediately, Action<Func<bool>>? startTimer = null)
    {
        _startTimer = startTimer;
        if(!needsFinalize)
            GC.SuppressFinalize(this);

        _updateRef = new WeakReference<BatchStreamPoolBase<T>>(this);
        _reclaimOnDispatcher = !reclaimImmediately ? Dispatcher.FromThread(Thread.CurrentThread) : null;
        EnsureUpdateTimer();
    }
    

    void EnsureUpdateTimer()
    {
        if (_timerIsRunning || !NeedsTimer)
            return;

        var timerProc = GetTimerProc(_updateRef);
        
        if (_startTimer != null)
            _startTimer(timerProc);
        else
        {
            if (_reclaimOnDispatcher != null)
            {
                if (_reclaimOnDispatcher.CheckAccess())
                    DispatcherTimer.Run(timerProc, TimeSpan.FromSeconds(1));
                else
                    _reclaimOnDispatcher.Invoke(() => DispatcherTimer.Run(timerProc, TimeSpan.FromSeconds(1)));
            }
        }

        _timerIsRunning = true;
        // Explicit capture
        static Func<bool> GetTimerProc(WeakReference<BatchStreamPoolBase<T>> updateRef) => () =>
        {
            if (updateRef.TryGetTarget(out var target))
                return target.UpdateTimerTick();

            return false;
        };
    }

    [MemberNotNullWhen(true, nameof(_reclaimOnDispatcher))]
    private bool NeedsTimer => _reclaimOnDispatcher != null &&
                               _currentUpdateTick - _lastActivityTick < (uint)_usageStatistics.Length * 2 + 1;
    private bool ReclaimImmediately => _reclaimOnDispatcher == null;

    private bool UpdateTimerTick()
    {
        lock (_pool)
        {
            _currentUpdateTick++;
            var maximumUsage = _usageStatistics.Max();
            var recentlyUsedPooledSlots = maximumUsage - _usage;
            var keepSlots = Math.Max(recentlyUsedPooledSlots, 10);
            while (keepSlots < _pool.Count) 
                DestroyItem(_pool.Pop());

            _usageStatisticsSlot = (_usageStatisticsSlot + 1) % _usageStatistics.Length;
            _usageStatistics[_usageStatisticsSlot] = 0;

            return _timerIsRunning = NeedsTimer;
        }
    }

    private void OnActivity()
    {
        _lastActivityTick = _currentUpdateTick;
        EnsureUpdateTimer();
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
            
            OnActivity();
            
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
            if (!_disposed && !ReclaimImmediately)
            {
                _pool.Push(item);
                OnActivity();
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
