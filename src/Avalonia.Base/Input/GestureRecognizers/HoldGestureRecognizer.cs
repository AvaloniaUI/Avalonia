using System.Timers;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Threading;

namespace Avalonia.Input
{
    public class HoldGestureRecognizer : StyledElement, IGestureRecognizer
    {
        private const int Tolerance = 30;
        private IInputElement? _target;
        private IGestureRecognizerActionsDispatcher? _actions;
        private int _gestureId;
        private IPointer? _tracking;
        private PointerPressedEventArgs? _pointerEventArgs;
        private Rect _trackingBounds;
        private Timer? _holdTimer;
        private bool _elasped;

        /// <summary>
        /// Defines the <see cref="IsHoldWithMouseEnabled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsHoldWithMouseEnabledProperty =
            AvaloniaProperty.Register<HoldGestureRecognizer, bool>(
                nameof(IsHoldWithMouseEnabled));
        
        /// <summary>
        /// Gets or sets whether to detect hold from the mouse
        /// </summary>
        public bool IsHoldWithMouseEnabled
        {
            get => GetValue(IsHoldWithMouseEnabledProperty);
            set => SetValue(IsHoldWithMouseEnabledProperty, value);
        }

        public void Initialize(IInputElement target, IGestureRecognizerActionsDispatcher actions)
        {
            _target = target;
            _actions = actions;

            _target?.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, Interactivity.RoutingStrategies.Tunnel | Interactivity.RoutingStrategies.Bubble);
            _target?.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, Interactivity.RoutingStrategies.Tunnel | Interactivity.RoutingStrategies.Bubble);

            _holdTimer = new Timer(300);
            _holdTimer.AutoReset = false;
            _holdTimer.Elapsed += HoldTimer_Elapsed;
        }

        private async void HoldTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            _elasped = true;
            _holdTimer?.Stop();

            if(_tracking != null)
            {
                await Dispatcher.UIThread.InvokeAsync(() => _target?.RaiseEvent(new HoldGestureEventArgs(_gestureId, _pointerEventArgs, HoldingState.Started)));
            }
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
                EndHold(!_elasped);
            }
        }

        public void PointerMoved(PointerEventArgs e)
        {
            if (_tracking == e.Pointer && _target is Visual visual)
            {
                var currentPosition = e.GetPosition(visual);

                if (!_trackingBounds.Contains(currentPosition))
                {
                    EndHold(true);
                }
            }
        }

        public void PointerPressed(PointerPressedEventArgs e)
        {
            if (_target != null && _target is Visual visual && (IsHoldWithMouseEnabled || e.Pointer.Type == PointerType.Touch || e.Pointer.Type == PointerType.Pen))
            {
                _elasped = false;
                var position = e.GetPosition(visual);
                _gestureId = HoldGestureEventArgs.GetNextFreeId();
                _tracking = e.Pointer;
                _pointerEventArgs = e;

                _trackingBounds = new Rect(position.X - Tolerance / 2, position.Y - Tolerance / 2, Tolerance, Tolerance);

                _holdTimer?.Start();
            }
        }

        public void PointerReleased(PointerReleasedEventArgs e)
        {
            if (_tracking == e.Pointer)
            {
                EndHold(!_elasped);
            }
        }

        private void EndHold(bool cancelled)
        {
            _holdTimer?.Stop();

            _tracking = null;
            _trackingBounds = default;

            _target?.RaiseEvent(new HoldGestureEventArgs(_gestureId, _pointerEventArgs, cancelled ? HoldingState.Cancelled : HoldingState.Completed));
        }
    }
}
