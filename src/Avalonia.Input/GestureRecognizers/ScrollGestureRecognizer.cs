using System;
using Avalonia.Interactivity;

namespace Avalonia.Input.GestureRecognizers
{
    public class ScrollGestureRecognizer 
        : StyledElement, // It's not an "element" in any way, shape or form, but TemplateBinding refuse to work otherwise
            IGestureRecognizer
    {
        private bool _scrolling;
        private Point _trackedRootPoint;
        private IPointer _tracking;
        private IInputElement _target;
        private IGestureRecognizerActionsDispatcher _actions;
        private bool _canHorizontallyScroll;
        private bool _canVerticallyScroll;
        private int _gestureId;
        
        /// <summary>
        /// Defines the <see cref="CanHorizontallyScroll"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollGestureRecognizer, bool> CanHorizontallyScrollProperty =
            AvaloniaProperty.RegisterDirect<ScrollGestureRecognizer, bool>(
                nameof(CanHorizontallyScroll),
                o => o.CanHorizontallyScroll,
                (o, v) => o.CanHorizontallyScroll = v);

        /// <summary>
        /// Defines the <see cref="CanVerticallyScroll"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollGestureRecognizer, bool> CanVerticallyScrollProperty =
            AvaloniaProperty.RegisterDirect<ScrollGestureRecognizer, bool>(
                nameof(CanVerticallyScroll),
                o => o.CanVerticallyScroll,
                (o, v) => o.CanVerticallyScroll = v);
        
        /// <summary>
        /// Gets or sets a value indicating whether the content can be scrolled horizontally.
        /// </summary>
        public bool CanHorizontallyScroll
        {
            get => _canHorizontallyScroll;
            set => SetAndRaise(CanHorizontallyScrollProperty, ref _canHorizontallyScroll, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the content can be scrolled horizontally.
        /// </summary>
        public bool CanVerticallyScroll
        {
            get => _canVerticallyScroll;
            set => SetAndRaise(CanVerticallyScrollProperty, ref _canVerticallyScroll, value);
        }
        

        public void Initialize(IInputElement target, IGestureRecognizerActionsDispatcher actions)
        {
            _target = target;
            _actions = actions;
        }
        
        public void PointerPressed(PointerPressedEventArgs e)
        {
            if (e.Pointer.IsPrimary && e.Pointer.Type == PointerType.Touch)
            {
                _tracking = e.Pointer;
                _scrolling = false;
                _trackedRootPoint = e.GetPosition(null);
            }
        }

        // Arbitrary chosen value, probably need to move that to platform settings or something
        private const double ScrollStartDistance = 30;
        public void PointerMoved(PointerEventArgs e)
        {
            if (e.Pointer == _tracking)
            {
                var rootPoint = e.GetPosition(null);
                if (!_scrolling)
                {
                    if (CanHorizontallyScroll && Math.Abs(_trackedRootPoint.X - rootPoint.X) > ScrollStartDistance)
                        _scrolling = true;
                    if (CanVerticallyScroll && Math.Abs(_trackedRootPoint.Y - rootPoint.Y) > ScrollStartDistance)
                        _scrolling = true;
                    if (_scrolling)
                    {
                        _actions.Capture(e.Pointer, this);
                        _gestureId = ScrollGestureEventArgs.GetNextFreeId();
                    }
                }

                if (_scrolling)
                {
                    var vector = _trackedRootPoint - rootPoint;
                    _trackedRootPoint = rootPoint;
                    _target.RaiseEvent(new ScrollGestureEventArgs(_gestureId, vector));
                    e.Handled = true;
                }
            }
        }

        public void PointerCaptureLost(PointerCaptureLostEventArgs e)
        {
            if (e.Pointer == _tracking) EndGesture();
        }

        void EndGesture()
        {
            _tracking = null;
            if (_scrolling)
            {
                _scrolling = false;
                _target.RaiseEvent(new ScrollGestureEndedEventArgs(_gestureId));
            }
            
        }
        
        
        public void PointerReleased(PointerReleasedEventArgs e)
        {
            // TODO: handle inertia
            if (e.Pointer == _tracking && _scrolling)
            {
                e.Handled = true;
                EndGesture();
            }
        }
    }
}
