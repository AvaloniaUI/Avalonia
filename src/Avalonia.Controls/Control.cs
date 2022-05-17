using System;
using System.ComponentModel;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// Base class for Avalonia controls.
    /// </summary>
    /// <remarks>
    /// The control class extends <see cref="InputElement"/> and adds the following features:
    ///
    /// - A <see cref="Tag"/> property to allow user-defined data to be attached to the control.
    /// - <see cref="ContextRequestedEvent"/> and other context menu related members.
    /// </remarks>
    public class Control : InputElement, IControl, INamed, IVisualBrushInitialize, ISetterValue
    {
        /// <summary>
        /// Defines the <see cref="FocusAdorner"/> property.
        /// </summary>
        public static readonly StyledProperty<ITemplate<IControl>?> FocusAdornerProperty =
            AvaloniaProperty.Register<Control, ITemplate<IControl>?>(nameof(FocusAdorner));

        /// <summary>
        /// Defines the <see cref="Tag"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> TagProperty =
            AvaloniaProperty.Register<Control, object?>(nameof(Tag));
        
        /// <summary>
        /// Defines the <see cref="ContextMenu"/> property.
        /// </summary>
        public static readonly StyledProperty<ContextMenu?> ContextMenuProperty =
            AvaloniaProperty.Register<Control, ContextMenu?>(nameof(ContextMenu));

        /// <summary>
        /// Defines the <see cref="ContextFlyout"/> property
        /// </summary>
        public static readonly StyledProperty<FlyoutBase?> ContextFlyoutProperty =
            AvaloniaProperty.Register<Control, FlyoutBase?>(nameof(ContextFlyout));

        /// <summary>
        /// Event raised when an element wishes to be scrolled into view.
        /// </summary>
        public static readonly RoutedEvent<RequestBringIntoViewEventArgs> RequestBringIntoViewEvent =
            RoutedEvent.Register<Control, RequestBringIntoViewEventArgs>("RequestBringIntoView", RoutingStrategies.Bubble);

        /// <summary>
        /// Provides event data for the <see cref="ContextRequested"/> event.
        /// </summary>
        public static readonly RoutedEvent<ContextRequestedEventArgs> ContextRequestedEvent =
            RoutedEvent.Register<Control, ContextRequestedEventArgs>(nameof(ContextRequested),
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        
        /// <summary>
        /// Defines the <see cref="FlowDirection"/> property.
        /// </summary>
        public static readonly AttachedProperty<FlowDirection> FlowDirectionProperty =
            AvaloniaProperty.RegisterAttached<Control, Control, FlowDirection>(nameof(FlowDirection), inherits: true);

        private DataTemplates? _dataTemplates;
        private IControl? _focusAdorner;
        private AutomationPeer? _automationPeer;

        /// <summary>
        /// Gets or sets the control's focus adorner.
        /// </summary>
        public ITemplate<IControl>? FocusAdorner
        {
            get => GetValue(FocusAdornerProperty);
            set => SetValue(FocusAdornerProperty, value);
        }

        /// <summary>
        /// Gets or sets the data templates for the control.
        /// </summary>
        /// <remarks>
        /// Each control may define data templates which are applied to the control itself and its
        /// children.
        /// </remarks>
        public DataTemplates DataTemplates => _dataTemplates ??= new DataTemplates();

        /// <summary>
        /// Gets or sets a context menu to the control.
        /// </summary>
        public ContextMenu? ContextMenu
        {
            get => GetValue(ContextMenuProperty);
            set => SetValue(ContextMenuProperty, value);
        }

        /// <summary>
        /// Gets or sets a context flyout to the control
        /// </summary>
        public FlyoutBase? ContextFlyout
        {
            get => GetValue(ContextFlyoutProperty);
            set => SetValue(ContextFlyoutProperty, value);
        }

        /// <summary>
        /// Gets or sets a user-defined object attached to the control.
        /// </summary>
        public object? Tag
        {
            get => GetValue(TagProperty);
            set => SetValue(TagProperty, value);
        }
        
        /// <summary>
        /// Gets or sets the text flow direction.
        /// </summary>
        public FlowDirection FlowDirection
        {
            get => GetValue(FlowDirectionProperty);
            set => SetValue(FlowDirectionProperty, value);
        }

        /// <summary>
        /// Occurs when the user has completed a context input gesture, such as a right-click.
        /// </summary>
        public event EventHandler<ContextRequestedEventArgs>? ContextRequested
        {
            add => AddHandler(ContextRequestedEvent, value);
            remove => RemoveHandler(ContextRequestedEvent, value);
        }

        public new IControl? Parent => (IControl?)base.Parent;

        /// <summary>
        /// Gets the value of the attached <see cref="FlowDirectionProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The flow direction.</returns>
        public static FlowDirection GetFlowDirection(Control control)
        {
            return control.GetValue(FlowDirectionProperty);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FlowDirectionProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetFlowDirection(Control control, FlowDirection value)
        {
            control.SetValue(FlowDirectionProperty, value);
        }

        /// <inheritdoc/>
        bool IDataTemplateHost.IsDataTemplatesInitialized => _dataTemplates != null;

        /// <summary>
        /// Gets a value indicating whether control bypass FlowDirecton policies.
        /// </summary>
        /// <remarks>
        /// Related to FlowDirection system and returns false as default, so if 
        /// <see cref="FlowDirection"/> is RTL then control will get a mirror presentation. 
        /// For controls that want to avoid this behavior, override this property and return true.
        /// </remarks>
        protected virtual bool BypassFlowDirectionPolicies => false;

        /// <inheritdoc/>
        void ISetterValue.Initialize(ISetter setter)
        {
            if (setter is Setter s && s.Property == ContextFlyoutProperty)
            {
                return; // Allow ContextFlyout to not need wrapping in <Template>
            }

            throw new InvalidOperationException(
                "Cannot use a control as a Setter value. Wrap the control in a <Template>.");
        }

        /// <inheritdoc/>
        void IVisualBrushInitialize.EnsureInitialized()
        {
            if (VisualRoot == null)
            {
                if (!IsInitialized)
                {
                    foreach (var i in this.GetSelfAndVisualDescendants())
                    {
                        var c = i as IControl;

                        if (c?.IsInitialized == false && c is ISupportInitialize init)
                        {
                            init.BeginInit();
                            init.EndInit();
                        }
                    }
                }

                if (!IsArrangeValid)
                {
                    Measure(Size.Infinity);
                    Arrange(new Rect(DesiredSize));
                }
            }
        }

        /// <summary>
        /// Gets the element that receives the focus adorner.
        /// </summary>
        /// <returns>The control that receives the focus adorner.</returns>
        protected virtual IControl? GetTemplateFocusTarget() => this;

        /// <inheritdoc/>
        protected sealed override void OnAttachedToVisualTreeCore(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTreeCore(e);

            InitializeIfNeeded();
        }

        /// <inheritdoc/>
        protected sealed override void OnDetachedFromVisualTreeCore(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTreeCore(e);
        }

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            InvalidateMirrorTransform();
        }

        /// <inheritdoc/>
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);

            if (IsFocused &&
                (e.NavigationMethod == NavigationMethod.Tab ||
                 e.NavigationMethod == NavigationMethod.Directional))
            {
                var adornerLayer = AdornerLayer.GetAdornerLayer(this);

                if (adornerLayer != null)
                {
                    if (_focusAdorner == null)
                    {
                        var template = GetValue(FocusAdornerProperty);

                        if (template != null)
                        {
                            _focusAdorner = template.Build();
                        }
                    }

                    if (_focusAdorner != null && GetTemplateFocusTarget() is Visual target)
                    {
                        AdornerLayer.SetAdornedElement((Visual)_focusAdorner, target);
                        adornerLayer.Children.Add(_focusAdorner);
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            if (_focusAdorner?.Parent != null)
            {
                var adornerLayer = (IPanel)_focusAdorner.Parent;
                adornerLayer.Children.Remove(_focusAdorner);
                _focusAdorner = null;
            }
        }

        protected virtual AutomationPeer OnCreateAutomationPeer()
        {
            return new NoneAutomationPeer(this);
        }

        internal AutomationPeer GetOrCreateAutomationPeer()
        {
            VerifyAccess();

            if (_automationPeer is object)
            {
                return _automationPeer;
            }

            _automationPeer = OnCreateAutomationPeer();
            return _automationPeer;
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (e.Source == this
                && !e.Handled
                && e.InitialPressMouseButton == MouseButton.Right)
            {
                var args = new ContextRequestedEventArgs(e);
                RaiseEvent(args);
                e.Handled = args.Handled;
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (e.Source == this
                && !e.Handled)
            {
                var keymap = AvaloniaLocator.Current.GetService<PlatformHotkeyConfiguration>()?.OpenContextMenu;

                if (keymap is null)
                    return;

                var matches = false;

                for (var index = 0; index < keymap.Count; index++)
                {
                    var key = keymap[index];
                    matches |= key.Matches(e);

                    if (matches)
                    {
                        break;
                    }
                }

                if (matches)
                {
                    var args = new ContextRequestedEventArgs();
                    RaiseEvent(args);
                    e.Handled = args.Handled;
                }
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == FlowDirectionProperty)
            {
                InvalidateMirrorTransform();
                
                foreach (var visual in VisualChildren)
                {
                    if (visual is Control child)
                    {
                        child.InvalidateMirrorTransform();
                    }
                }
            }
        }

        /// <summary>
        /// Computes the <see cref="IVisual.HasMirrorTransform"/> value according to the 
        /// <see cref="FlowDirection"/> and <see cref="BypassFlowDirectionPolicies"/>
        /// </summary>
        public virtual void InvalidateMirrorTransform()
        {
            var flowDirection = this.FlowDirection;
            var parentFlowDirection = FlowDirection.LeftToRight;

            bool bypassFlowDirectionPolicies = BypassFlowDirectionPolicies;
            bool parentBypassFlowDirectionPolicies = false;

            var parent = this.FindAncestorOfType<Control>();
            if (parent != null)
            {
                parentFlowDirection = parent.FlowDirection;
                parentBypassFlowDirectionPolicies = parent.BypassFlowDirectionPolicies;
            }
            else if (Parent is Control logicalParent)
            {
                parentFlowDirection = logicalParent.FlowDirection;
                parentBypassFlowDirectionPolicies = logicalParent.BypassFlowDirectionPolicies;
            }

            bool thisShouldBeMirrored = flowDirection == FlowDirection.RightToLeft && !bypassFlowDirectionPolicies;
            bool parentShouldBeMirrored = parentFlowDirection == FlowDirection.RightToLeft && !parentBypassFlowDirectionPolicies;

            bool shouldApplyMirrorTransform = thisShouldBeMirrored != parentShouldBeMirrored;

            HasMirrorTransform = shouldApplyMirrorTransform;
        }
    }
}
