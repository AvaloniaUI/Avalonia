using System;
using Avalonia.Input.GestureRecognizers;

namespace Avalonia.Input
{
    public class PullGestureRecognizer : GestureRecognizer
    {
        internal static int MinPullDetectionSize = 50;
        private Point _initialPosition;
        private int _gestureId;
        private IPointer? _tracking;
        private bool _pullInProgress;

        /// <summary>
        /// Defines the <see cref="PullDirection"/> property.
        /// </summary>
        public static readonly StyledProperty<PullDirection> PullDirectionProperty =
            AvaloniaProperty.Register<PullGestureRecognizer, PullDirection>(nameof(PullDirection));

        public PullDirection PullDirection
        {
            get => GetValue(PullDirectionProperty);
            set => SetValue(PullDirectionProperty, value);
        }

        public PullGestureRecognizer(PullDirection pullDirection)
        {
            PullDirection = pullDirection;
        }

        public PullGestureRecognizer() { }

        protected override void PointerCaptureLost(IPointer pointer)
        {
            if (_tracking == pointer)
            {
                EndPull();
            }
        }

        protected override void PointerMoved(PointerEventArgs e)
        {
            if (_tracking == e.Pointer && Target is Visual visual)
            {
                var currentPosition = e.GetPosition(visual);
                Capture(e.Pointer);

                Vector delta = default;
                switch (PullDirection)
                {
                    case PullDirection.TopToBottom:
                        if (currentPosition.Y > _initialPosition.Y)
                        {
                            delta = new Vector(0, currentPosition.Y - _initialPosition.Y);
                        }
                        break;
                    case PullDirection.BottomToTop:
                        if (currentPosition.Y < _initialPosition.Y)
                        {
                            delta = new Vector(0, _initialPosition.Y - currentPosition.Y);
                        }
                        break;
                    case PullDirection.LeftToRight:
                        if (currentPosition.X > _initialPosition.X)
                        {
                            delta = new Vector(currentPosition.X - _initialPosition.X, 0);
                        }
                        break;
                    case PullDirection.RightToLeft:
                        if (currentPosition.X < _initialPosition.X)
                        {
                            delta = new Vector(_initialPosition.X - currentPosition.X, 0);
                        }
                        break;
                }

                _pullInProgress = true;
                var pullEventArgs = new PullGestureEventArgs(_gestureId, delta, PullDirection);
                Target?.RaiseEvent(pullEventArgs);

                e.Handled = pullEventArgs.Handled;
            }
        }

        protected override void PointerPressed(PointerPressedEventArgs e)
        {
            if (Target != null && Target is Visual visual && (e.Pointer.Type == PointerType.Touch || e.Pointer.Type == PointerType.Pen))
            {
                var position = e.GetPosition(visual);

                var canPull = false;

                var bounds = visual.Bounds;

                switch (PullDirection)
                {
                    case PullDirection.TopToBottom:
                        canPull = position.Y < Math.Max(MinPullDetectionSize, bounds.Height * 0.1);
                        break;
                    case PullDirection.BottomToTop:
                        canPull = position.Y > Math.Min(bounds.Height - MinPullDetectionSize, bounds.Height - (bounds.Height * 0.1));
                        break;
                    case PullDirection.LeftToRight:
                        canPull = position.X < Math.Max(MinPullDetectionSize, bounds.Width * 0.1);
                        break;
                    case PullDirection.RightToLeft:
                        canPull = position.X > Math.Min(bounds.Width - MinPullDetectionSize, bounds.Width - (bounds.Width * 0.1));
                        break;
                }

                if (canPull)
                {
                    _gestureId = PullGestureEventArgs.GetNextFreeId();
                    _tracking = e.Pointer;
                    _initialPosition = position;
                }
            }
        }

        protected override void PointerReleased(PointerReleasedEventArgs e)
        {
            if (_tracking == e.Pointer && _pullInProgress)
            {
                EndPull();
            }
        }

        private void EndPull()
        {
            _tracking = null;
            _initialPosition = default;
            _pullInProgress = false;

            Target?.RaiseEvent(new PullGestureEndedEventArgs(_gestureId, PullDirection));
        }
    }
}
