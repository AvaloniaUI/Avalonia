using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.Styling;
using Avalonia.Threading;
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
            RoutedEvent.Register<Control, RequestBringIntoViewEventArgs>(
                "RequestBringIntoView",
                RoutingStrategies.Bubble);

        /// <summary>
        /// Provides event data for the <see cref="ContextRequested"/> event.
        /// </summary>
        public static readonly RoutedEvent<ContextRequestedEventArgs> ContextRequestedEvent =
            RoutedEvent.Register<Control, ContextRequestedEventArgs>(
                nameof(ContextRequested),
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        
        /// <summary>
        /// Defines the <see cref="Loaded"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> LoadedEvent =
            RoutedEvent.Register<Control, RoutedEventArgs>(
                nameof(Loaded),
                RoutingStrategies.Direct);

        /// <summary>
        /// Defines the <see cref="Unloaded"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> UnloadedEvent =
            RoutedEvent.Register<Control, RoutedEventArgs>(
                nameof(Unloaded),
                RoutingStrategies.Direct);

        /// <summary>
        /// Defines the <see cref="FlowDirection"/> property.
        /// </summary>
        public static readonly AttachedProperty<FlowDirection> FlowDirectionProperty =
            AvaloniaProperty.RegisterAttached<Control, Control, FlowDirection>(
                nameof(FlowDirection),
                inherits: true);

        // Note the following:
        // _loadedQueue :
        //   Is the queue where any control will be added to indicate that its loaded
        //   event should be scheduled and called later.
        // _loadedProcessingQueue :
        //   Contains a copied snapshot of the _loadedQueue at the time when processing
        //   starts and individual events are being fired. This was needed to avoid
        //   exceptions if new controls were added in the Loaded event itself.

        private static bool _isLoadedProcessing = false;
        private static readonly HashSet<Control> _loadedQueue = new HashSet<Control>();
        private static readonly HashSet<Control> _loadedProcessingQueue = new HashSet<Control>();

        private bool _isLoaded = false;
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
        /// Gets a value indicating whether the control is fully constructed in the visual tree
        /// and both layout and render are complete.
        /// </summary>
        /// <remarks>
        /// This is set to true while raising the <see cref="Loaded"/> event.
        /// </remarks>
        public bool IsLoaded => _isLoaded;

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

        /// <summary>
        /// Occurs when the control has been fully constructed in the visual tree and both
        /// layout and render are complete.
        /// </summary>
        /// <remarks>
        /// This event is guaranteed to occur after the control template is applied and references
        /// to objects created after the template is applied are available. This makes it different
        /// from OnAttachedToVisualTree which doesn't have these references. This event occurs at the
        /// latest possible time in the control creation life-cycle.
        /// </remarks>
        public event EventHandler<RoutedEventArgs>? Loaded
        {
            add => AddHandler(LoadedEvent, value);
            remove => RemoveHandler(LoadedEvent, value);
        }

        /// <summary>
        /// Occurs when the control is removed from the visual tree.
        /// </summary>
        /// <remarks>
        /// This is API symmetrical with <see cref="Loaded"/> and exists for compatibility with other
        /// XAML frameworks; however, it behaves the same as OnDetachedFromVisualTree.
        /// </remarks>
        public event EventHandler<RoutedEventArgs>? Unloaded
        {
            add => AddHandler(UnloadedEvent, value);
            remove => RemoveHandler(UnloadedEvent, value);
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

        private static Action loadedProcessingAction = () =>
        {
            // Copy the loaded queue for processing
            // There was a possibility of the "Collection was modified; enumeration operation may not execute."
            // exception when only a single hash set was used. This could happen when new controls are added
            // within the Loaded callback/event itself. To fix this, two hash sets are used and while one is
            // being processed the other accepts adding new controls to process next.
            _loadedProcessingQueue.Clear();
            foreach (Control control in _loadedQueue)
            {
                _loadedProcessingQueue.Add(control);
            }
            _loadedQueue.Clear();

            foreach (Control control in _loadedProcessingQueue)
            {
                control.OnLoadedCore();
            }

            _loadedProcessingQueue.Clear();
            _isLoadedProcessing = false;

            // Restart if any controls were added to the queue while processing
            if (_loadedQueue.Count > 0)
            {
                _isLoadedProcessing = true;
                Dispatcher.UIThread.Post(loadedProcessingAction!, DispatcherPriority.Loaded);
            }
        };

        /// <summary>
        /// Schedules <see cref="OnLoadedCore"/> to be called for this control.
        /// For performance, it will be queued with other controls.
        /// </summary>
        internal void ScheduleOnLoadedCore()
        {
            if (_isLoaded == false)
            {
                bool isAdded = _loadedQueue.Add(this);

                if (isAdded &&
                    _isLoadedProcessing == false)
                {
                    _isLoadedProcessing = true;
                    Dispatcher.UIThread.Post(loadedProcessingAction!, DispatcherPriority.Loaded);
                }
            }
        }

        /// <summary>
        /// Invoked as the first step of marking the control as loaded and raising the
        /// <see cref="Loaded"/> event.
        /// </summary>
        internal void OnLoadedCore()
        {
            if (_isLoaded == false &&
                ((ILogical)this).IsAttachedToLogicalTree)
            {
                _isLoaded = true;
                OnLoaded();
            }
        }

        /// <summary>
        /// Invoked as the first step of marking the control as unloaded and raising the
        /// <see cref="Unloaded"/> event.
        /// </summary>
        internal void OnUnloadedCore()
        {
            if (_isLoaded)
            {
                // Remove from the loaded event queue here as a failsafe in case the control
                // is detached before the dispatcher runs the Loaded jobs.
                _loadedQueue.Remove(this);

                _isLoaded = false;
                OnUnloaded();
            }
        }

        /// <summary>
        /// Invoked just before the <see cref="Loaded"/> event.
        /// </summary>
        protected virtual void OnLoaded()
        {
            var eventArgs = new RoutedEventArgs(LoadedEvent);
            eventArgs.Source = null;
            RaiseEvent(eventArgs);
        }

        /// <summary>
        /// Invoked just before the <see cref="Unloaded"/> event.
        /// </summary>
        protected virtual void OnUnloaded()
        {
            var eventArgs = new RoutedEventArgs(UnloadedEvent);
            eventArgs.Source = null;
            RaiseEvent(eventArgs);
        }

        /// <inheritdoc/>
        protected sealed override void OnAttachedToVisualTreeCore(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTreeCore(e);

            InitializeIfNeeded();

            ScheduleOnLoadedCore();
        }

        /// <inheritdoc/>
        protected sealed override void OnDetachedFromVisualTreeCore(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTreeCore(e);

            OnUnloadedCore();
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
                {
                    return;
                }

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

            var parent = ((IVisual)this).VisualParent as Control;
            if (parent != null)
            {
                parentFlowDirection = parent.FlowDirection;
                parentBypassFlowDirectionPolicies = parent.BypassFlowDirectionPolicies;
            }

            bool thisShouldBeMirrored = flowDirection == FlowDirection.RightToLeft && !bypassFlowDirectionPolicies;
            bool parentShouldBeMirrored = parentFlowDirection == FlowDirection.RightToLeft && !parentBypassFlowDirectionPolicies;

            bool shouldApplyMirrorTransform = thisShouldBeMirrored != parentShouldBeMirrored;

            HasMirrorTransform = shouldApplyMirrorTransform;
        }
    }
}
