// -----------------------------------------------------------------------
// <copyright file="Popup.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Primitives
{
    using System;
    using Perspex.Interactivity;
    using Perspex.Rendering;
    using Perspex.VisualTree;
    using Splat;

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
        private PopupRoot popupRoot;

        /// <summary>
        /// The top level control of the Popup's visual tree.
        /// </summary>
        private TopLevel topLevel;

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
        public Control Child
        {
            get { return this.GetValue(ChildProperty); }
            set { this.SetValue(ChildProperty, value); }
        }

        /// <summary>
        /// Gets or sets a dependency resolver for the <see cref="PopupRoot"/>.
        /// </summary>
        /// <remarks>
        /// This property allows a client to customize the behaviour of the popup by injecting
        /// a specialized dependency resolver into the <see cref="PopupRoot"/>'s constructor.
        /// </remarks>
        public IDependencyResolver DependencyResolver
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the popup is currently open.
        /// </summary>
        public bool IsOpen
        {
            get { return this.GetValue(IsOpenProperty); }
            set { this.SetValue(IsOpenProperty, value); }
        }

        /// <summary>
        /// Gets or sets the placement mode of the popup in relation to the <see cref="PlacementTarget"/>.
        /// </summary>
        public PlacementMode PlacementMode
        {
            get { return this.GetValue(PlacementModeProperty); }
            set { this.SetValue(PlacementModeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the control that is used to determine the popup's position.
        /// </summary>
        public Control PlacementTarget
        {
            get { return this.GetValue(PlacementTargetProperty); }
            set { this.SetValue(PlacementTargetProperty, value); }
        }

        /// <summary>
        /// Gets the root of the popup window.
        /// </summary>
        public PopupRoot PopupRoot
        {
            get { return this.popupRoot; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the popup should stay open when the popup loses focus.
        /// </summary>
        public bool StaysOpen
        {
            get { return this.GetValue(StaysOpenProperty); }
            set { this.SetValue(StaysOpenProperty, value); }
        }

        /// <summary>
        /// Gets the root of the popup window.
        /// </summary>
        IVisual IVisualTreeHost.Root
        {
            get { return this.popupRoot; }
        }

        /// <summary>
        /// Opens the popup.
        /// </summary>
        public void Open()
        {
            if (this.popupRoot == null)
            {
                this.popupRoot = new PopupRoot(this.DependencyResolver)
                {
                    [~PopupRoot.ContentProperty] = this[~ChildProperty],
                    [~PopupRoot.WidthProperty] = this[~WidthProperty],
                    [~PopupRoot.HeightProperty] = this[~HeightProperty],
                    [~PopupRoot.MinWidthProperty] = this[~MinWidthProperty],
                    [~PopupRoot.MaxWidthProperty] = this[~MaxWidthProperty],
                    [~PopupRoot.MinHeightProperty] = this[~MinHeightProperty],
                    [~PopupRoot.MaxHeightProperty] = this[~MaxHeightProperty],
                };

                ((ISetLogicalParent)this.popupRoot).SetParent(this);
            }

            this.popupRoot.SetPosition(this.GetPosition());
            this.topLevel.Deactivated += this.MaybeClose;
            this.popupRoot.AddHandler(PopupRoot.PointerPressedEvent, this.MaybeClose, RoutingStrategies.Bubble, true);
            this.topLevel.AddHandler(TopLevel.PointerPressedEvent, this.MaybeClose, RoutingStrategies.Tunnel);

            this.PopupRootCreated?.Invoke(this, EventArgs.Empty);

            this.popupRoot.Show();
            this.IsOpen = true;
            this.Opened?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Closes the popup.
        /// </summary>
        public void Close()
        {
            if (this.popupRoot != null)
            {
                this.popupRoot.PointerPressed -= this.MaybeClose;
                this.topLevel.RemoveHandler(TopLevel.PointerPressedEvent, this.MaybeClose);
                this.topLevel.Deactivated -= this.MaybeClose;
                this.popupRoot.Hide();
            }

            this.IsOpen = false;
            this.Closed?.Invoke(this, EventArgs.Empty);
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

        /// <summary>
        /// Called when the control is added to the visual tree.
        /// </summary>
        /// <param name="root">THe root of the visual tree.</param>
        protected override void OnAttachedToVisualTree(IRenderRoot root)
        {
            base.OnAttachedToVisualTree(root);
            this.topLevel = root as TopLevel;
        }

        /// <summary>
        /// Called when the control is removed to the visual tree.
        /// </summary>
        /// <param name="root">THe root of the visual tree.</param>
        protected override void OnDetachedFromVisualTree(IRenderRoot root)
        {
            base.OnDetachedFromVisualTree(root);
            this.topLevel = null;
        }

        /// <summary>
        /// Called when the <see cref="IsOpen"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void IsOpenChanged(PerspexPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                this.Open();
            }
            else
            {
                this.Close();
            }
        }

        /// <summary>
        /// Called when the <see cref="Child"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void ChildChanged(PerspexPropertyChangedEventArgs e)
        {
            this.LogicalChildren.Clear();

            if (e.OldValue != null)
            {
                ((ISetLogicalParent)e.OldValue).SetParent(null);
            }

            if (e.NewValue != null)
            {
                ((ISetLogicalParent)e.NewValue).SetParent(this);
                this.LogicalChildren.Add((ILogical)e.NewValue);
            }
        }

        /// <summary>
        /// Gets the position for the popup based on the placement properties.
        /// </summary>
        /// <returns>The popup's position in screen coordinates.</returns>
        private Point GetPosition()
        {
            var target = this.PlacementTarget ?? this.GetVisualParent<Control>();
            Point point;

            if (target != null)
            {
                switch (this.PlacementMode)
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
            if (!this.StaysOpen)
            {
                var routed = e as RoutedEventArgs;

                if (routed != null)
                {
                    routed.Handled = true;
                }

                this.Close();
            }
        }
    }
}
