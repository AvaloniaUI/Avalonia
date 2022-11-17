using Avalonia.Input.GestureRecognizers;

namespace Avalonia.Input
{
    public class PullGestureRecognizer : StyledElement, IGestureRecognizer
    {
        private IInputElement? _target;
        private IGestureRecognizerActionsDispatcher? _actions;
        private Point _initialPosition;
        private int _gestureId;
        private IPointer? _tracking;
        private PullDirection _pullDirection;

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

        public void Initialize(IInputElement target, IGestureRecognizerActionsDispatcher actions)
        {
            _target = target;
            _actions = actions;

            _target?.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, Interactivity.RoutingStrategies.Tunnel | Interactivity.RoutingStrategies.Bubble);
            _target?.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, Interactivity.RoutingStrategies.Tunnel | Interactivity.RoutingStrategies.Bubble);
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            PointerPressed(e);
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            PointerReleased(e);
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
            if (_tracking == e.Pointer)
            {
                var currentPosition = e.GetPosition(_target);
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

                _target?.RaiseEvent(new PullGestureEventArgs(_gestureId, delta, PullDirection));
            }
        }

        public void PointerPressed(PointerPressedEventArgs e)
        {
            if (_target != null)// && (e.Pointer.Type == PointerType.Touch || e.Pointer.Type == PointerType.Pen))
            {
                var position = e.GetPosition(_target);

                var canPull = false;

                var bounds = _target.Bounds;

                switch (PullDirection)
                {
                    case PullDirection.TopToBottom:
                        canPull = position.Y < bounds.Height * 0.1;
                        break;
                    case PullDirection.BottomToTop:
                        canPull = position.Y > bounds.Height - (bounds.Height * 0.1);
                        break;
                    case PullDirection.LeftToRight:
                        canPull = position.X < bounds.Width * 0.1;
                        break;
                    case PullDirection.RightToLeft:
                        canPull = position.X > bounds.Width - (bounds.Width * 0.1);
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
            if (_tracking == e.Pointer)
            {
                EndPull();
            }
        }

        private void EndPull()
        {
            _tracking = null;
            _initialPosition = default;

            _target?.RaiseEvent(new PullGestureEndedEventArgs(_gestureId, PullDirection));
        }
    }
}
