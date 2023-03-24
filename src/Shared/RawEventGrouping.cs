#nullable enable
using System;
using System.Collections.Generic;
using Avalonia.Collections.Pooled;
using Avalonia.Controls.Platform;
using Avalonia.Input.Raw;
using Avalonia.Threading;

namespace Avalonia;

/*
  This helper maintains an input queue for backends that handle input asynchronously.
  While doing that it groups Move and TouchUpdate events so we could provide GetIntermediatePoints API
 */

internal interface IRawEventGrouperDispatchQueue
{
    void Add(RawInputEventArgs args, Action<RawInputEventArgs> handler);
}


class ManualRawEventGrouperDispatchQueue : IRawEventGrouperDispatchQueue
{
    private readonly Queue<(RawInputEventArgs args, Action<RawInputEventArgs> handler)> _inputQueue = new();
    public void Add(RawInputEventArgs args, Action<RawInputEventArgs> handler) => _inputQueue.Enqueue((args, handler));

    public bool HasJobs => _inputQueue.Count > 0;
    
    public void DispatchNext()
    {
        if (_inputQueue.Count == 0)
            return;
        var ev = _inputQueue.Dequeue();
        ev.handler(ev.args);
    }
}

internal class ManualRawEventGrouperDispatchQueueDispatcherInputProvider : ManagedDispatcherImpl.IManagedDispatcherInputProvider
{
    private readonly ManualRawEventGrouperDispatchQueue _queue;

    public ManualRawEventGrouperDispatchQueueDispatcherInputProvider(ManualRawEventGrouperDispatchQueue queue)
    {
        _queue = queue;
    }

    public bool HasInput => _queue.HasJobs;
    public void DispatchNextInputEvent() => _queue.DispatchNext();
}

internal class AutomaticRawEventGrouperDispatchQueue : IRawEventGrouperDispatchQueue
{
    private readonly Queue<(RawInputEventArgs args, Action<RawInputEventArgs> handler)> _inputQueue = new();
    private readonly Action _dispatchFromQueue;

    public AutomaticRawEventGrouperDispatchQueue()
    {
        _dispatchFromQueue = DispatchFromQueue;
    }
    
    public void Add(RawInputEventArgs args, Action<RawInputEventArgs> handler)
    {
        _inputQueue.Enqueue((args, handler));
        
        if (_inputQueue.Count == 1)
            Dispatcher.UIThread.Post(_dispatchFromQueue, DispatcherPriority.Input);
        
    }
    
    private void DispatchFromQueue()
    {
        while (true)
        {
            if(_inputQueue.Count == 0)
                return;

            var ev = _inputQueue.Dequeue();

            ev.handler(ev.args);
            
            if (Dispatcher.UIThread.HasJobsWithPriority(DispatcherPriority.Input + 1))
            {
                Dispatcher.UIThread.Post(_dispatchFromQueue, DispatcherPriority.Input);
                return;
            }
        }
    }
}

internal class RawEventGrouper : IDisposable
{
    private readonly Action<RawInputEventArgs> _eventCallback;
    private readonly IRawEventGrouperDispatchQueue _queue;
    private readonly Dictionary<long, RawPointerEventArgs> _lastTouchPoints = new();
    private RawInputEventArgs? _lastEvent;
    private Action<RawInputEventArgs> _dispatch;
    private bool _disposed;

    public RawEventGrouper(Action<RawInputEventArgs> eventCallback, IRawEventGrouperDispatchQueue? queue = null)
    {
        _eventCallback = eventCallback;
        _queue = queue ?? new AutomaticRawEventGrouperDispatchQueue();
        _dispatch = Dispatch;
    }
    
    private void AddToQueue(RawInputEventArgs args)
    {
        _lastEvent = args;
        _queue.Add(args, _dispatch);
    }

    private void Dispatch(RawInputEventArgs ev)
    {
        if (!_disposed)
        {
            if (_lastEvent == ev)
                _lastEvent = null;

            if (ev is RawTouchEventArgs { Type: RawPointerEventType.TouchUpdate } touchUpdate)
                _lastTouchPoints.Remove(touchUpdate.RawPointerId);
            
            _eventCallback?.Invoke(ev);
        }

        if (ev is RawPointerEventArgs { IntermediatePoints.Value: PooledList<RawPointerPoint> list }) 
            list.Dispose();
    }

    
    public void HandleEvent(RawInputEventArgs args)
    {
        /*
         Try to update already enqueued events if
         1) they are still not handled (_lastEvent and _lastTouchPoints shouldn't contain said event in that case)
         2) previous event belongs to the same "event block", events in the same block:
           - belong from the same device
           - are pointer move events (Move/TouchUpdate)
           - have the same type
           - have same modifiers
           
         Even if nothing is updated and the event is actually enqueued, we need to update the relevant tracking info  
        */
        if (
            args is RawPointerEventArgs pointerEvent
            && _lastEvent != null 
            && _lastEvent.Device == args.Device 
            && _lastEvent is RawPointerEventArgs lastPointerEvent
            && lastPointerEvent.InputModifiers == pointerEvent.InputModifiers
            && lastPointerEvent.Type == pointerEvent.Type 
            && lastPointerEvent.Type is RawPointerEventType.Move or RawPointerEventType.TouchUpdate)
        {
            if (args is RawTouchEventArgs touchEvent)
            {
                if (_lastTouchPoints.TryGetValue(touchEvent.RawPointerId, out var lastTouchEvent))
                    MergeEvents(lastTouchEvent, touchEvent);
                else
                {
                    _lastTouchPoints[touchEvent.RawPointerId] = touchEvent;
                    AddToQueue(touchEvent);
                }
            }
            else
                MergeEvents(lastPointerEvent, pointerEvent);

            return;
        }
        else
        {
            _lastTouchPoints.Clear();
            if (args is RawTouchEventArgs { Type: RawPointerEventType.TouchUpdate } touchEvent)
                _lastTouchPoints[touchEvent.RawPointerId] = touchEvent;
        }
        AddToQueue(args);
    }

    private static IReadOnlyList<RawPointerPoint> GetPooledList() => new PooledList<RawPointerPoint>();
    private static readonly Func<IReadOnlyList<RawPointerPoint>> s_getPooledListDelegate = GetPooledList;

    private static void MergeEvents(RawPointerEventArgs last, RawPointerEventArgs current)
    {
        
        last.IntermediatePoints ??= new Lazy<IReadOnlyList<RawPointerPoint>?>(s_getPooledListDelegate);
        ((PooledList<RawPointerPoint>)last.IntermediatePoints.Value!).Add(new RawPointerPoint { Position = last.Position });
        last.Position = current.Position;
        last.Timestamp = current.Timestamp;
        last.InputModifiers = current.InputModifiers;
    }

    public void Dispose()
    {
        _disposed = true;
        _lastEvent = null;
        _lastTouchPoints.Clear();
    }
}

