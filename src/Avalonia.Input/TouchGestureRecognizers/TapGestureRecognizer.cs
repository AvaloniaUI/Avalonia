using System;
using Avalonia.Interactivity;

namespace Avalonia.Input.TouchGestureRecognizers
{
    /*
    public class TapGestureRecognizer : ITouchGestureRecognizer
    {
        long _started;
        Point _startPoint;
        const double Distance = 20;
        const long MaxTapDuration = 500;
        TouchGestureRecognizerResult ITouchGestureRecognizer.RecognizeGesture(IInputElement owner, TouchEventArgs args)
        {
            if (args.Route == RoutingStrategies.Tunnel)
                return TouchGestureRecognizerResult.Continue;
            
            // Multi-touch sequence
            if(args.Touches.Count > 1)
                return TouchGestureRecognizerResult.Reject;
            // Sequence started, save the start time
            if(args.Type == TouchEventType.TouchBegin)
            {
                _started = args.Timestamp;
                var pos = args.Touches[0].GetPosition(owner);
                if (pos == null)
                    return TouchGestureRecognizerResult.Reject;
                _startPoint = pos.Value;
                return TouchGestureRecognizerResult.Continue;
            }
            
            if(args.Type == TouchEventType.TouchEnd)
            {
                var pos = args.RemovedTouches[0].GetPosition(owner);
                if (pos == null)
                    return TouchGestureRecognizerResult.Reject;
                var endPoint = pos.Value;
                
                if(Math.Abs(endPoint.X - _startPoint.X) < Distance
                   && Math.Abs(endPoint.Y - _startPoint.Y) < Distance
                   && (args.Timestamp - _started) < MaxTapDuration)
                {
                    ((Interactive)args.RemovedTouches[0].InitialTarget).RaiseEvent(
                        new RoutedEventArgs(Gestures.TappedEvent));
                    return TouchGestureRecognizerResult.Accept;
                }
                else
                    return TouchGestureRecognizerResult.Reject;
            }
            return TouchGestureRecognizerResult.Continue;
        }

        public void Cancel()
        {
            _started = 0;
            _startPoint = default;
        }
    }
    */
}
