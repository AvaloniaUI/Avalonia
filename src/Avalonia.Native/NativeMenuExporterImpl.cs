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

        private void CreateSubmenu(IAvnAppMenuItem parent, NativeMenuItem item, ICollection<NativeMenuItem> children)
        {
            var menu = _factory.CreateMenu();

            using (var buffer = new Utf8Buffer(item.Text))
            {
                menu.Title = buffer.DangerousGetHandle();
            }
            
            SetChildren(menu, children);
        }

        private void SetChildren(IAvnAppMenu menu, ICollection<NativeMenuItem> children)
        {
            foreach (var item in children)
            {
                var menuItem = _factory.CreateMenuItem();

                using (var buffer = new Utf8Buffer(item.Text))
                {
                    menuItem.Title = buffer.DangerousGetHandle();
                }

                menu.AddItem(menuItem);

                if (item.SubItems.Count > 0)
                {
                    CreateSubmenu(menuItem, item, item.SubItems);
                }
            }
        }

        public void SetMenu(ICollection<NativeMenuItem> menuItems)
        {
            var mainMenu = _factory.ObtainMainAppMenu();


            foreach (var menuItem in menuItems)
            {
                if (menuItem.SubItems.Count > 0)
                {
                    var menu = _factory.CreateMenu();


                    var item = _factory.CreateMenuItem();

                    using (var buffer = new Utf8Buffer(menuItem.Text))
                    {

                        menu.Title = buffer.DangerousGetHandle();
                    }

                    using (var buffer = new Utf8Buffer("ItemTitle"))
                    {

                        item.Title = buffer.DangerousGetHandle();
                    }

                    mainMenu.AddItem(item);

                    item.SetSubMenu(menu);
                    
                    SetChildren(menu, menuItem.SubItems);
                }
            }
        }
    }
}
