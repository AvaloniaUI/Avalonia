using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Automation.Peers;
using System.Linq;
using Avalonia.Controls.Diagnostics;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Automation;
using Avalonia.Reactive;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control context menu.
    /// </summary>
    public class ContextMenu : MenuBase, ISetterValue, IPopupHostProvider
    {
        /// <summary>
        /// Defines the <see cref="HorizontalOffset"/> property.
        /// </summary>
        public static readonly StyledProperty<double> HorizontalOffsetProperty =
            Popup.HorizontalOffsetProperty.AddOwner<ContextMenu>();

        /// <summary>
        /// Defines the <see cref="VerticalOffset"/> property.
        /// </summary>
        public static readonly StyledProperty<double> VerticalOffsetProperty =
            Popup.VerticalOffsetProperty.AddOwner<ContextMenu>();

        /// <summary>
        /// Defines the <see cref="PlacementAnchor"/> property.
        /// </summary>
        public static readonly StyledProperty<PopupAnchor> PlacementAnchorProperty =
            Popup.PlacementAnchorProperty.AddOwner<ContextMenu>();

        /// <summary>
        /// Defines the <see cref="PlacementConstraintAdjustment"/> property.
        /// </summary>
        public static readonly StyledProperty<PopupPositionerConstraintAdjustment> PlacementConstraintAdjustmentProperty =
            Popup.PlacementConstraintAdjustmentProperty.AddOwner<ContextMenu>();

        /// <summary>
        /// Defines the <see cref="PlacementGravity"/> property.
        /// </summary>
        public static readonly StyledProperty<PopupGravity> PlacementGravityProperty =
            Popup.PlacementGravityProperty.AddOwner<ContextMenu>();

        /// <summary>
        /// Defines the <see cref="Placement"/> property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1013",
            Justification = "We keep PlacementModeProperty for backward compatibility.")]
        public static readonly StyledProperty<PlacementMode> PlacementProperty =
            Popup.PlacementProperty.AddOwner<ContextMenu>();

        /// <summary>
        /// Defines the <see cref="PlacementMode"/> property.
        /// </summary>
        [Obsolete("Use the Placement property instead."), EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly StyledProperty<PlacementMode> PlacementModeProperty = PlacementProperty;

        /// <summary>
        /// Defines the <see cref="PlacementRect"/> property.
        /// </summary>
        public static readonly StyledProperty<Rect?> PlacementRectProperty =
            Popup.PlacementRectProperty.AddOwner<ContextMenu>();

        /// <summary>
        /// Defines the <see cref="WindowManagerAddShadowHint"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> WindowManagerAddShadowHintProperty  =
            Popup.WindowManagerAddShadowHintProperty.AddOwner<ContextMenu>();

        /// <summary>
        /// Defines the <see cref="PlacementTarget"/> property.
        /// </summary>
        public static readonly StyledProperty<Control?> PlacementTargetProperty =
            Popup.PlacementTargetProperty.AddOwner<ContextMenu>();

        private Popup? _popup;
        private List<Control>? _attachedControls;
        private IInputElement? _previousFocus;
        private Action<IPopupHost?>? _popupHostChangedHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextMenu"/> class.
        /// </summary>
        public ContextMenu()
            : this(new DefaultMenuInteractionHandler(true))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextMenu"/> class.
        /// </summary>
        /// <param name="interactionHandler">The menu interaction handler.</param>
        public ContextMenu(IMenuInteractionHandler interactionHandler)
            : base(interactionHandler)
        {
        }

        /// <summary>
        /// Initializes static members of the <see cref="ContextMenu"/> class.
        /// </summary>
        static ContextMenu()
        {
            PlacementProperty.OverrideDefaultValue<ContextMenu>(PlacementMode.Pointer);
            ContextMenuProperty.Changed.Subscribe(ContextMenuChanged);
            AutomationProperties.AccessibilityViewProperty.OverrideDefaultValue<ContextMenu>(AccessibilityView.Control);
            AutomationProperties.ControlTypeOverrideProperty.OverrideDefaultValue<ContextMenu>(AutomationControlType.Menu);
        }

        /// <inheritdoc cref="Popup.HorizontalOffset"/>
        public double HorizontalOffset
        {
            get => GetValue(HorizontalOffsetProperty);
            set => SetValue(HorizontalOffsetProperty, value);
        }

        /// <inheritdoc cref="Popup.VerticalOffset"/>
        public double VerticalOffset
        {
            get => GetValue(VerticalOffsetProperty);
            set => SetValue(VerticalOffsetProperty, value);
        }

        /// <inheritdoc cref="Popup.PlacementAnchor"/>
        public PopupAnchor PlacementAnchor
        {
            get => GetValue(PlacementAnchorProperty);
            set => SetValue(PlacementAnchorProperty, value);
        }

        /// <inheritdoc cref="Popup.PlacementConstraintAdjustment"/>
        public PopupPositionerConstraintAdjustment PlacementConstraintAdjustment
        {
            get => GetValue(PlacementConstraintAdjustmentProperty);
            set => SetValue(PlacementConstraintAdjustmentProperty, value);
        }

        /// <inheritdoc cref="Popup.PlacementGravity"/>
        public PopupGravity PlacementGravity
        {
            get => GetValue(PlacementGravityProperty);
            set => SetValue(PlacementGravityProperty, value);
        }

        /// <inheritdoc cref="Placement"/>
        [Obsolete("Use the Placement property instead."), EditorBrowsable(EditorBrowsableState.Never)]
        public PlacementMode PlacementMode
        {
            get => GetValue(PlacementProperty);
            set => SetValue(PlacementProperty, value);
        }

        /// <inheritdoc cref="Popup.Placement"/>
        public PlacementMode Placement
        {
            get => GetValue(PlacementProperty);
            set => SetValue(PlacementProperty, value);
        }

        public bool WindowManagerAddShadowHint
        {
            get => GetValue(WindowManagerAddShadowHintProperty);
            set => SetValue(WindowManagerAddShadowHintProperty, value);
        }

        /// <inheritdoc cref="Popup.PlacementRect"/>
        public Rect? PlacementRect
        {
            get => GetValue(PlacementRectProperty);
            set => SetValue(PlacementRectProperty, value);
        }

        /// <inheritdoc cref="Popup.PlacementTarget"/>
        public Control? PlacementTarget
        {
            get => GetValue(PlacementTargetProperty);
            set => SetValue(PlacementTargetProperty, value);
        }

        /// <summary>
        /// Occurs when the value of the
        /// <see cref="P:Avalonia.Controls.ContextMenu.IsOpen" />
        /// property is changing from false to true.
        /// </summary>
        public event CancelEventHandler? Opening;

        /// <summary>
        /// Occurs when the value of the
        /// <see cref="P:Avalonia.Controls.ContextMenu.IsOpen" />
        /// property is changing from true to false.
        /// </summary>
        public event CancelEventHandler? Closing;

        /// <summary>
        /// Called when the <see cref="Control.ContextMenu"/> property changes on a control.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void ContextMenuChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var control = (Control)e.Sender;

            if (e.OldValue is ContextMenu oldMenu)
            {
                control.ContextRequested -= ControlContextRequested;
                control.AttachedToVisualTree -= ControlOnAttachedToVisualTree;
                control.DetachedFromVisualTree -= ControlDetachedFromVisualTree;
                oldMenu._attachedControls?.Remove(control);
                ((ISetLogicalParent?)oldMenu._popup)?.SetParent(null);
            }

            if (e.NewValue is ContextMenu)
            {
                control.ContextRequested += ControlContextRequested;
                control.AttachedToVisualTree += ControlOnAttachedToVisualTree;
                control.DetachedFromVisualTree += ControlDetachedFromVisualTree;
            }
            
            if (control.IsAttachedToVisualTree)
            {
                AttachControlToContextMenu(control); 
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == WindowManagerAddShadowHintProperty && _popup != null)
            {
                _popup.WindowManagerAddShadowHint = change.GetNewValue<bool>();
            }
        }

        /// <summary>
        /// Opens the menu.
        /// </summary>
        public override void Open() => Open(null);

        /// <summary>
        /// Opens a context menu on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        public void Open(Control? control)
        {
            if (control is null && (_attachedControls is null || _attachedControls.Count == 0))
            {
                throw new ArgumentNullException(nameof(control));
            }

            if (control is object &&
                _attachedControls is object &&
                !_attachedControls.Contains(control))
            {
                throw new ArgumentException(
                    "Cannot show ContentMenu on a different control to the one it is attached to.",
                    nameof(control));
            }

            control ??= _attachedControls![0];
            Open(control, PlacementTarget ?? control, Placement);
        }

        /// <summary>
        /// Closes the menu.
        /// </summary>
        public override void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            if (_popup != null && _popup.IsVisible)
            {
                _popup.IsOpen = false;
            }
        }

        void ISetterValue.Initialize(SetterBase setter)
        {
            // ContextMenu can be assigned to the ContextMenu property in a setter. This overrides
            // the behavior defined in Control which requires controls to be wrapped in a <template>.
            if (!(setter is Setter s && s.Property == ContextMenuProperty))
            {
                throw new InvalidOperationException(
                    "Cannot use a control as a Setter value. Wrap the control in a <Template>.");
            }
        }

        IPopupHost? IPopupHostProvider.PopupHost => _popup?.Host;

        event Action<IPopupHost?>? IPopupHostProvider.PopupHostChanged 
        { 
            add => _popupHostChangedHandler += value; 
            remove => _popupHostChangedHandler -= value;
        }

        private void Open(Control control, Control placementTarget, PlacementMode placement)
        {
            if (IsOpen)
            {
                return;
            }

            if (_popup == null)
            {
                _popup = new Popup
                {
                    IsLightDismissEnabled = true,
                    OverlayDismissEventPassThrough = true,
                };

                _popup.Opened += PopupOpened;
                _popup.Closed += PopupClosed;
                _popup.Closing += PopupClosing;
                _popup.KeyUp += PopupKeyUp;
            }

            if (_popup.Parent != control)
            {
                ((ISetLogicalParent)_popup).SetParent(null);
                ((ISetLogicalParent)_popup).SetParent(control);
            }

            _popup.Placement = placement;

            //Position of the line below is really important. 
            //All styles are being applied only when control has logical parent.
            //Line below will add ContextMenu as child to the Popup and this will trigger styles and they would be applied.
            //If you will move line below somewhere else it may cause that ContextMenu will behave differently from what you are expecting.
            _popup.Child = this;
            _popup.PlacementTarget = placementTarget;
            _popup.HorizontalOffset = HorizontalOffset;
            _popup.VerticalOffset = VerticalOffset;
            _popup.PlacementAnchor = PlacementAnchor;
            _popup.PlacementConstraintAdjustment = PlacementConstraintAdjustment;
            _popup.PlacementGravity = PlacementGravity;
            _popup.PlacementRect = PlacementRect;
            _popup.WindowManagerAddShadowHint = WindowManagerAddShadowHint;
            IsOpen = true;
            _popup.IsOpen = true;

            RaiseEvent(new RoutedEventArgs
            {
                RoutedEvent = OpenedEvent,
                Source = this,
            });
        }

        private void PopupOpened(object? sender, EventArgs e)
        {
            _previousFocus = FocusManager.GetFocusManager(this)?.GetFocusedElement();
            Focus();

            _popupHostChangedHandler?.Invoke(_popup!.Host);
        }

        private void PopupClosing(object? sender, CancelEventArgs e)
        {
            e.Cancel = CancelClosing();
        }

        private void PopupClosed(object? sender, EventArgs e)
        {
            foreach (var i in LogicalChildren)
            {
                if (i is MenuItem menuItem)
                {
                    menuItem.IsSubMenuOpen = false;
                }
            }

            SelectedIndex = -1;
            IsOpen = false;

            if (_attachedControls is null || _attachedControls.Count == 0)
            {
                ((ISetLogicalParent)_popup!).SetParent(null);
            }

            RaiseEvent(new RoutedEventArgs
            {
                RoutedEvent = ClosedEvent,
                Source = this,
            });
            
            _popupHostChangedHandler?.Invoke(null);
        }

        private void PopupKeyUp(object? sender, KeyEventArgs e)
        {
            if (IsOpen)
            {
                var keymap = Application.Current!.PlatformSettings!.HotkeyConfiguration;

                if (keymap?.OpenContextMenu.Any(k => k.Matches(e)) == true
                    && !CancelClosing())
                {
                    Close();
                    e.Handled = true;
                }
            }
        }

        private static void ControlContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            if (sender is Control control
                && control.ContextMenu is ContextMenu contextMenu
                && !e.Handled
                && !contextMenu.CancelOpening())
            {
                var requestedByPointer = e.TryGetPosition(null, out _);
                contextMenu.Open(
                    control, 
                    e.Source as Control ?? control, 
                    requestedByPointer ? contextMenu.Placement : PlacementMode.Bottom);
                e.Handled = true;
            }
        }
        
        
        private static void ControlOnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            AttachControlToContextMenu(sender);
        }

        private static void AttachControlToContextMenu(object? sender)
        {
            if (sender is Control { ContextMenu: { } contextMenu } control)
            {
                contextMenu._attachedControls ??= new List<Control>();
                contextMenu._attachedControls.Add(control);
            }
        }

        private static void ControlDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (sender is Control { ContextMenu: { } contextMenu } control)
            {
                if (contextMenu._popup?.Parent == control)
                {
                    ((ISetLogicalParent)contextMenu._popup).SetParent(null);
                }

                contextMenu.Close();
                contextMenu._attachedControls?.Remove(control);
            }
        }

        private bool CancelClosing()
        {
            var eventArgs = new CancelEventArgs();
            Closing?.Invoke(this, eventArgs);
            return eventArgs.Cancel;
        }

        private bool CancelOpening()
        {
            var eventArgs = new CancelEventArgs();
            Opening?.Invoke(this, eventArgs);
            return eventArgs.Cancel;
        }
    }
}
