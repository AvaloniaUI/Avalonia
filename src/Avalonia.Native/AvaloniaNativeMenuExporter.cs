using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Native.Interop;
using Avalonia.Platform.Interop;
using Avalonia.Threading;

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

    class AvaloniaNativeMenuExporter : ITopLevelNativeMenuExporter
    {
        private IAvaloniaNativeFactory _factory;
        private NativeMenu _menu;
        private bool _resetQueued;                
        private bool _exported = false;
        private IAvnWindow _nativeWindow;
        private bool _prependAppMenu;
        private List<NativeMenuItem> _menuItems = new List<NativeMenuItem>(); 

        public AvaloniaNativeMenuExporter(IAvnWindow nativeWindow, IAvaloniaNativeFactory factory)
        {
            _factory = factory;
            _nativeWindow = nativeWindow;
        }

        public bool IsNativeMenuExported => _exported;

        public event EventHandler OnIsNativeMenuExportedChanged;

        public void SetNativeMenu(NativeMenu menu)
        {
            if (menu == null)
                menu = new NativeMenu();

            if (_menu != null)
                ((INotifyCollectionChanged)_menu.Items).CollectionChanged -= OnMenuItemsChanged;
            _menu = menu;
            ((INotifyCollectionChanged)_menu.Items).CollectionChanged += OnMenuItemsChanged;

            DoLayoutReset();
        }

        public void SetPrependApplicationMenu(bool prepend)
        {
            _prependAppMenu = prepend;
        }

        private void OnItemPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            QueueReset();
        }

        private void OnMenuItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            QueueReset();
        }

        /*
                 This is basic initial implementation, so we don't actually track anything and
                 just reset the whole layout on *ANY* change
                 
                 This is not how it should work and will prevent us from implementing various features,
                 but that's the fastest way to get things working, so...
             */
        void DoLayoutReset()
        {
            _resetQueued = false;
            foreach (var i in _menuItems)
            {
                i.PropertyChanged -= OnItemPropertyChanged;
                if (i.Menu != null)
                    ((INotifyCollectionChanged)i.Menu.Items).CollectionChanged -= OnMenuItemsChanged;
            }

            _menuItems.Clear();      

            SetMenu(_nativeWindow, _menu.Items);
            
            _exported = true;
        }

        private void QueueReset()
        {
            if (_resetQueued)
                return;
            _resetQueued = true;
            Dispatcher.UIThread.Post(DoLayoutReset, DispatcherPriority.Background);
        }

        private IAvnAppMenu CreateSubmenu(ICollection<NativeMenuItem> children)
        {
            var menu = _factory.CreateMenu();

            SetChildren(menu, children);

            return menu;
        }

        private void AddMenuItem(NativeMenuItem item)
        {
            if(item.Menu?.Items != null)
            {
                ((INotifyCollectionChanged)item.Menu.Items).CollectionChanged += OnMenuItemsChanged;
            }
        }

        private void SetChildren(IAvnAppMenu menu, ICollection<NativeMenuItem> children)
        {
            foreach (var item in children)
            {
                AddMenuItem(item);

                var menuItem = _factory.CreateMenuItem();

                using (var buffer = new Utf8Buffer(item.Header))
                {
                    menuItem.Title = buffer.DangerousGetHandle();
                }

                if (item.Gesture != null)
                {
                    using (var buffer = new Utf8Buffer(item.Gesture.Key.ToString().ToLower()))
                    {
                        menuItem.SetGesture(buffer.DangerousGetHandle(), (AvnInputModifiers)item.Gesture.KeyModifiers);
                    }
                }

                menuItem.SetAction(new PredicateCallback(() =>
                {
                    if (item.Command != null || item.HasClickHandlers)
                    {
                        return item.Enabled;
                    }

                    return false;
                }), new MenuActionCallback(() => { item.RaiseClick(); }));
                menu.AddItem(menuItem);

                if (item.Menu?.Items?.Count > 0)
                {
                    var submenu = _factory.CreateMenu();

                    using (var buffer = new Utf8Buffer(item.Header))
                    {
                        submenu.Title = buffer.DangerousGetHandle();
                    }

                    menuItem.SetSubMenu(submenu);

                    AddItemsToMenu(submenu, item.Menu?.Items);
                }
            }
        }

        private void AddItemsToMenu(IAvnAppMenu menu, ICollection<NativeMenuItem> items, bool isMainMenu = false)
        {
            foreach (var item in items)
            {
                var menuItem = _factory.CreateMenuItem();

                AddMenuItem(item);

                menuItem.SetAction(new PredicateCallback(() =>
                {
                    if (item.Command != null || item.HasClickHandlers)
                    {
                        return item.Enabled;
                    }

                    return false;
                }), new MenuActionCallback(() => { item.RaiseClick(); }));

                if (item.Menu?.Items.Count > 0 || isMainMenu)
                {
                    var subMenu = CreateSubmenu(item.Menu?.Items);

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

                    if (item.Gesture != null)
                    {
                        using (var buffer = new Utf8Buffer(item.Gesture.Key.ToString().ToLower()))
                        {
                            menuItem.SetGesture(buffer.DangerousGetHandle(), (AvnInputModifiers)item.Gesture.KeyModifiers);
                        }
                    }
                }

                menu.AddItem(menuItem);
            }
        }

        private void SetMenu(IAvnWindow avnWindow, ICollection<NativeMenuItem> menuItems)
        {
            if (_prependAppMenu)
            {
                var menu = NativeMenu.GetMenu(Application.Current);

                var items = menuItems.ToList();

                items.InsertRange(0, menu.Items);

                menuItems = items;
            }

            var appMenu = avnWindow.ObtainMainMenu();

            if(appMenu is null)
            {
                appMenu = _factory.CreateMenu();

                avnWindow.SetMainMenu(appMenu);
            }

            appMenu.Clear();

            AddItemsToMenu(appMenu, menuItems);
        }
    }
}
