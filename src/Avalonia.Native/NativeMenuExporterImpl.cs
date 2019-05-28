using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Native.Interop;
using Avalonia.Platform.Interop;

namespace Avalonia.Native
{
    public class NativeMenuExporterImpl : INativeMenuExporter
    {
        private IAvaloniaNativeFactory _factory;

        public NativeMenuExporterImpl(IAvaloniaNativeFactory factory)
        {
            _factory = factory;
        }

        public void SetMenu(ICollection<NativeMenuItem> menuItems)
        {
            var mainMenu = _factory.ObtainMainAppMenu();

            using (var buffer = new Utf8Buffer("MainMenu"))
            {
                mainMenu.Title = buffer.DangerousGetHandle();
            }

            foreach (var menuItem in menuItems)
            {
                var item = _factory.CreateMenuItem();

                using (var buffer = new Utf8Buffer(menuItem.Text))
                {
                    item.Title = buffer.DangerousGetHandle();
                }

                mainMenu.AddItem(item);
            }
        }
    }
}
