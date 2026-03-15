using System;
using System.Drawing;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// A controls that enables easy control over text selection using touch based input
    /// </summary>
    [TemplatePart("PART_Indicator", typeof(Border))]
    public class TextSelectionHandle : Thumb
    {
        internal SelectionHandleType SelectionHandleType
        {
            get => field;
            set
            {
                field = value;
                UpdateHandleClasses();
            }
        }

        internal Point IndicatorPosition
        {
            get
            {
                var topLeft = GetTopLeft();
                return topLeft.WithX(topLeft.X + GetIndicatorOffset());
            }
        }

        internal bool IsDragging { get; private set; }

        private Point _startPosition;
        private Vector _delta;
        private Point? _lastPoint;
        private Border? _indicator;
        private Point? _initialRequestedPosition;

        static TextSelectionHandle()
        {
            DragStartedEvent.AddClassHandler<TextSelectionHandle>((x, e) => x.OnDragStarted(e), RoutingStrategies.Bubble);
            DragDeltaEvent.AddClassHandler<TextSelectionHandle>((x, e) => x.OnDragDelta(e), RoutingStrategies.Bubble);
            DragCompletedEvent.AddClassHandler<TextSelectionHandle>((x, e) => x.OnDragCompleted(e), RoutingStrategies.Bubble);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _indicator = e.NameScope.Get<Border>("PART_Indicator");

            UpdateHandleClasses();
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);

            if(_initialRequestedPosition.HasValue)
            {
                SetTopLeft(_initialRequestedPosition.Value);
                _initialRequestedPosition = null;
            }
        }

        protected override void OnLoaded(RoutedEventArgs args)
        {
            base.OnLoaded(args);

            UpdateHandleClasses();

            InvalidateVisual();
        }
        protected override void OnDragStarted(VectorEventArgs e)
        {
            base.OnDragStarted(e);

            _startPosition = GetTopLeft();
            _delta = default;
            IsDragging = true;
        }

        protected override void OnDragDelta(VectorEventArgs e)
        {
            base.OnDragDelta(e);
            var newDelta = e.Vector;

            if (Math.Abs((newDelta - _delta).Length) > 0)
            {
                _delta = e.Vector;
                var point = _startPosition + _delta;
                Canvas.SetTop(this, point.Y);
                Canvas.SetLeft(this, point.X);
            }
        }

        protected override void OnDragCompleted(VectorEventArgs e)
        {
            base.OnDragCompleted(e);
            IsDragging = false;

            _startPosition = default;
        }

        protected override void ArrangeCore(Rect finalRect)
        {
            UpdateHandleClasses();

            base.ArrangeCore(finalRect);
        }

        internal void SetTopLeft(Point point)
        {
            if(_indicator == null)
            {
                _initialRequestedPosition = point;
            }
            Canvas.SetTop(this, point.Y);
            Canvas.SetLeft(this, point.X - GetIndicatorOffset());
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
            if (e.Pointer.Captured != this)
                return;

            VectorEventArgs ev;

            if (!_lastPoint.HasValue)
            {
                _lastPoint = e.GetPosition(VisualRoot as Visual);
                e.Pointer.Capture(this);

                ev = new VectorEventArgs
                {
                    RoutedEvent = DragStartedEvent,
                    Vector = (Vector)_lastPoint,
                };
            }
            else
            {
                var vector = e.GetPosition(VisualRoot as Visual) - _lastPoint.Value;

                ev = new VectorEventArgs
                {
                    RoutedEvent = DragDeltaEvent,
                    Vector = vector,
                };
            }

            RaiseEvent(ev);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            PseudoClasses.Add(":pressed");
            e.Pointer.Capture(this);
            var point = e.GetPosition(this);
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
            e.Pointer.Capture(null);
        }

        internal double GetIndicatorOffset()
        {
            return _indicator?.Bounds.Center.X ?? SelectionHandleType switch
            {
                SelectionHandleType.Caret => Bounds.Width / 2,
                SelectionHandleType.Start => Bounds.Width,
                SelectionHandleType.End => 0,
                _ => throw new NotImplementedException(),
            };
        }

        private void UpdateHandleClasses()
        {
            Classes.Remove("caret");
            Classes.Remove("start");
            Classes.Remove("end");

            Classes.Add(SelectionHandleType switch
            {
                SelectionHandleType.Caret => "caret",
                SelectionHandleType.Start => "start",
                SelectionHandleType.End => "end",
                _ => throw new NotImplementedException(),
            });

            InvalidateVisual();
        }
    }
}
