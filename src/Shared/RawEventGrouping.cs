#nullable enable
using System;
using System.Collections.Generic;
using Avalonia.Collections.Pooled;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Threading;
using JetBrains.Annotations;

namespace Avalonia;

/*
  This helper maintains an input queue for backends that handle input asynchronously.
  While doing that it groups Move and TouchUpdate events so we could provide GetIntermediatePoints API
 */

internal class RawEventGrouper : IDisposable
{
    private readonly Action<RawInputEventArgs> _eventCallback;
    private readonly Queue<RawInputEventArgs> _inputQueue = new();
    private readonly Action _dispatchFromQueue;
    readonly Dictionary<long, RawTouchEventArgs> _lastTouchPoints = new();
    RawInputEventArgs? _lastEvent;

    public RawEventGrouper(Action<RawInputEventArgs> eventCallback)
    {
        _eventCallback = eventCallback;
        _dispatchFromQueue = DispatchFromQueue;
    }
    
    private void AddToQueue(RawInputEventArgs args)
    {
        _lastEvent = args;
        _inputQueue.Enqueue(args);
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

            if (_lastEvent == ev) 
                _lastEvent = null;
            
            if (ev is RawTouchEventArgs { Type: RawPointerEventType.TouchUpdate } touchUpdate)
                _lastTouchPoints.Remove(touchUpdate.TouchPointId);

            _eventCallback?.Invoke(ev);

            if (ev is RawPointerEventArgs { IntermediatePoints.Value: PooledList<RawPointerPoint> list }) 
                list.Dispose();

            if (Dispatcher.UIThread.HasJobsWithPriority(DispatcherPriority.Input + 1))
            {
                Dispatcher.UIThread.Post(_dispatchFromQueue, DispatcherPriority.Input);
                return;
            }
        }
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
                if (_lastTouchPoints.TryGetValue(touchEvent.TouchPointId, out var lastTouchEvent))
                    MergeEvents(lastTouchEvent, touchEvent);
                else
                {
                    _lastTouchPoints[touchEvent.TouchPointId] = touchEvent;
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
                _lastTouchPoints[touchEvent.TouchPointId] = touchEvent;
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
        _inputQueue.Clear();
        _lastEvent = null;
        _lastTouchPoints.Clear();
    }
}

