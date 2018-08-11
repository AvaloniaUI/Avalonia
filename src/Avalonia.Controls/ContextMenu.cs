namespace Avalonia.Controls
{
    using Input;
    using Interactivity;
    using LogicalTree;
    using Primitives;
    using System;
    using System.Reactive.Linq;
    using System.Linq;
    using System.ComponentModel;

    public class ContextMenu : SelectingItemsControl
    {
        private bool _isOpen;
        private Popup _popup;

        /// <summary>
        /// Defines the <see cref="IsOpen"/> property.
        /// </summary>
        public static readonly DirectProperty<ContextMenu, bool> IsOpenProperty =
                            AvaloniaProperty.RegisterDirect<ContextMenu, bool>(nameof(IsOpen), o => o.IsOpen);


        /// <summary>
        /// Initializes static members of the <see cref="ContextMenu"/> class.
        /// </summary>
        static ContextMenu()
        {
            ContextMenuProperty.Changed.Subscribe(ContextMenuChanged);

            MenuItem.ClickEvent.AddClassHandler<ContextMenu>(x => x.OnContextMenuClick, handledEventsToo: true);
        }

        /// <summary>
        /// Gets a value indicating whether the popup is open
        /// </summary>
        public bool IsOpen => _isOpen;

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
        /// Called when a submenu is clicked somewhere in the menu.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void OnContextMenuClick(RoutedEventArgs e)
        {
            Hide();
            FocusManager.Instance.Focus(null);
            e.Handled = true;
        }

        /// <summary>
        /// Closes the menu.
        /// </summary>
        public void Hide()
        {
            if (_popup != null && _popup.IsVisible)
            {
                _popup.IsOpen = false;
            }

            SelectedIndex = -1;

            SetAndRaise(IsOpenProperty, ref _isOpen, false);
        }

        /// <summary>
        /// Shows a context menu for the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        private void Show(Control control)
        {
            if (control != null)
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
                }

                ((ISetLogicalParent)_popup).SetParent(control);
                _popup.Child = this;

                _popup.IsOpen = true;

                SetAndRaise(IsOpenProperty, ref _isOpen, true);
            }
        }

        private static void PopupClosed(object sender, EventArgs e)
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

                control.ContextMenu.Hide();
                e.Handled = true;
            }

            if (e.MouseButton == MouseButton.Right)
            {
                if (contextMenu.CancelOpening())
                    return;

                contextMenu.Show(control);
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
