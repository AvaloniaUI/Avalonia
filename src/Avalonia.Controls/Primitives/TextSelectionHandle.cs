using System;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// A controls that enables easy control over text selection using touch based input
    /// </summary>
    public class TextSelectionHandle : Thumb
    {
        internal SelectionHandleType SelectionHandleType { get; set; }

        private Point _startPosition;

        internal Point IndicatorPosition => IsDragging ? _startPosition.WithX(_startPosition.X) + _delta : Bounds.Position.WithX(Bounds.Position.X).WithY(Bounds.Y);

        internal bool IsDragging { get; private set; }

        private Vector _delta;
        private Point? _lastPoint;
        private TranslateTransform? _transform;

        static TextSelectionHandle()
        {
            DragStartedEvent.AddClassHandler<TextSelectionHandle>((x, e) => x.OnDragStarted(e), RoutingStrategies.Bubble);
            DragDeltaEvent.AddClassHandler<TextSelectionHandle>((x, e) => x.OnDragDelta(e), RoutingStrategies.Bubble);
            DragCompletedEvent.AddClassHandler<TextSelectionHandle>((x, e) => x.OnDragCompleted(e), RoutingStrategies.Bubble);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (_transform == null)
            {
                _transform = new TranslateTransform();
            }

            RenderTransform = _transform;
        }

        protected override void OnLoaded(RoutedEventArgs args)
        {
            base.OnLoaded(args);

            InvalidateMeasure();
        }
        protected override void OnDragStarted(VectorEventArgs e)
        {
            base.OnDragStarted(e);

            _startPosition = Bounds.Position;
            _delta = default;
            IsDragging = true;
        }

        protected override void OnDragDelta(VectorEventArgs e)
        {
            base.OnDragDelta(e);

            _delta = e.Vector;
            UpdateTextSelectionHandlePosition();
        }

        protected override void OnDragCompleted(VectorEventArgs e)
        {
            base.OnDragCompleted(e);
            IsDragging = false;

            _startPosition = default;
        }

        private void UpdateTextSelectionHandlePosition()
        {
            SetTopLeft(IndicatorPosition);
        }

        protected override void ArrangeCore(Rect finalRect)
        {
            base.ArrangeCore(finalRect);

            if (_transform != null)
            {
                if (SelectionHandleType == SelectionHandleType.Caret)
                {
                    HasMirrorTransform = true;
                    _transform.X = Bounds.Width / 2 * -1;
                }
                else if (SelectionHandleType == SelectionHandleType.Start)
                {
                    HasMirrorTransform = true;
                    _transform.X = Bounds.Width * -1;
                }
                else
                {
                    HasMirrorTransform = false;
                    _transform.X = 0;
                }
            }
        }

        internal void SetTopLeft(Point point)
        {
            Canvas.SetTop(this, point.Y);
            Canvas.SetLeft(this, point.X);
        }

        internal Point GetTopLeft()
        {
            return new Point(Canvas.GetLeft(this), Canvas.GetTop(this));
        }

        protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
        {
            if (_lastPoint.HasValue)
            {
                var ev = new VectorEventArgs
                {
                    RoutedEvent = DragCompletedEvent,
                    Vector = _lastPoint.Value,
                };

                _lastPoint = null;

                RaiseEvent(ev);
            }

            PseudoClasses.Remove(":pressed");

            base.OnPointerCaptureLost(e);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (_lastPoint.HasValue)
            {
                var ev = new VectorEventArgs
                {
                    RoutedEvent = DragDeltaEvent,
                    Vector = e.GetPosition(VisualRoot as Visual) - _lastPoint.Value,
                };

                RaiseEvent(ev);
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            e.Handled = true;
            _lastPoint = e.GetPosition(VisualRoot as Visual);

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
                    Vector = (Vector)e.GetPosition(VisualRoot as Visual),
                };

                RaiseEvent(ev);
            }

            PseudoClasses.Remove(":pressed");
        }
    }
}
