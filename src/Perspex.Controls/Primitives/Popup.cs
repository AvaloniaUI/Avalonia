// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Interactivity;
using Perspex.Metadata;
using Perspex.Rendering;
using Perspex.VisualTree;

namespace Perspex.Controls.Primitives
{
    /// <summary>
    /// Displays a popup window.
    /// </summary>
    public class Popup : Control, IVisualTreeHost
    {
        /// <summary>
        /// Defines the <see cref="Child"/> property.
        /// </summary>
        public static readonly PerspexProperty<Control> ChildProperty =
            PerspexProperty.Register<Popup, Control>(nameof(Child));

        /// <summary>
        /// Defines the <see cref="IsOpen"/> property.
        /// </summary>
        public static readonly PerspexProperty<bool> IsOpenProperty =
            PerspexProperty.Register<Popup, bool>(nameof(IsOpen));

        /// <summary>
        /// Defines the <see cref="PlacementMode"/> property.
        /// </summary>
        public static readonly PerspexProperty<PlacementMode> PlacementModeProperty =
            PerspexProperty.Register<Popup, PlacementMode>(nameof(PlacementMode), defaultValue: PlacementMode.Bottom);

        /// <summary>
        /// Defines the <see cref="PlacementTarget"/> property.
        /// </summary>
        public static readonly PerspexProperty<Control> PlacementTargetProperty =
            PerspexProperty.Register<Popup, Control>(nameof(PlacementTarget));

        /// <summary>
        /// Defines the <see cref="StaysOpen"/> property.
        /// </summary>
        public static readonly PerspexProperty<bool> StaysOpenProperty =
            PerspexProperty.Register<Popup, bool>(nameof(StaysOpen), true);

        /// <summary>
        /// The root of the popup.
        /// </summary>
        private PopupRoot _popupRoot;

        /// <summary>
        /// The top level control of the Popup's visual tree.
        /// </summary>
        private TopLevel _topLevel;

        /// <summary>
        /// Initializes static members of the <see cref="Popup"/> class.
        /// </summary>
        static Popup()
        {
            ChildProperty.Changed.AddClassHandler<Popup>(x => x.ChildChanged);
            IsOpenProperty.Changed.AddClassHandler<Popup>(x => x.IsOpenChanged);
        }

        /// <summary>
        /// Raised when the popup closes.
        /// </summary>
        public event EventHandler Closed;

        /// <summary>
        /// Raised when the popup opens.
        /// </summary>
        public event EventHandler Opened;

        /// <summary>
        /// Raised when the popup root has been created, but before it has been shown.
        /// </summary>
        public event EventHandler PopupRootCreated;

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
        public IPerspexDependencyResolver DependencyResolver
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the popup is currently open.
        /// </summary>
        public bool IsOpen
        {
            get { return GetValue(IsOpenProperty); }
            set { SetValue(IsOpenProperty, value); }
        }

        /// <summary>
        /// Gets or sets the placement mode of the popup in relation to the <see cref="PlacementTarget"/>.
        /// </summary>
        public PlacementMode PlacementMode
        {
            get { return GetValue(PlacementModeProperty); }
            set { SetValue(PlacementModeProperty, value); }
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
        /// Gets the root of the popup window.
        /// </summary>
        public PopupRoot PopupRoot => _popupRoot;

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
        /// Gets the root of the popup window.
        /// </summary>
        IVisual IVisualTreeHost.Root => _popupRoot;

        /// <summary>
        /// Opens the popup.
        /// </summary>
        public void Open()
        {
            if (_popupRoot == null)
            {
                _popupRoot = new PopupRoot(DependencyResolver)
                {
                    [~ContentControl.ContentProperty] = this[~ChildProperty],
                    [~WidthProperty] = this[~WidthProperty],
                    [~HeightProperty] = this[~HeightProperty],
                    [~MinWidthProperty] = this[~MinWidthProperty],
                    [~MaxWidthProperty] = this[~MaxWidthProperty],
                    [~MinHeightProperty] = this[~MinHeightProperty],
                    [~MaxHeightProperty] = this[~MaxHeightProperty],
                };

                ((ISetLogicalParent)_popupRoot).SetParent(this);
            }

            _popupRoot.SetPosition(GetPosition());
            _popupRoot.AddHandler(PointerPressedEvent, MaybeClose, RoutingStrategies.Bubble, true);

            if (_topLevel != null)
            {
                _topLevel.Deactivated += MaybeClose;
                _topLevel.AddHandler(PointerPressedEvent, MaybeClose, RoutingStrategies.Tunnel);
            }

            PopupRootCreated?.Invoke(this, EventArgs.Empty);

            _popupRoot.Show();
            IsOpen = true;
            Opened?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Closes the popup.
        /// </summary>
        public void Close()
        {
            if (_popupRoot != null)
            {
                _popupRoot.PointerPressed -= MaybeClose;
                _topLevel.RemoveHandler(PointerPressedEvent, MaybeClose);
                _topLevel.Deactivated -= MaybeClose;
                _popupRoot.Hide();
            }

            IsOpen = false;
            Closed?.Invoke(this, EventArgs.Empty);
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
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _topLevel = e.Root as TopLevel;
        }

        /// <inheritdoc/>
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _topLevel = null;
        }

        /// <summary>
        /// Called when the <see cref="IsOpen"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void IsOpenChanged(PerspexPropertyChangedEventArgs e)
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

        /// <summary>
        /// Called when the <see cref="Child"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void ChildChanged(PerspexPropertyChangedEventArgs e)
        {
            LogicalChildren.Clear();

            if (e.OldValue != null)
            {
                ((ISetLogicalParent)e.OldValue).SetParent(null);
            }

            if (e.NewValue != null)
            {
                ((ISetLogicalParent)e.NewValue).SetParent(this);
                LogicalChildren.Add((ILogical)e.NewValue);
            }
        }

        /// <summary>
        /// Gets the position for the popup based on the placement properties.
        /// </summary>
        /// <returns>The popup's position in screen coordinates.</returns>
        private Point GetPosition()
        {
            var target = PlacementTarget ?? this.GetVisualParent<Control>();
            Point point;

            if (target != null)
            {
                switch (PlacementMode)
                {
                    case PlacementMode.Bottom:
                        point = new Point(0, target.Bounds.Height);
                        break;
                    case PlacementMode.Right:
                        point = new Point(target.Bounds.Width, 0);
                        break;
                    default:
                        throw new InvalidOperationException("Invalid value for Popup.PlacementMode");
                }

                return target.PointToScreen(point);
            }
            else
            {
                return new Point();
            }
        }

        /// <summary>
        /// Conditionally closes the popup in response to an event, based on the value of the
        /// <see cref="StaysOpen"/> property.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void MaybeClose(object sender, EventArgs e)
        {
            if (!StaysOpen)
            {
                Close();
            }
        }
    }
}
