using System;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control context menu.
    /// </summary>
    public class ContextMenu : MenuBase
    {
        private static readonly ITemplate<IPanel> DefaultPanel =
            new FuncTemplate<IPanel>(() => new StackPanel { Orientation = Orientation.Vertical });
        private Popup _popup;

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
            ItemsPanelProperty.OverrideDefaultValue(typeof(ContextMenu), DefaultPanel);
            ContextMenuProperty.Changed.Subscribe(ContextMenuChanged);
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
        public override void Open() => Open(null);

        /// <summary>
        /// Opens a context menu on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        public void Open(Control control)
        {
            if (IsOpen)
            {
                return;
            }

            if (_popup == null)
            {
                _popup = new Popup
                {
                    PlacementMode = PlacementMode.Pointer,
                    PlacementTarget = control,
                    StaysOpen = false,
                    ObeyScreenEdges = true
                };

                _popup.Opened += PopupOpened;
                _popup.Closed += PopupClosed;
            }

            ((ISetLogicalParent)_popup).SetParent(control);
            _popup.Child = this;
            _popup.IsOpen = true;

            IsOpen = true;

            RaiseEvent(new RoutedEventArgs
            {
                RoutedEvent = MenuOpenedEvent,
                Source = this,
            });
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

        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new MenuItemContainerGenerator(this);
        }

        private void CloseCore()
        {
            SelectedIndex = -1;
            IsOpen = false;

            RaiseEvent(new RoutedEventArgs
            {
                RoutedEvent = MenuClosedEvent,
                Source = this,
            });
        }

        private void PopupOpened(object sender, EventArgs e)
        {
            Focus();
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

                contextMenu.CloseCore();
            }
        }

        private static void ControlPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            var control = (Control)sender;
            var contextMenu = control.ContextMenu;

            if (control.ContextMenu.IsOpen)
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
    }
}
