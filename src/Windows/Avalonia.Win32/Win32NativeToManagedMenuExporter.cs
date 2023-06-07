using Avalonia.Reactive;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Platform;

namespace Avalonia.Win32
{
    internal class Win32NativeToManagedMenuExporter : INativeMenuExporter
    {
        private NativeMenu? _nativeMenu;

        public void SetNativeMenu(NativeMenu? nativeMenu)
        {
            _nativeMenu = nativeMenu;
        }

        private static AvaloniaList<MenuItem> Populate(NativeMenu nativeMenu)
        {
            var result = new AvaloniaList<MenuItem>();
            
            foreach (var menuItem in nativeMenu.Items)
            {
                if (menuItem is NativeMenuItemSeparator)
                {
                    result.Add(new MenuItem { Header = "-" });
                }
                else if (menuItem is NativeMenuItem item)
                {
                    var newItem = new MenuItem
                    {
                        [!MenuItem.HeaderProperty] = item.GetObservable(NativeMenuItem.HeaderProperty).ToBinding(),
                        [!MenuItem.IconProperty] = item.GetObservable(NativeMenuItem.IconProperty)
                            .Select(i => i is {} bitmap ? new Image { Source = bitmap } : null).ToBinding(),
                        [!MenuItem.IsEnabledProperty] = item.GetObservable(NativeMenuItem.IsEnabledProperty).ToBinding(),
                        [!MenuItem.CommandProperty] = item.GetObservable(NativeMenuItem.CommandProperty).ToBinding(),
                        [!MenuItem.CommandParameterProperty] = item.GetObservable(NativeMenuItem.CommandParameterProperty).ToBinding(),
                        [!MenuItem.InputGestureProperty] = item.GetObservable(NativeMenuItem.GestureProperty).ToBinding()
                    };

                    if (item.Menu != null)
                    {
                        newItem.ItemsSource = Populate(item.Menu);
                    }
                    else if (item.HasClickHandlers && item is INativeMenuItemExporterEventsImplBridge bridge)
                    {
                        newItem.Click += (_, _) => bridge.RaiseClicked();
                    }

                    result.Add(newItem);
                }
            }

            return result;
        }

        public AvaloniaList<MenuItem>? GetMenu()
        {
            if (_nativeMenu != null)
            {
                return Populate(_nativeMenu);
            }

            return null;
        }
    }
}
