using System;
using System.Reactive.Linq;
using System.Linq;
using System.ComponentModel;
using Avalonia.Controls.Platform;
using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Controls.Primitives;

namespace Avalonia.Controls
{
    public class ContextMenu : SelectingItemsControl, IMenu
    {
        private readonly IMenuInteractionHandler _interaction;
        private bool _isOpen;
        private Popup _popup;

        /// <summary>
        /// Defines the <see cref="IsOpen"/> property.
        /// </summary>
        public static readonly DirectProperty<ContextMenu, bool> IsOpenProperty =
                            AvaloniaProperty.RegisterDirect<ContextMenu, bool>(nameof(IsOpen), o => o.IsOpen);

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextMenu"/> class.
        /// </summary>
        public ContextMenu()
        {
            _interaction = AvaloniaLocator.Current.GetService<IMenuInteractionHandler>() ??
                new DefaultMenuInteractionHandler();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextMenu"/> class.
        /// </summary>
        /// <param name="interactionHandler">The menu interaction handler.</param>
        public ContextMenu(IMenuInteractionHandler interactionHandler)
        {
            Contract.Requires<ArgumentNullException>(interactionHandler != null);

            _interaction = interactionHandler;
        }

        /// <summary>
        /// Initializes static members of the <see cref="ContextMenu"/> class.
        /// </summary>
        static ContextMenu()
        {
            ContextMenuProperty.Changed.Subscribe(ContextMenuChanged);
        }

        /// <summary>
        /// Gets a value indicating whether the popup is open
        /// </summary>
        public bool IsOpen => _isOpen;

        /// <inheritdoc/>
        IMenuInteractionHandler IMenu.InteractionHandler => _interaction;

        /// <inheritdoc/>
        IMenuItem IMenuElement.SelectedItem
        {
            get
            {
                var index = SelectedIndex;
                return (index != -1) ?
                    (IMenuItem)ItemContainerGenerator.ContainerFromIndex(index) :
                    null;
            }
            set
            {
                SelectedIndex = ItemContainerGenerator.IndexFromContainer(value);
            }
        }

        /// <inheritdoc/>
        IEnumerable<IMenuItem> IMenuElement.SubItems
        {
            get
            {
                return ItemContainerGenerator.Containers
                    .Select(x => x.ContainerControl)
                    .OfType<IMenuItem>();
            }
        }

        /// <summary>
        /// Occurs when the value of the
        /// <see cref="P:Avalonia.Controls.ContextMenu.IsOpen" />
        /// property is changing from false to true.
        /// </summary>
        public event CancelEventHandler ContextMenuOpening;

        /// <summary>
        /// Occurs when the value of the
        /// <see cref="P:Avalonia.Controls.ContextMenu.IsOpen" />
        /// property is changing from true to false.
        /// </summary>
        public event CancelEventHandler ContextMenuClosing;

        /// <summary>
        /// Called when the <see cref="Control.ContextMenu"/> property changes on a control.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void ContextMenuChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var control = (Control)e.Sender;

            if (e.OldValue != null)
            {
                control.PointerReleased -= ControlPointerReleased;
            }

            if (e.NewValue != null)
            {
                control.PointerReleased += ControlPointerReleased;
            }
        }

        /// <summary>
        /// Opens the menu.
        /// </summary>
        public void Open() => Open(null);

        /// <summary>
        /// Opens a context menu on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        public void Open(Control control)
        {
            if (_popup == null)
            {
                _popup = new Popup()
                {
                    PlacementMode = PlacementMode.Pointer,
                    PlacementTarget = control,
                    StaysOpen = false,
                    ObeyScreenEdges = true
                };

                _popup.Closed += PopupClosed;
                _interaction.Attach(this);
            }

            ((ISetLogicalParent)_popup).SetParent(control);
            _popup.Child = this;
            _popup.IsOpen = true;

            SetAndRaise(IsOpenProperty, ref _isOpen, true);
        }

        /// <summary>
        /// Closes the menu.
        /// </summary>
        public void Close()
        {
            if (_popup != null && _popup.IsVisible)
            {
                _popup.IsOpen = false;
            }

            SelectedIndex = -1;

            SetAndRaise(IsOpenProperty, ref _isOpen, false);
        }

        private void PopupClosed(object sender, EventArgs e)
        {
            var contextMenu = (sender as Popup)?.Child as ContextMenu;

            if (contextMenu != null)
            {
                foreach (var i in contextMenu.GetLogicalChildren().OfType<MenuItem>())
                {
                    i.IsSubMenuOpen = false;
                }

                contextMenu._isOpen = false;
                contextMenu.SelectedIndex = -1;
            }
        }

        private static void ControlPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            var control = (Control)sender;
            var contextMenu = control.ContextMenu;

            if (control.ContextMenu._isOpen)
            {
                if (contextMenu.CancelClosing())
                    return;

                control.ContextMenu.Close();
                e.Handled = true;
            }

            if (e.MouseButton == MouseButton.Right)
            {
                if (contextMenu.CancelOpening())
                    return;

                contextMenu.Open(control);
                e.Handled = true;
            }
        }

        private bool CancelClosing()
        {
            var eventArgs = new CancelEventArgs();
            ContextMenuClosing?.Invoke(this, eventArgs);
            return eventArgs.Cancel;
        }

        private bool CancelOpening()
        {
            var eventArgs = new CancelEventArgs();
            ContextMenuOpening?.Invoke(this, eventArgs);
            return eventArgs.Cancel;
        }

        bool IMenuElement.MoveSelection(NavigationDirection direction, bool wrap)
        {
            throw new NotImplementedException();
        }
    }
}
