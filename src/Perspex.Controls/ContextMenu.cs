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
        private static PopupRoot s_popup;

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

            if (s_popup != null && s_popup.IsVisible)
            {
                s_popup.Hide();
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
                if (s_popup == null)
                {
                    s_popup = new PopupRoot
                    {
                        Content = new ContentControl(),
                    };

                    ((ISetLogicalParent)s_popup).SetParent(control);
                }

                var cp = MouseDevice.Instance?.GetPosition(control);
                var position = control.PointToScreen(cp ?? new Point(0, 0));

                ((ContentControl)s_popup.Content).Content = control.ContextMenu;
                s_popup.Position = position;
                s_popup.Show();

                control.ContextMenu._isOpen = true;
            }
        }

        private static void ControlPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            var control = (Control)sender;

            if (e.MouseButton == MouseButton.Right)
            {
                if(control.ContextMenu._isOpen)
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
