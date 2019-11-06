// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.Controls.Primitives
{
    public class Thumb : TemplatedControl
    {
        public static readonly RoutedEvent<VectorEventArgs> DragStartedEvent =
            RoutedEvent.Register<Thumb, VectorEventArgs>(nameof(DragStarted), RoutingStrategies.Bubble);

        public static readonly RoutedEvent<VectorEventArgs> DragDeltaEvent =
            RoutedEvent.Register<Thumb, VectorEventArgs>(nameof(DragDelta), RoutingStrategies.Bubble);

        public static readonly RoutedEvent<VectorEventArgs> DragCompletedEvent =
            RoutedEvent.Register<Thumb, VectorEventArgs>(nameof(DragCompleted), RoutingStrategies.Bubble);

        private Point? _lastPoint;

        static Thumb()
        {
            DragStartedEvent.AddClassHandler<Thumb>((x,e) => x.OnDragStarted(e), RoutingStrategies.Bubble);
            DragDeltaEvent.AddClassHandler<Thumb>((x, e) => x.OnDragDelta(e), RoutingStrategies.Bubble);
            DragCompletedEvent.AddClassHandler<Thumb>((x, e) => x.OnDragCompleted(e), RoutingStrategies.Bubble);
        }

        public event EventHandler<VectorEventArgs> DragStarted
        {
            add { AddHandler(DragStartedEvent, value); }
            remove { RemoveHandler(DragStartedEvent, value); }
        }

        public event EventHandler<VectorEventArgs> DragDelta
        {
            add { AddHandler(DragDeltaEvent, value); }
            remove { RemoveHandler(DragDeltaEvent, value); }
        }

        public event EventHandler<VectorEventArgs> DragCompleted
        {
            add { AddHandler(DragCompletedEvent, value); }
            remove { RemoveHandler(DragCompletedEvent, value); }
        }

        protected virtual void OnDragStarted(VectorEventArgs e)
        {
        }

        protected virtual void OnDragDelta(VectorEventArgs e)
        {
        }

        protected virtual void OnDragCompleted(VectorEventArgs e)
        {
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (_lastPoint.HasValue)
            {
                var ev = new VectorEventArgs
                {
                    RoutedEvent = DragDeltaEvent,
                    Vector = e.GetPosition(this) - _lastPoint.Value,
                };

                RaiseEvent(ev);
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            e.Handled = true;
            _lastPoint = e.GetPosition(this);

            var ev = new VectorEventArgs
            {
                RoutedEvent = DragStartedEvent,
                Vector = (Vector)_lastPoint,
            };

            PseudoClasses.Add(":pressed");

            RaiseEvent(ev);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (_lastPoint.HasValue)
            {
                e.Handled = true;
                _lastPoint = null;

                var ev = new VectorEventArgs
                {
                    RoutedEvent = DragCompletedEvent,
                    Vector = (Vector)e.GetPosition(this),
                };

                RaiseEvent(ev);
            }

            PseudoClasses.Remove(":pressed");
        }
    }
}
