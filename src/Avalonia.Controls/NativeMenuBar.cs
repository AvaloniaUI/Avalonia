using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

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
                if (args.NewValue.GetValueOrDefault<bool>())
                    item.Click += OnMenuItemClick;
                else
                    item.Click -= OnMenuItemClick;
            });
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NativeMenu))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NativeMenuItem))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NativeMenuItemBase))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NativeMenuItemSeparator))]
        public NativeMenuBar()
        {

        }

        public static void SetEnableMenuItemClickForwarding(MenuItem menuItem, bool enable)
        {
            menuItem.SetValue(EnableMenuItemClickForwardingProperty, enable);
        }

        private static void OnMenuItemClick(object? sender, RoutedEventArgs e)
        {
            (((MenuItem)sender!).DataContext as INativeMenuItemExporterEventsImplBridge)?.RaiseClicked();
        }
    }
}
