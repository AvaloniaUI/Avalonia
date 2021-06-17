using System;
using System.ComponentModel;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Logging;
using Avalonia.Rendering;

#nullable enable

namespace Avalonia.Controls.Primitives
{
    public abstract class FlyoutBase : AvaloniaObject
    {
        static FlyoutBase()
        {
            Control.ContextFlyoutProperty.Changed.Subscribe(OnContextFlyoutPropertyChanged);
        }

        /// <summary>
        /// Defines the <see cref="IsOpen"/> property
        /// </summary>
        private static readonly DirectProperty<FlyoutBase, bool> IsOpenProperty =
           AvaloniaProperty.RegisterDirect<FlyoutBase, bool>(nameof(IsOpen),
               x => x.IsOpen);

        /// <summary>
        /// Defines the <see cref="Target"/> property
        /// </summary>
        public static readonly DirectProperty<FlyoutBase, Control?> TargetProperty =
            AvaloniaProperty.RegisterDirect<FlyoutBase, Control?>(nameof(Target), x => x.Target);

        /// <summary>
        /// Defines the <see cref="Placement"/> property
        /// </summary>
        public static readonly StyledProperty<FlyoutPlacementMode> PlacementProperty =
            AvaloniaProperty.Register<FlyoutBase, FlyoutPlacementMode>(nameof(Placement));

        /// <summary>
        /// Defines the <see cref="ShowMode"/> property
        /// </summary>
        public static readonly DirectProperty<FlyoutBase, FlyoutShowMode> ShowModeProperty =
            AvaloniaProperty.RegisterDirect<FlyoutBase, FlyoutShowMode>(nameof(ShowMode),
                x => x.ShowMode, (x, v) => x.ShowMode = v);

        /// <summary>
        /// Defines the AttachedFlyout property
        /// </summary>
        public static readonly AttachedProperty<FlyoutBase?> AttachedFlyoutProperty =
            AvaloniaProperty.RegisterAttached<FlyoutBase, Control, FlyoutBase?>("AttachedFlyout", null);

        private bool _isOpen;
        private Control? _target;
        private FlyoutShowMode _showMode = FlyoutShowMode.Standard;
        private Rect? _enlargedPopupRect;
        private PixelRect? _enlargePopupRectScreenPixelRect;
        private IDisposable? _transientDisposable;

        protected Popup? Popup { get; private set; }

        /// <summary>
        /// Gets whether this Flyout is currently Open
        /// </summary>
        public bool IsOpen
        {
            get => _isOpen;
            private set => SetAndRaise(IsOpenProperty, ref _isOpen, value);
        }

        /// <summary>
        /// Gets or sets the desired placement
        /// </summary>
        public FlyoutPlacementMode Placement
        {
            get => GetValue(PlacementProperty);
            set => SetValue(PlacementProperty, value);
        }

        /// <summary>
        /// Gets or sets the desired ShowMode
        /// </summary>
        public FlyoutShowMode ShowMode
        {
            get => _showMode;
            set => SetAndRaise(ShowModeProperty, ref _showMode, value);
        }

        /// <summary>
        /// Gets the Target used for showing the Flyout
        /// </summary>
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

        /// <summary>
        /// Shows the Flyout at the given Control
        /// </summary>
        /// <param name="placementTarget">The control to show the Flyout at</param>
        public void ShowAt(Control placementTarget)
        {
            ShowAtCore(placementTarget);
        }

        /// <summary>
        /// Shows the Flyout for the given control at the current pointer location, as in a ContextFlyout
        /// </summary>
        /// <param name="placementTarget">The target control</param>
        /// <param name="showAtPointer">True to show at pointer</param>
        public void ShowAt(Control placementTarget, bool showAtPointer)
        {
            ShowAtCore(placementTarget, showAtPointer);
        }

        /// <summary>
        /// Hides the Flyout
        /// </summary>
        public void Hide()
        {
            HideCore();
        }

        protected virtual void HideCore(bool canCancel = true)
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

            IsOpen = false;
            Popup.IsOpen = false;

            // Ensure this isn't active
            _transientDisposable?.Dispose();
            _transientDisposable = null;
            _enlargedPopupRect = null;
            _enlargePopupRectScreenPixelRect = null;

            OnClosed();
        }

        protected virtual void ShowAtCore(Control placementTarget, bool showAtPointer = false)
        {
            if (placementTarget == null)
                throw new ArgumentNullException("placementTarget cannot be null");

            if (Popup == null)
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
                    HideCore(false);
                }
            }

            if (Popup.Parent != null && Popup.Parent != placementTarget)
            {
                ((ISetLogicalParent)Popup).SetParent(null);
            }

            if (Popup.PlacementTarget != placementTarget)
            {
                Popup.PlacementTarget = Target = placementTarget;
                ((ISetLogicalParent)Popup).SetParent(placementTarget);
            }

            if (Popup.Child == null)
            {
                Popup.Child = CreatePresenter();
            }

            OnOpening();
            PositionPopup(showAtPointer);
            IsOpen = Popup.IsOpen = true;            
            OnOpened();
                        
            if (ShowMode == FlyoutShowMode.Standard)
            {
                // Try and focus content inside Flyout
                if (Popup.Child.Focusable)
                {
                    FocusManager.Instance?.Focus(Popup.Child);
                }
                else
                {
                    var nextFocus = KeyboardNavigationHandler.GetNext(Popup.Child, NavigationDirection.Next);
                    if (nextFocus != null)
                    {
                        FocusManager.Instance?.Focus(nextFocus);
                    }
                }
            }
            else if (ShowMode == FlyoutShowMode.TransientWithDismissOnPointerMoveAway)
            {
                _transientDisposable = InputManager.Instance?.Process.Subscribe(HandleTransientDismiss);
            }
        }

        private void HandleTransientDismiss(RawInputEventArgs args)
        {
            if (args is RawPointerEventArgs pArgs && pArgs.Type == RawPointerEventType.Move)
            {
                // In ShowMode = TransientWithDismissOnPointerMoveAway, the Flyout is kept
                // shown as long as the pointer is within a certain px distance from the
                // flyout itself. I'm not sure what WinUI uses, but I'm defaulting to 
                // 100px, which seems about right
                // enlargedPopupRect is the Flyout bounds enlarged 100px
                // For windowed popups, enlargedPopupRect is in screen coordinates,
                // for overlay popups, its in OverlayLayer coordinates

                if (_enlargedPopupRect == null && _enlargePopupRectScreenPixelRect == null)
                {
                    // Only do this once when the Flyout opens & cache the result
                    if (Popup?.Host is PopupRoot root)
                    { 
                        // Get the popup root bounds and convert to screen coordinates
                        
                        var tmp = root.Bounds.Inflate(100);
                        _enlargePopupRectScreenPixelRect = new PixelRect(root.PointToScreen(tmp.TopLeft), root.PointToScreen(tmp.BottomRight));
                    }
                    else if (Popup?.Host is OverlayPopupHost host)
                    {
                        // Overlay popups are in OverlayLayer coordinates, just use that
                        _enlargedPopupRect = host.Bounds.Inflate(100);
                    }

                    return;
                }

                if (Popup?.Host is PopupRoot)
                {
                    // As long as the pointer stays within the enlargedPopupRect
                    // the flyout stays open. If it leaves, close it
                    // Despite working in screen coordinates, leaving the TopLevel
                    // window will not close this (as pointer events stop), which 
                    // does match UWP
                    var pt = pArgs.Root.PointToScreen(pArgs.Position);
                    if (!_enlargePopupRectScreenPixelRect?.Contains(pt) ?? false)
                    {
                        HideCore(false);
                    }
                }
                else if (Popup?.Host is OverlayPopupHost)
                {
                    // Same as above here, but just different coordinate space
                    // so we don't need to translate
                    if (!_enlargedPopupRect?.Contains(pArgs.Position) ?? false)
                    {
                        HideCore(false);
                    }
                }
            }
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

        /// <summary>
        /// Used to create the content the Flyout displays
        /// </summary>
        /// <returns></returns>
        protected abstract Control CreatePresenter();

        private void InitPopup()
        {
            Popup = new Popup();
            Popup.WindowManagerAddShadowHint = false;
            Popup.IsLightDismissEnabled = true;

            Popup.Opened += OnPopupOpened;
            Popup.Closed += OnPopupClosed;
        }

        private void OnPopupOpened(object sender, EventArgs e)
        {
            IsOpen = true;
        }

        private void OnPopupClosed(object sender, EventArgs e)
        {
            HideCore();
        }

        private void PositionPopup(bool showAtPointer)
        {
            Size sz;
            if(Popup.Child.DesiredSize == Size.Empty)
            {
                // Popup may not have been shown yet. Measure content
                sz = LayoutHelper.MeasureChild(Popup.Child, Size.Infinity, new Thickness());
            }
            else
            {
                sz = Popup.Child.DesiredSize;
            }

            if (showAtPointer)
            {
                Popup.PlacementMode = PlacementMode.Pointer;
            }
            else
            {
                Popup.PlacementMode = PlacementMode.AnchorAndGravity;
                Popup.PlacementConstraintAdjustment =
                    PopupPositioning.PopupPositionerConstraintAdjustment.SlideX |
                    PopupPositioning.PopupPositionerConstraintAdjustment.SlideY;
            }

            var trgtBnds = Target?.Bounds ?? Rect.Empty;

            switch (Placement)
            {
                case FlyoutPlacementMode.Top: //Above & centered
                    Popup.PlacementRect = new Rect(0, 0, trgtBnds.Width-1, 1);
                    Popup.PlacementGravity = PopupPositioning.PopupGravity.Top;
                    Popup.PlacementAnchor = PopupPositioning.PopupAnchor.Top;
                    break;

                case FlyoutPlacementMode.TopEdgeAlignedLeft:
                    Popup.PlacementRect = new Rect(0, 0, 0, 0);
                    Popup.PlacementGravity = PopupPositioning.PopupGravity.TopRight;                    
                    break;

                case FlyoutPlacementMode.TopEdgeAlignedRight:
                    Popup.PlacementRect = new Rect(trgtBnds.Width - 1, 0, 10, 1);
                    Popup.PlacementGravity = PopupPositioning.PopupGravity.TopLeft;                    
                    break;

                case FlyoutPlacementMode.RightEdgeAlignedTop:
                    Popup.PlacementRect = new Rect(trgtBnds.Width - 1, 0, 1, 1);
                    Popup.PlacementGravity = PopupPositioning.PopupGravity.BottomRight;
                    Popup.PlacementAnchor = PopupPositioning.PopupAnchor.Right;
                    break;

                case FlyoutPlacementMode.Right: //Right & centered
                    Popup.PlacementRect = new Rect(trgtBnds.Width - 1, 0, 1, trgtBnds.Height);
                    Popup.PlacementGravity = PopupPositioning.PopupGravity.Right;
                    Popup.PlacementAnchor = PopupPositioning.PopupAnchor.Right;
                    break;

                case FlyoutPlacementMode.RightEdgeAlignedBottom:
                    Popup.PlacementRect = new Rect(trgtBnds.Width - 1, trgtBnds.Height - 1, 1, 1);
                    Popup.PlacementGravity = PopupPositioning.PopupGravity.TopRight;
                    Popup.PlacementAnchor = PopupPositioning.PopupAnchor.Right;
                    break;

                case FlyoutPlacementMode.Bottom: //Below & centered
                    Popup.PlacementRect = new Rect(0, trgtBnds.Height - 1, trgtBnds.Width, 1);
                    Popup.PlacementGravity = PopupPositioning.PopupGravity.Bottom;
                    Popup.PlacementAnchor = PopupPositioning.PopupAnchor.Bottom;
                    break;

                case FlyoutPlacementMode.BottomEdgeAlignedLeft:
                    Popup.PlacementRect = new Rect(0, trgtBnds.Height - 1, 1, 1);
                    Popup.PlacementGravity = PopupPositioning.PopupGravity.BottomRight;
                    Popup.PlacementAnchor = PopupPositioning.PopupAnchor.Bottom;
                    break;

                case FlyoutPlacementMode.BottomEdgeAlignedRight:
                    Popup.PlacementRect = new Rect(trgtBnds.Width - 1, trgtBnds.Height - 1, 1, 1);
                    Popup.PlacementGravity = PopupPositioning.PopupGravity.BottomLeft;
                    Popup.PlacementAnchor = PopupPositioning.PopupAnchor.Bottom;
                    break;

                case FlyoutPlacementMode.LeftEdgeAlignedTop:
                    Popup.PlacementRect = new Rect(0, 0, 1, 1);
                    Popup.PlacementGravity = PopupPositioning.PopupGravity.BottomLeft;
                    Popup.PlacementAnchor = PopupPositioning.PopupAnchor.Left;
                    break;

                case FlyoutPlacementMode.Left: //Left & centered
                    Popup.PlacementRect = new Rect(0, 0, 1, trgtBnds.Height);
                    Popup.PlacementGravity = PopupPositioning.PopupGravity.Left;
                    Popup.PlacementAnchor = PopupPositioning.PopupAnchor.Left;
                    break;

                case FlyoutPlacementMode.LeftEdgeAlignedBottom:
                    Popup.PlacementRect = new Rect(0, trgtBnds.Height - 1, 1, 1);
                    Popup.PlacementGravity = PopupPositioning.PopupGravity.TopLeft;
                    Popup.PlacementAnchor = PopupPositioning.PopupAnchor.BottomLeft;
                    break;

                //includes Auto (not sure what determines that)...
                default:
                    //This is just FlyoutPlacementMode.Top behavior (above & centered)
                    Popup.PlacementRect = new Rect(-sz.Width / 2, 0, sz.Width, 1);
                    Popup.PlacementGravity = PopupPositioning.PopupGravity.Top;
                    break;
            }
        }

        private static void OnContextFlyoutPropertyChanged(AvaloniaPropertyChangedEventArgs args)
        {
            if (args.Sender is Control c)
            {
                if (args.OldValue is FlyoutBase)
                {
                    c.PointerReleased -= OnControlWithContextFlyoutPointerReleased;
                }
                if (args.NewValue is FlyoutBase)
                {
                    c.PointerReleased += OnControlWithContextFlyoutPointerReleased;
                }
            }
        }

        private static void OnControlWithContextFlyoutPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (sender is Control c)
            {
                if (e.InitialPressMouseButton == MouseButton.Right &&
                e.GetCurrentPoint(c).Properties.PointerUpdateKind == PointerUpdateKind.RightButtonReleased)
                {
                    if (c.ContextFlyout != null)
                    {
                        if (c.ContextMenu != null)
                        {
                            Logger.TryGet(LogEventLevel.Verbose, "FlyoutBase")?.Log(c, "ContextMenu and ContextFlyout are both set, defaulting to ContextMenu");
                            return;
                        }
                        c.ContextFlyout.ShowAt(c, true);
                    }
                }
            }            
        }

        internal static void SetPresenterClasses(IControl presenter, Classes classes)
        {
            //Remove any classes no longer in use, ignoring pseudoclasses
            for (int i = presenter.Classes.Count - 1; i >= 0; i--)
            {
                if (!classes.Contains(presenter.Classes[i]) &&
                    !presenter.Classes[i].Contains(":"))
                {
                    presenter.Classes.RemoveAt(i);
                }
            }

            //Add new classes
            presenter.Classes.AddRange(classes);
        }
    }
}
