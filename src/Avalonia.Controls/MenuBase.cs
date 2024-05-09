using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Rendering;

namespace Avalonia.Controls
{
    /// <summary>
    /// Base class for menu controls.
    /// </summary>
    public abstract class MenuBase : SelectingItemsControl, IFocusScope, IMenu
    {
        /// <summary>
        /// Defines the <see cref="IsOpen"/> property.
        /// </summary>
        public static readonly DirectProperty<MenuBase, bool> IsOpenProperty =
            AvaloniaProperty.RegisterDirect<MenuBase, bool>(
                nameof(IsOpen),
                o => o.IsOpen);

        /// <summary>
        /// Defines the <see cref="Opened"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> OpenedEvent =
            RoutedEvent.Register<MenuBase, RoutedEventArgs>(nameof(Opened), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="Closed"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> ClosedEvent =
            RoutedEvent.Register<MenuBase, RoutedEventArgs>(nameof(Closed), RoutingStrategies.Bubble);

        private bool _isOpen;

        /// <summary>
        /// Initializes a new instance of the <see cref="MenuBase"/> class.
        /// </summary>
        protected MenuBase()
        {
            InteractionHandler = new DefaultMenuInteractionHandler(false);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MenuBase"/> class.
        /// </summary>
        /// <param name="interactionHandler">The menu interaction handler.</param>
        protected MenuBase(IMenuInteractionHandler interactionHandler)
        {
            InteractionHandler = interactionHandler ?? throw new ArgumentNullException(nameof(interactionHandler));
        }

        /// <summary>
        /// Initializes static members of the <see cref="MenuBase"/> class.
        /// </summary>
        static MenuBase()
        {
            MenuItem.SubmenuOpenedEvent.AddClassHandler<MenuBase>((x, e) => x.OnSubmenuOpened(e));
        }

        /// <summary>
        /// Gets a value indicating whether the menu is open.
        /// </summary>
        public bool IsOpen
        {
            get => _isOpen;
            protected set => SetAndRaise(IsOpenProperty, ref _isOpen, value);
        }

        /// <inheritdoc/>
        IMenuInteractionHandler IMenu.InteractionHandler => InteractionHandler;

        IRenderRoot? IMenu.VisualRoot => VisualRoot;
        
        /// <inheritdoc/>
        IMenuItem? IMenuElement.SelectedItem
        {
            get
            {
                var index = SelectedIndex;
                return (index != -1) ?
                    (IMenuItem?)ContainerFromIndex(index) :
                    null;
            }
            set => SelectedIndex = value is Control c ?
                    IndexFromContainer(c) : -1;
        }

        /// <inheritdoc/>
        IEnumerable<IMenuItem> IMenuElement.SubItems => LogicalChildren.OfType<IMenuItem>();

        /// <summary>
        /// Gets the interaction handler for the menu.
        /// </summary>
        protected internal IMenuInteractionHandler InteractionHandler { get; }

        /// <summary>
        /// Occurs when a <see cref="Menu"/> is opened.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? Opened
        {
            add => AddHandler(OpenedEvent, value);
            remove => RemoveHandler(OpenedEvent, value);
        }

        /// <summary>
        /// Occurs when a <see cref="Menu"/> is closed.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? Closed
        {
            add => AddHandler(ClosedEvent, value);
            remove => RemoveHandler(ClosedEvent, value);
        }

        /// <summary>
        /// Closes the menu.
        /// </summary>
        public abstract void Close();

        /// <summary>
        /// Opens the menu.
        /// </summary>
        public abstract void Open();

        /// <inheritdoc/>
        bool IMenuElement.MoveSelection(NavigationDirection direction, bool wrap) => MoveSelection(direction, wrap);

        protected internal override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
        {
            return new MenuItem();
        }

        protected internal override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
        {
            if (item is MenuItem or Separator)
            {
                recycleKey = null;
                return false;
            }

            recycleKey = DefaultRecycleKey;
            return true;
        }

        /// <inheritdoc/>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Don't handle here: let the interaction handler handle it.
        }

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            InteractionHandler.Attach(this);
        }

        /// <inheritdoc/>
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            InteractionHandler.Detach(this);
        }

        /// <summary>
        /// Called when a submenu opens somewhere in the menu.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnSubmenuOpened(RoutedEventArgs e)
        {
            if (e.Source is MenuItem menuItem && menuItem.Parent == this)
            {
                foreach (var child in this.GetLogicalChildren().OfType<MenuItem>())
                {
                    if (child != menuItem && child.IsSubMenuOpen)
                    {
                        child.IsSubMenuOpen = false;
                    }
                }
            }

            IsOpen = true;
        }
    }
}
