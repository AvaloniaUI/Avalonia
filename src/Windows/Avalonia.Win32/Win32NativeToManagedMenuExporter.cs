using System.Collections.Generic;
using Avalonia.Collections;
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

        private AvaloniaList<MenuItem> Populate(NativeMenu nativeMenu)
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
                    var newItem = new MenuItem { Header = item.Header, Icon = item.Icon, Command = item.Command, CommandParameter = item.CommandParameter };

                    if (item.Menu != null)
                    {
                        newItem.Items = Populate(item.Menu);
                    }
                    else if (item.HasClickHandlers && item is INativeMenuItemExporterEventsImplBridge bridge)
                    {
                        newItem.Click += (_, __) => bridge.RaiseClicked();
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
