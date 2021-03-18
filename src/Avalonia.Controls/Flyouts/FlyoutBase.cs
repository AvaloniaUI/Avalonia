using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Avalonia.Layout;

#nullable enable

namespace Avalonia.Controls.Primitives
{
    public abstract class FlyoutBase : AvaloniaObject
    {
        private static readonly DirectProperty<FlyoutBase, bool> IsOpenProperty =
           AvaloniaProperty.RegisterDirect<FlyoutBase, bool>(nameof(IsOpen),
               x => x.IsOpen);

        public static readonly DirectProperty<FlyoutBase, Control?> TargetProperty =
            AvaloniaProperty.RegisterDirect<FlyoutBase, Control?>(nameof(Target), x => x.Target);

        public static readonly DirectProperty<FlyoutBase, FlyoutPlacementMode> PlacementProperty =
            AvaloniaProperty.RegisterDirect<FlyoutBase, FlyoutPlacementMode>(nameof(Placement),
                x => x.Placement, (x, v) => x.Placement = v);

        public static readonly AttachedProperty<FlyoutBase?> AttachedFlyoutProperty =
            AvaloniaProperty.RegisterAttached<FlyoutBase, Control, FlyoutBase?>("AttachedFlyout", null);

        private bool _isOpen;
        private Control? _target;
        protected Popup? _popup;

        public bool IsOpen
        {
            get => _isOpen;
            private set => SetAndRaise(IsOpenProperty, ref _isOpen, value);
        }

        public FlyoutPlacementMode Placement
        {
            get => GetValue(PlacementProperty);
            set => SetValue(PlacementProperty, value);
        }

        public Control? Target
        {
            get => _target;
            private set => SetAndRaise(TargetProperty, ref _target, value);
        }

        public event EventHandler? Closed;
        public event EventHandler<CancelEventArgs>? Closing;
        public event EventHandler? Opened;
        public event EventHandler? Opening;

        public static FlyoutBase? GetAttachedFlyout(Control element)
        {
            return element.GetValue(AttachedFlyoutProperty);
        }

        public static void SetAttachedFlyout(Control element, FlyoutBase? value)
        {
            element.SetValue(AttachedFlyoutProperty, value);
        }

        public static void ShowAttachedFlyout(Control flyoutOwner)
        {
            var flyout = GetAttachedFlyout(flyoutOwner);
            flyout?.ShowAt(flyoutOwner);
        }

        public void ShowAt(Control placementTarget)
        {
            ShowAtCore(placementTarget);
        }

        public void ShowAt(Control placementTarget, bool showAtPointer)
        {
            ShowAtCore(placementTarget, showAtPointer);
        }

        public void Hide(bool canCancel = true)
        {
            if (!IsOpen)
            {
                return;
            }

            if (canCancel)
            {
                bool cancel = false;

                var closing = new CancelEventArgs();
                Closing?.Invoke(this, closing);
                if (cancel || closing.Cancel)
                {
                    return;
                }
            }

            IsOpen = _popup.IsOpen = false;
            
            OnClosed();
        }

        protected virtual void ShowAtCore(Control placementTarget, bool showAtPointer = false)
        {
            if (placementTarget == null)
                throw new ArgumentNullException("placementTarget cannot be null");

            if (_popup == null)
            {
                InitPopup();
            }

            if (IsOpen)
            {
                if (placementTarget == Target)
                {
                    return;
                }
                else // Close before opening a new one
                {
                    Hide(false);
                }
            }

            if (_popup.Parent != null && _popup.Parent != placementTarget)
            {
                ((ISetLogicalParent)_popup).SetParent(null);
            }

            _popup.PlacementTarget = Target = placementTarget;

            ((ISetLogicalParent)_popup).SetParent(placementTarget);

            if (_popup.Child == null)
            {
                _popup.Child = CreatePresenter();
            }

            OnOpening();
            IsOpen = _popup.IsOpen = true;
            PositionPopup(showAtPointer);
            OnOpened();
        }

        protected virtual void OnOpening()
        {
            Opening?.Invoke(this, null);
        }

        protected virtual void OnOpened()
        {
            Opened?.Invoke(this, null);
        }

        protected virtual void OnClosing(CancelEventArgs args)
        {
            Closing?.Invoke(this, args);
        }

        protected virtual void OnClosed()
        {
            Closed?.Invoke(this, null);
        }

        protected abstract Control CreatePresenter();

        private void InitPopup()
        {
            _popup = new Popup();
            _popup.WindowManagerAddShadowHint = false;
            _popup.IsLightDismissEnabled = true;            

            _popup.Opened += OnPopupOpened;
            _popup.Closed += OnPopupClosed;
        }

        private void OnPopupOpened(object sender, EventArgs e)
        {
            IsOpen = true;
            OnOpened();
        }

        private void OnPopupClosed(object sender, EventArgs e)
        {
            Hide();
        }

        private void PositionPopup(bool showAtPointer)
        {
            Size sz;
            if(_popup.DesiredSize == Size.Empty)
            {
                sz = LayoutHelper.MeasureChild(_popup, Size.Infinity, new Thickness());
            }
            else
            {
                sz = _popup.DesiredSize;
            }

            if (showAtPointer)
            {
                _popup.PlacementMode = PlacementMode.Pointer;
            }
            else
            {
                _popup.PlacementMode = PlacementMode.AnchorAndGravity;
                _popup.PlacementConstraintAdjustment =
                    PopupPositioning.PopupPositionerConstraintAdjustment.SlideX |
                    PopupPositioning.PopupPositionerConstraintAdjustment.SlideY;
            }


            var trgtBnds = Target?.Bounds ?? Rect.Empty;

            switch (Placement)
            {
                case FlyoutPlacementMode.Top: //Above & centered
                    _popup.PlacementRect = new Rect(-sz.Width / 2, 0, sz.Width, 1);
                    _popup.PlacementGravity = PopupPositioning.PopupGravity.Top;

                    break;

                case FlyoutPlacementMode.TopEdgeAlignedLeft:
                    _popup.PlacementRect = new Rect(0, 0, 0, 0);
                    _popup.PlacementGravity = PopupPositioning.PopupGravity.TopRight;
                    
                    break;

                case FlyoutPlacementMode.TopEdgeAlignedRight:
                    _popup.PlacementRect = new Rect(trgtBnds.Width - 1, 0, 10, 1);
                    _popup.PlacementGravity = PopupPositioning.PopupGravity.TopLeft;
                    
                    break;

                case FlyoutPlacementMode.RightEdgeAlignedTop:
                    _popup.PlacementRect = new Rect(trgtBnds.Width - 1, 0, 1, 1);
                    _popup.PlacementGravity = PopupPositioning.PopupGravity.BottomRight;
                    _popup.PlacementAnchor = PopupPositioning.PopupAnchor.Right;

                    break;

                case FlyoutPlacementMode.Right: //Right & centered
                    _popup.PlacementRect = new Rect(trgtBnds.Width - 1, 0, 1, trgtBnds.Height);
                    _popup.PlacementGravity = PopupPositioning.PopupGravity.Right;
                    _popup.PlacementAnchor = PopupPositioning.PopupAnchor.Right;

                    break;

                case FlyoutPlacementMode.RightEdgeAlignedBottom:
                    _popup.PlacementRect = new Rect(trgtBnds.Width - 1, trgtBnds.Height - 1, 1, 1);
                    _popup.PlacementGravity = PopupPositioning.PopupGravity.TopRight;
                    _popup.PlacementAnchor = PopupPositioning.PopupAnchor.Right;

                    break;

                case FlyoutPlacementMode.Bottom: //Below & centered
                    _popup.PlacementRect = new Rect(0, trgtBnds.Height - 1, trgtBnds.Width, 1);
                    _popup.PlacementGravity = PopupPositioning.PopupGravity.Bottom;
                    _popup.PlacementAnchor = PopupPositioning.PopupAnchor.Bottom;

                    break;

                case FlyoutPlacementMode.BottomEdgeAlignedLeft:
                    _popup.PlacementRect = new Rect(0, trgtBnds.Height - 1, 1, 1);
                    _popup.PlacementGravity = PopupPositioning.PopupGravity.BottomRight;
                    _popup.PlacementAnchor = PopupPositioning.PopupAnchor.Bottom;

                    break;

                case FlyoutPlacementMode.BottomEdgeAlignedRight:
                    _popup.PlacementRect = new Rect(trgtBnds.Width - 1, trgtBnds.Height - 1, 1, 1);
                    _popup.PlacementGravity = PopupPositioning.PopupGravity.BottomLeft;
                    _popup.PlacementAnchor = PopupPositioning.PopupAnchor.Bottom;

                    break;

                case FlyoutPlacementMode.LeftEdgeAlignedTop:
                    _popup.PlacementRect = new Rect(0, 0, 1, 1);
                    _popup.PlacementGravity = PopupPositioning.PopupGravity.BottomLeft;
                    _popup.PlacementAnchor = PopupPositioning.PopupAnchor.Left;

                    break;

                case FlyoutPlacementMode.Left: //Left & centered
                    _popup.PlacementRect = new Rect(0, 0, 1, trgtBnds.Height);
                    _popup.PlacementGravity = PopupPositioning.PopupGravity.Left;
                    _popup.PlacementAnchor = PopupPositioning.PopupAnchor.Left;

                    break;

                case FlyoutPlacementMode.LeftEdgeAlignedBottom:
                    _popup.PlacementRect = new Rect(0, trgtBnds.Height - 1, 1, 1);
                    _popup.PlacementGravity = PopupPositioning.PopupGravity.TopLeft;
                    _popup.PlacementAnchor = PopupPositioning.PopupAnchor.BottomLeft;

                    break;

                case FlyoutPlacementMode.Full:
                    //Not sure how the get this to work
                    //Popup should display at max size in the middle of the VisualRoot/Window of the Target
                    throw new NotSupportedException("FlyoutPlacementMode.Full is not supported at this time");
                //break;

                //includes Auto (not sure what determines that)...
                default:
                    //This is just FlyoutPlacementMode.Top behavior (above & centered)
                    _popup.PlacementRect = new Rect(-sz.Width / 2, 0, sz.Width, 1);
                    _popup.PlacementGravity = PopupPositioning.PopupGravity.Top;

                    break;
            }
        }
    }
}
