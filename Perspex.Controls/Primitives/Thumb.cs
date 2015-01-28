// -----------------------------------------------------------------------
// <copyright file="Thumb.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Primitives
{
    using System;
    using Perspex.Input;
    using Perspex.Interactivity;

    public class Thumb : TemplatedControl
    {
        public static readonly RoutedEvent<VectorEventArgs> DragStartedEvent =
            RoutedEvent.Register<Thumb, VectorEventArgs>("DragStarted", RoutingStrategy.Bubble);

        public static readonly RoutedEvent<VectorEventArgs> DragDeltaEvent =
            RoutedEvent.Register<Thumb, VectorEventArgs>("DragDelta", RoutingStrategy.Bubble);

        public static readonly RoutedEvent<VectorEventArgs> DragCompletedEvent =
            RoutedEvent.Register<Thumb, VectorEventArgs>("DragCompleted", RoutingStrategy.Bubble);

        private Point? lastPoint;

        static Thumb()
        {
            DragStartedEvent.AddClassHandler<Thumb>(x => x.OnDragStarted);
            DragDeltaEvent.AddClassHandler<Thumb>(x => x.OnDragDelta);
            DragCompletedEvent.AddClassHandler<Thumb>(x => x.OnDragCompleted);
        }

        public event EventHandler<VectorEventArgs> DragStarted
        {
            add { this.AddHandler(DragStartedEvent, value); }
            remove { this.RemoveHandler(DragStartedEvent, value); }
        }

        public event EventHandler<VectorEventArgs> DragDelta
        {
            add { this.AddHandler(DragDeltaEvent, value); }
            remove { this.RemoveHandler(DragDeltaEvent, value); }
        }

        public event EventHandler<VectorEventArgs> DragCompleted
        {
            add { this.AddHandler(DragCompletedEvent, value); }
            remove { this.RemoveHandler(DragCompletedEvent, value); }
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
            if (this.lastPoint.HasValue)
            {
                var ev = new VectorEventArgs
                {
                    RoutedEvent = DragDeltaEvent,
                    Vector = e.GetPosition(this) - this.lastPoint.Value,
                };

                this.RaiseEvent(ev);
            }
        }

        protected override void OnPointerPressed(PointerPressEventArgs e)
        {
            e.Device.Capture(this);
            this.lastPoint = e.GetPosition(this);

            var ev = new VectorEventArgs
            {
                RoutedEvent = DragStartedEvent,
                Vector = (Vector)this.lastPoint,
            };

            this.RaiseEvent(ev);
        }

        protected override void OnPointerReleased(PointerEventArgs e)
        {
            if (this.lastPoint.HasValue)
            {
                e.Device.Capture(null);
                this.lastPoint = null;

                var ev = new VectorEventArgs
                {
                    RoutedEvent = DragCompletedEvent,
                    Vector = (Vector)e.GetPosition(this),
                };

                this.RaiseEvent(ev);
            }
        }
    }
}
