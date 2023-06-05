using System;
using System.Collections.Generic;
using Avalonia.Collections.Pooled;
using Avalonia.Input.Raw;

namespace Avalonia.Wayland
{
    internal class WlRawEventGrouper : IDisposable
    {
        private readonly Queue<RawPointerEventArgsWithWindow> _inputQueue = new();
        private readonly Dictionary<long, RawPointerEventArgs> _lastTouchPoints = new();

        private RawPointerEventArgsWithWindow? _lastEvent;
        private bool _disposed;

        public bool HasJobs => _inputQueue.Count > 0;

        public void DispatchNext()
        {
            if (_inputQueue.Count == 0)
                return;
            var ev = _inputQueue.Dequeue();
            Dispatch(ev);
        }

        private void AddToQueue(RawPointerEventArgsWithWindow args)
        {
            _lastEvent = args;
            _inputQueue.Enqueue(args);
        }

        private void Dispatch(RawPointerEventArgsWithWindow ev)
        {
            if (!_disposed)
            {
                if (_lastEvent == ev)
                    _lastEvent = null;

                if (ev.Args is RawTouchEventArgs { Type: RawPointerEventType.TouchUpdate } touchUpdate)
                    _lastTouchPoints.Remove(touchUpdate.RawPointerId);

                ev.Window.Input?.Invoke(ev.Args);
            }

            if (ev.Args is { IntermediatePoints.Value: PooledList<RawPointerPoint> list })
                list.Dispose();
        }

        public void HandleEvent(RawPointerEventArgsWithWindow args)
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
            if (_lastEvent is not null
                && _lastEvent.Args.Device == args.Args.Device
                && _lastEvent.Args.InputModifiers == args.Args.InputModifiers
                && _lastEvent.Args.Type == args.Args.Type
                && _lastEvent.Args.Type is RawPointerEventType.Move or RawPointerEventType.TouchUpdate)
            {
                if (args.Args is RawTouchEventArgs touchEvent)
                {
                    if (_lastTouchPoints.TryGetValue(touchEvent.RawPointerId, out var lastTouchEvent))
                    {
                        MergeEvents(lastTouchEvent, touchEvent);
                    }
                    else
                    {
                        _lastTouchPoints[touchEvent.RawPointerId] = touchEvent;
                        AddToQueue(args);
                    }
                }
                else
                {
                    MergeEvents(_lastEvent.Args, args.Args);
                }
            }
            else
            {
                _lastTouchPoints.Clear();
                if (args.Args is RawTouchEventArgs { Type: RawPointerEventType.TouchUpdate } touchEvent)
                    _lastTouchPoints[touchEvent.RawPointerId] = touchEvent;
                AddToQueue(args);
            }
        }

        private static void MergeEvents(RawPointerEventArgs last, RawPointerEventArgs current)
        {
            last.IntermediatePoints ??= new Lazy<IReadOnlyList<RawPointerPoint>?>(static () => new PooledList<RawPointerPoint>());
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

    internal class RawPointerEventArgsWithWindow
    {
        public RawPointerEventArgsWithWindow(WlWindow window, RawPointerEventArgs args)
        {
            Window = window;
            Args = args;
        }

        public WlWindow Window { get; }

        public RawPointerEventArgs Args { get; }
    }
}
