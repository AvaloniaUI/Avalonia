namespace Perspex.Controls
{
    using Input;
    using Interactivity;
    using LogicalTree;
    using Primitives;
    using System;
    using System.Reactive.Linq;

    public class ContextMenu : SelectingItemsControl
    {
        private bool _isOpen;

        /// <summary>
        /// The popup window used to display the active context menu.
        /// </summary>
        private static Popup _popup;

        /// <summary>
        /// Initializes static members of the <see cref="ContextMenu"/> class.
        /// </summary>
        static ContextMenu()
        {
            ContextMenuProperty.Changed.Subscribe(ContextMenuChanged);

            MenuItem.ClickEvent.AddClassHandler<ContextMenu>(x => x.OnContextMenuClick);            
        }

        /// <summary>
        /// called when the <see cref="ContextMenuProperty"/> property changes on a control.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void ContextMenuChanged(PerspexPropertyChangedEventArgs e)
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
            foreach (MenuItem i in this.GetLogicalChildren())
            {
                i.IsSubMenuOpen = false;
            }

            if (_popup != null && _popup.IsVisible)
            {
                _popup.Close();
            }

            SelectedIndex = -1;

            _isOpen = false;
        }

        /// <summary>
        /// Shows a context menu for the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        private static void Show(Control control)
        {
            if (control != null)
            {
                if(_popup == null)
                {
                    _popup = new Popup()
                    {
                        PlacementTarget = control,
                        StaysOpen = false                                         
                    };
                }
                 
                ((ISetLogicalParent)_popup).SetParent(control);
                _popup.Child = control.ContextMenu;

                _popup.Open();

                control.ContextMenu._isOpen = true;
            }
        }


        private void PopupOpened(object sender, EventArgs e)
        {
            var selectedIndex = SelectedIndex;

            if (selectedIndex != -1)
            {
                var container = ItemContainerGenerator.ContainerFromIndex(selectedIndex);
                container?.Focus();
            }
        }

        private static void ControlPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            var control = (Control)sender;

            if (e.MouseButton == MouseButton.Right)
            {
                if (control.ContextMenu._isOpen)
                {
                    control.ContextMenu.Hide();
                }

                Show(control);
            }
            else
            {
                control.ContextMenu.Hide();
            }
        }
    }
}
