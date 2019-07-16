using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Native.Interop;
using Avalonia.Platform.Interop;

namespace Avalonia.Native
{
    public class MenuActionCallback : CallbackBase, IAvnActionCallback
    {
        private Action _action;

        public MenuActionCallback(Action action)
        {
            _action = action;
        }
        
        void IAvnActionCallback.Run()
        {
            _action?.Invoke();
        }
    }

    public class NativeMenuExporterImpl : INativeMenuExporter
    {
        private IAvaloniaNativeFactory _factory;

        public NativeMenuExporterImpl(IAvaloniaNativeFactory factory)
        {
            _factory = factory;
        }

        private IAvnAppMenu CreateSubmenu(ICollection<NativeMenuItem> children)
        {
            var menu = _factory.CreateMenu();

            SetChildren(menu, children);

            return menu;
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
                    var submenu = _factory.CreateMenu();

                    using (var buffer = new Utf8Buffer(item.Text))
                    {
                        submenu.Title = buffer.DangerousGetHandle();
                    }

                    menuItem.SetSubMenu(submenu);
                    
                    AddItemsToMenu(submenu, item.SubItems);
                }
            }
        }

        private void AddItemsToMenu(IAvnAppMenu menu, ICollection<NativeMenuItem> items, bool isMainMenu = false)
        {
            foreach (var item in items)
            {
                var menuItem = _factory.CreateMenuItem();
                
                menuItem.SetAction(new MenuActionCallback(()=>item.RaiseClick()));

                if (item.SubItems.Count > 0 || isMainMenu)
                {
                    var subMenu = CreateSubmenu(item.SubItems);

                    menuItem.SetSubMenu(subMenu);
                    
                    using (var buffer = new Utf8Buffer(item.Text))
                    {
                        subMenu.Title = buffer.DangerousGetHandle();
                    }
                }
                else
                {
                    using (var buffer = new Utf8Buffer(item.Text))
                    {
                        menuItem.Title = buffer.DangerousGetHandle();
                    }
                }

                menu.AddItem(menuItem);
            }
        }

        public void SetMenu(ICollection<NativeMenuItem> menuItems)
        {
            var mainMenu = _factory.ObtainAppMenu();
            AddItemsToMenu(_factory.ObtainAppBar(), menuItems);
            /*var mainMenu = _factory.ObtainMainAppMenu();


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
            }*/
        }
    }
}
