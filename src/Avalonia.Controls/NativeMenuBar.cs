using System;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    public class NativeMenuBar : TemplatedControl
    {
        public static readonly AttachedProperty<bool> EnableMenuItemClickForwardingProperty =
            AvaloniaProperty.RegisterAttached<NativeMenuBar, MenuItem, Boolean>(
                "EnableMenuItemClickForwarding");

        static NativeMenuBar()
        {
            EnableMenuItemClickForwardingProperty.Changed.Subscribe(args =>
            {
                var item = (MenuItem)args.Sender;
                if (args.NewValue.Equals(true))
                    item.Click += OnMenuItemClick;
                else
                    item.Click -= OnMenuItemClick;
            });
        }
        
        public static void SetEnableMenuItemClickForwarding(MenuItem menuItem, bool enable)
        {
            menuItem.SetValue(EnableMenuItemClickForwardingProperty, enable);
        }

        private static void OnMenuItemClick(object sender, RoutedEventArgs e)
        {
            (((MenuItem)sender).DataContext as NativeMenuItem)?.RaiseClick();
        }
    }
}
