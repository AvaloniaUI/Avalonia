// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Displays a popup window.
    /// </summary>
    public class Popup : Control, IVisualTreeHost
    {
        /// <summary>
        /// Defines the <see cref="Child"/> property.
        /// </summary>
        public static readonly StyledProperty<Control> ChildProperty =
            AvaloniaProperty.Register<Popup, Control>(nameof(Child));

        /// <summary>
        /// Defines the <see cref="IsOpen"/> property.
        /// </summary>
        public static readonly DirectProperty<Popup, bool> IsOpenProperty =
            AvaloniaProperty.RegisterDirect<Popup, bool>(
                nameof(IsOpen),
                o => o.IsOpen,
                (o, v) => o.IsOpen = v);

        /// <summary>
        /// Defines the <see cref="PlacementMode"/> property.
        /// </summary>
        public static readonly StyledProperty<PlacementMode> PlacementModeProperty =
            AvaloniaProperty.Register<Popup, PlacementMode>(nameof(PlacementMode), defaultValue: PlacementMode.Bottom);

        /// <summary>
        /// Defines the <see cref="ObeyScreenEdges"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> ObeyScreenEdgesProperty =
            AvaloniaProperty.Register<Popup, bool>(nameof(ObeyScreenEdges), true);

        /// <summary>
        /// Defines the <see cref="HorizontalOffset"/> property.
        /// </summary>
        public static readonly StyledProperty<double> HorizontalOffsetProperty =
            AvaloniaProperty.Register<Popup, double>(nameof(HorizontalOffset));

        /// <summary>
        /// Defines the <see cref="VerticalOffset"/> property.
        /// </summary>
        public static readonly StyledProperty<double> VerticalOffsetProperty =
            AvaloniaProperty.Register<Popup, double>(nameof(VerticalOffset));

        /// <summary>
        /// Defines the <see cref="PlacementTarget"/> property.
        /// </summary>
        public static readonly StyledProperty<Control> PlacementTargetProperty =
            AvaloniaProperty.Register<Popup, Control>(nameof(PlacementTarget));

        /// <summary>
        /// Defines the <see cref="StaysOpen"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> StaysOpenProperty =
            AvaloniaProperty.Register<Popup, bool>(nameof(StaysOpen), true);

        /// <summary>
        /// Defines the <see cref="Topmost"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> TopmostProperty =
            AvaloniaProperty.Register<Popup, bool>(nameof(Topmost));

        private bool _isOpen;
        private IPopupHost _popupHost;
        private TopLevel _topLevel;
        private IDisposable _nonClientListener;
        private IDisposable _presenterSubscription;
        bool _ignoreIsOpenChanged = false;
        private List<IDisposable> _bindings = new List<IDisposable>();

        /// <summary>
        /// Initializes static members of the <see cref="Popup"/> class.
        /// </summary>
        static Popup()
        {
            IsHitTestVisibleProperty.OverrideDefaultValue<Popup>(false);
            ChildProperty.Changed.AddClassHandler<Popup>((x, e) => x.ChildChanged(e));
            IsOpenProperty.Changed.AddClassHandler<Popup>((x, e) => x.IsOpenChanged(e));
        }

        public Popup()
        {
            
        }

        /// <summary>
        /// Raised when the popup closes.
        /// </summary>
        public event EventHandler Closed;

        /// <summary>
        /// Raised when the popup opens.
        /// </summary>
        public event EventHandler Opened;

        public IPopupHost Host => _popupHost;

        /// <summary>
        /// Gets or sets the control to display in the popup.
        /// </summary>
        [Content]
        public Control Child
        {
            get { return GetValue(ChildProperty); }
            set { SetValue(ChildProperty, value); }
        }

        /// <summary>
        /// Gets or sets a dependency resolver for the <see cref="PopupRoot"/>.
        /// </summary>
        /// <remarks>
        /// This property allows a client to customize the behaviour of the popup by injecting
        /// a specialized dependency resolver into the <see cref="PopupRoot"/>'s constructor.
        /// </remarks>
        public IAvaloniaDependencyResolver DependencyResolver
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the popup is currently open.
        /// </summary>
        public bool IsOpen
        {
            get { return _isOpen; }
            set { SetAndRaise(IsOpenProperty, ref _isOpen, value); }
        }

        /// <summary>
        /// Gets or sets the placement mode of the popup in relation to the <see cref="PlacementTarget"/>.
        /// </summary>
        public PlacementMode PlacementMode
        {
            get { return GetValue(PlacementModeProperty); }
            set { SetValue(PlacementModeProperty, value); }
        }

        [Obsolete("This property has no effect")]
        public bool ObeyScreenEdges
        {
            get => GetValue(ObeyScreenEdgesProperty);
            set => SetValue(ObeyScreenEdgesProperty, value);
        }

        /// <summary>
        /// Gets or sets the Horizontal offset of the popup in relation to the <see cref="PlacementTarget"/>
        /// </summary>
        public double HorizontalOffset
        {
            get { return GetValue(HorizontalOffsetProperty); }
            set { SetValue(HorizontalOffsetProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Vertical offset of the popup in relation to the <see cref="PlacementTarget"/>
        /// </summary>
        public double VerticalOffset
        {
            get { return GetValue(VerticalOffsetProperty); }
            set { SetValue(VerticalOffsetProperty, value); }
        }

        /// <summary>
        /// Gets or sets the control that is used to determine the popup's position.
        /// </summary>
        public Control PlacementTarget
        {
            get { return GetValue(PlacementTargetProperty); }
            set { SetValue(PlacementTargetProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the popup should stay open when the popup is
        /// pressed or loses focus.
        /// </summary>
        public bool StaysOpen
        {
            get { return GetValue(StaysOpenProperty); }
            set { SetValue(StaysOpenProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether this popup appears on top of all other windows
        /// </summary>
        public bool Topmost
        {
            get { return GetValue(TopmostProperty); }
            set { SetValue(TopmostProperty, value); }
        }

        /// <summary>
        /// Gets the root of the popup window.
        /// </summary>
        IVisual IVisualTreeHost.Root => _popupHost?.HostedVisualTreeRoot;

        /// <summary>
        /// Opens the popup.
        /// </summary>
        public void Open()
        {
            // Popup is currently open
            if (_topLevel != null)
                return;
            CloseCurrent();
            var placementTarget = PlacementTarget ?? this.GetLogicalAncestors().OfType<IVisual>().FirstOrDefault();
            if (placementTarget == null)
                throw new InvalidOperationException("Popup has no logical parent and PlacementTarget is null");
            
            _topLevel = placementTarget.GetVisualRoot() as TopLevel;

            if (_topLevel == null)
            {
                throw new InvalidOperationException(
                    "Attempted to open a popup not attached to a TopLevel");
            }

            _popupHost = OverlayPopupHost.CreatePopupHost(placementTarget, DependencyResolver);

            _bindings.Add(_popupHost.BindConstraints(this, WidthProperty, MinWidthProperty, MaxWidthProperty,
                HeightProperty, MinHeightProperty, MaxHeightProperty, TopmostProperty));

            _popupHost.SetChild(Child);
            ((ISetLogicalParent)_popupHost).SetParent(this);
            _popupHost.ConfigurePosition(placementTarget,
                PlacementMode, new Point(HorizontalOffset, VerticalOffset));
            _popupHost.TemplateApplied += RootTemplateApplied;
            
            var window = _topLevel as Window;
            if (window != null)
            {
                window.Deactivated += WindowDeactivated;
            }
            else
            {
                var parentPopuproot = _topLevel as PopupRoot;
                if (parentPopuproot?.Parent is Popup popup)
                {
                    popup.Closed += ParentClosed;
                }
            }
            _topLevel.AddHandler(PointerPressedEvent, PointerPressedOutside, RoutingStrategies.Tunnel);
            _nonClientListener = InputManager.Instance?.Process.Subscribe(ListenForNonClientClick);
        

            _popupHost.Show();

            using (BeginIgnoringIsOpen())
            {
                IsOpen = true;
            }

            Opened?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Closes the popup.
        /// </summary>
        public void Close()
        {
            if (_popupHost != null)
            {
                _popupHost.TemplateApplied -= RootTemplateApplied;
            }

            _presenterSubscription?.Dispose();

            CloseCurrent();
            using (BeginIgnoringIsOpen())
            {
                IsOpen = false;
            }

            Closed?.Invoke(this, EventArgs.Empty);
        }

        void CloseCurrent()
        {
            if (_topLevel != null)
            {
                _topLevel.RemoveHandler(PointerPressedEvent, PointerPressedOutside);
                var window = _topLevel as Window;
                if (window != null)
                    window.Deactivated -= WindowDeactivated;
                else
                {
                    var parentPopuproot = _topLevel as PopupRoot;
                    if (parentPopuproot?.Parent is Popup popup)
                    {
                        popup.Closed -= ParentClosed;
                    }
                }
                _nonClientListener?.Dispose();
                _nonClientListener = null;
                
                _topLevel = null;
            }
            if (_popupHost != null)
            {
                foreach(var b in _bindings)
                    b.Dispose();
                _bindings.Clear();
                _popupHost.SetChild(null);
                _popupHost.Hide();
                ((ISetLogicalParent)_popupHost).SetParent(null);
                _popupHost.Dispose();
                _popupHost = null;
            }

        }

        /// <summary>
        /// Measures the control.
        /// </summary>
        /// <param name="availableSize">The available size for the control.</param>
        /// <returns>A size of 0,0 as Popup itself takes up no space.</returns>
        protected override Size MeasureCore(Size availableSize)
        {
            return new Size();
        }

        /// <inheritdoc/>
        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);
            Close();
        }


        /// <summary>
        /// Called when the <see cref="IsOpen"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void IsOpenChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (!_ignoreIsOpenChanged)
            {
                if ((bool)e.NewValue)
                {
                    Open();
                }
                else
                {
                    Close();
                }
            }
        }

        /// <summary>
        /// Called when the <see cref="Child"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void ChildChanged(AvaloniaPropertyChangedEventArgs e)
        {
            LogicalChildren.Clear();

            ((ISetLogicalParent)e.OldValue)?.SetParent(null);

            if (e.NewValue != null)
            {
                ((ISetLogicalParent)e.NewValue).SetParent(this);
                LogicalChildren.Add((ILogical)e.NewValue);
            }
        }

        private void ListenForNonClientClick(RawInputEventArgs e)
        {
            var mouse = e as RawPointerEventArgs;

            if (!StaysOpen && mouse?.Type == RawPointerEventType.NonClientLeftButtonDown)
            {
                Close();
            }
        }

        private void PointerPressedOutside(object sender, PointerPressedEventArgs e)
        {
            if (!StaysOpen)
            {
                if (!IsChildOrThis((IVisual)e.Source))
                {
                    Close();
                    e.Handled = true;
                }
            }
        }

        private void RootTemplateApplied(object sender, TemplateAppliedEventArgs e)
        {
            _popupHost.TemplateApplied -= RootTemplateApplied;

            if (_presenterSubscription != null)
            {
                _presenterSubscription.Dispose();
                _presenterSubscription = null;
            }

            // If the Popup appears in a control template, then the child controls
            // that appear in the popup host need to have their TemplatedParent
            // properties set.
            if (TemplatedParent != null)
            {
                _popupHost.Presenter?.ApplyTemplate();
                _popupHost.Presenter?.GetObservable(ContentPresenter.ChildProperty)
                    .Subscribe(SetTemplatedParentAndApplyChildTemplates);
            }
        }

        private void SetTemplatedParentAndApplyChildTemplates(IControl control)
        {
            if (control != null)
            {
                var templatedParent = TemplatedParent;

                if (control.TemplatedParent == null)
                {
                    control.SetValue(TemplatedParentProperty, templatedParent);
                }

                control.ApplyTemplate();

                if (!(control is IPresenter) && control.TemplatedParent == templatedParent)
                {
                    foreach (IControl child in control.GetVisualChildren())
                    {
                        SetTemplatedParentAndApplyChildTemplates(child);
                    }
                }
            }
        }

        private bool IsChildOrThis(IVisual child)
        {
            IVisual root = child.GetVisualRoot();
            while (root is IHostedVisualTreeRoot hostedRoot )
            {
                if (root == this._popupHost)
                    return true;
                root = hostedRoot.Host?.GetVisualRoot();
            }
            return false;
        }
        
        public bool IsInsidePopup(IVisual visual)
        {
            return _popupHost != null && ((IVisual)_popupHost)?.IsVisualAncestorOf(visual) == true;
        }

        public bool IsPointerOverPopup => ((IInputElement)_popupHost).IsPointerOver;

        private void WindowDeactivated(object sender, EventArgs e)
        {
            if (!StaysOpen)
            {
                Close();
            }
        }

        private void ParentClosed(object sender, EventArgs e)
        {
            if (!StaysOpen)
            {
                Close();
            }
        }

        private IgnoreIsOpenScope BeginIgnoringIsOpen()
        {
            return new IgnoreIsOpenScope(this);
        }

        private readonly struct IgnoreIsOpenScope : IDisposable
        {
            private readonly Popup _owner;

            public IgnoreIsOpenScope(Popup owner)
            {
                _owner = owner;
                _owner._ignoreIsOpenChanged = true;
            }

            public void Dispose()
            {
                _owner._ignoreIsOpenChanged = false;
            }
        }
    }
}
