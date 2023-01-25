using System;
using System.Diagnostics;
using Avalonia.Input.GestureRecognizers;

namespace Avalonia.Input
{
    public class PullGestureRecognizer : StyledElement, IGestureRecognizer
    {
        internal static int MinPullDetectionSize = 50;

        private IInputElement? _target;
        private IGestureRecognizerActionsDispatcher? _actions;
        private Point _initialPosition;
        private int _gestureId;
        private IPointer? _tracking;
        private PullDirection _pullDirection;
        private bool _pullInProgress;

        /// <summary>
        /// Defines the <see cref="PullDirection"/> property.
        /// </summary>
        public static readonly DirectProperty<PullGestureRecognizer, PullDirection> PullDirectionProperty =
            AvaloniaProperty.RegisterDirect<PullGestureRecognizer, PullDirection>(
                nameof(PullDirection),
                o => o.PullDirection,
                (o, v) => o.PullDirection = v);

        public PullDirection PullDirection
        {
            get => _pullDirection;
            set => SetAndRaise(PullDirectionProperty, ref _pullDirection, value);
        }

        public PullGestureRecognizer(PullDirection pullDirection)
        {
            PullDirection = pullDirection;
        }

        public PullGestureRecognizer() { }

        public void Initialize(IInputElement target, IGestureRecognizerActionsDispatcher actions)
        {
            _target = target;
            _actions = actions;
        }

        public void PointerCaptureLost(IPointer pointer)
        {
            if (_tracking == pointer)
            {
                EndPull();
            }
        }

        public void PointerMoved(PointerEventArgs e)
        {
            if (_tracking == e.Pointer && _target is Visual visual)
            {
                var currentPosition = e.GetPosition(visual);
                _actions!.Capture(e.Pointer, this);

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
                _target?.RaiseEvent(pullEventArgs);

                e.Handled = pullEventArgs.Handled;
            }
        }

        public void PointerPressed(PointerPressedEventArgs e)
        {
            if (_target != null && _target is Visual visual && (e.Pointer.Type == PointerType.Touch || e.Pointer.Type == PointerType.Pen))
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

        public void PointerReleased(PointerReleasedEventArgs e)
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

            _target?.RaiseEvent(new PullGestureEndedEventArgs(_gestureId, PullDirection));
        }
    }
}
