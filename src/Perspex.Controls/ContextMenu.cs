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
        /// Defines the ContextMenu.Menu attached property.
        /// </summary>
        //public static readonly AttachedProperty<object> ContextMenuProperty =
        //    PerspexProperty.RegisterAttached<ContextMenu, Control, object>("Menu");

        /// <summary>
        /// The popup window used to display the active context menu.
        /// </summary>
        private static PopupRoot s_popup;

        /// <summary>
        /// The control that the currently visible context menu is attached to.
        /// </summary>
        private static Control s_current;

        /// <summary>
        /// Initializes static members of the <see cref="ToolTip"/> class.
        /// </summary>
        static ContextMenu()
        {
            ContextMenuProperty.Changed.Subscribe(ContextMenuChanged);

            MenuItem.ClickEvent.AddClassHandler<ContextMenu>(x => x.OnContextMenuClick);
        }

        /// <summary>
        /// Gets the value of the ToolTip.Tip attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <returns>
        /// The content to be displayed in the control's tooltip.
        /// </returns>
        public static object GetContextMenu(Control element)
        {
            return element.GetValue(ContextMenuProperty);
        }

        /// <summary>
        /// Sets the value of the ToolTip.Tip attached property.
        /// </summary>
        /// <param name="element">The control to get the property from.</param>
        /// <param name="value">The content to be displayed in the control's tooltip.</param>
        public static void SetContextMenu(Control element, object value)
        {
            element.SetValue(ContextMenuProperty, value);
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
        /// Shows a tooltip for the specified control.
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
                        Content = new ToolTip(),
                    };

                    ((ISetLogicalParent)s_popup).SetParent(control);
                }

                var cp = MouseDevice.Instance?.GetPosition(control);
                var position = control.PointToScreen(cp ?? new Point(0, 0));

                ((ToolTip)s_popup.Content).Content = GetContextMenu(control);
                s_popup.Position = position;
                s_popup.Show();

                s_current = control;

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
