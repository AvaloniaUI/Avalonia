using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Platform;

#nullable enable

namespace Avalonia.Win32
{
    internal class Win32NativeToManagedMenuExporter : INativeMenuExporter
    {
        private NativeMenu? _nativeMenu;

        public void SetNativeMenu(NativeMenu? nativeMenu)
        {
            _nativeMenu = nativeMenu;
        }

        private IEnumerable<MenuItem> Populate(NativeMenu nativeMenu)
        {
            foreach (var menuItem in nativeMenu.Items)
            {
                if (menuItem is NativeMenuItemSeparator)
                {
                    yield return new MenuItem { Header = "-" };
                }
                else if (menuItem is NativeMenuItem item)
                {
                    var newItem = new MenuItem { Header = item.Header, Icon = item.Icon, Command = item.Command, CommandParameter = item.CommandParameter };

                    if (item.Menu != null)
                    {
                        newItem.Items = Populate(item.Menu);
                    }
                    else if (item.HasClickHandlers && item is INativeMenuItemExporterEventsImplBridge bridge)
                    {
                        newItem.Click += (_, __) => bridge.RaiseClicked();
                    }

                    yield return newItem;
                }
            }
        }

        public IEnumerable<MenuItem>? GetMenu()
        {
            if (_nativeMenu != null)
            {
                return Populate(_nativeMenu);
            }

            return null;
        }
    }
}
