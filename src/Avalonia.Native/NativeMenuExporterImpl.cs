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
    
    public class PredicateCallback : CallbackBase, IAvnPredicateCallback
    {
        private Func<bool> _predicate;

        public PredicateCallback(Func<bool> predicate)
        {
            _predicate = predicate;
        }
        
        bool IAvnPredicateCallback.Evaluate()
        {
            return _predicate();
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

                using (var buffer = new Utf8Buffer(item.Header))
                {
                    menuItem.Title = buffer.DangerousGetHandle();
                }

                menuItem.SetAction(new PredicateCallback(() =>
                {
                    if (item.Command != null)
                    {
                        return item.Command.CanExecute(null);
                    }

                    return false;
                }), new MenuActionCallback(()=>{item.RaiseClick();}));
                menu.AddItem(menuItem);

                if (item.Items.Count > 0)
                {
                    var submenu = _factory.CreateMenu();

                    using (var buffer = new Utf8Buffer(item.Header))
                    {
                        submenu.Title = buffer.DangerousGetHandle();
                    }

                    menuItem.SetSubMenu(submenu);
                    
                    AddItemsToMenu(submenu, item.Items);
                }
            }
        }

        private void AddItemsToMenu(IAvnAppMenu menu, ICollection<NativeMenuItem> items, bool isMainMenu = false)
        {
            foreach (var item in items)
            {
                var menuItem = _factory.CreateMenuItem();
                
                menuItem.SetAction(new PredicateCallback(() =>
                {
                    if (item.Command != null)
                    {
                        return item.Command.CanExecute(null);
                    }

                    return false;
                }), new MenuActionCallback(()=>{item.RaiseClick();}));

                if (item.Items.Count > 0 || isMainMenu)
                {
                    var subMenu = CreateSubmenu(item.Items);

                    menuItem.SetSubMenu(subMenu);
                    
                    using (var buffer = new Utf8Buffer(item.Header))
                    {
                        subMenu.Title = buffer.DangerousGetHandle();
                    }
                }
                else
                {
                    using (var buffer = new Utf8Buffer(item.Header))
                    {
                        menuItem.Title = buffer.DangerousGetHandle();
                    }
                }

                menu.AddItem(menuItem);
            }
        }

        public void SetMenu(ICollection<NativeMenuItem> menuItems)
        {
            var appMenu = _factory.ObtainAppMenu();

            appMenu.Clear();
            
            AddItemsToMenu(appMenu, menuItems);
        }
    }
}
