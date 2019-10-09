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
        private List<NativeMenuItem> _menuItems = new List<NativeMenuItem>();

        public AvaloniaNativeMenuExporter(IAvnWindow nativeWindow, IAvaloniaNativeFactory factory)
        {
            _factory = factory;
            _nativeWindow = nativeWindow;

            DoLayoutReset();
        }

        public AvaloniaNativeMenuExporter(IAvaloniaNativeFactory factory)
        {
            _factory = factory;

            _menu = NativeMenu.GetMenu(Application.Current);
            DoLayoutReset();
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

        private void OnItemPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            QueueReset();
        }

        private void OnMenuItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            QueueReset();
        }

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

            if(_nativeWindow is null)
            {
                _menu = NativeMenu.GetMenu(Application.Current);

                if(_menu != null)
                {
                    SetMenu(_menu);
                }
            }
            else
            {
                SetMenu(_nativeWindow, _menu?.Items);
            }

            _exported = true;
        }

        private void QueueReset()
        {
            if (_resetQueued)
                return;
            _resetQueued = true;
            Dispatcher.UIThread.Post(DoLayoutReset, DispatcherPriority.Background);
        }

        private IAvnAppMenu CreateSubmenu(ICollection<NativeMenuItemBase> children)
        {
            var menu = _factory.CreateMenu();

            SetChildren(menu, children);

            return menu;
        }

        private void AddMenuItem(NativeMenuItem item)
        {
            if (item.Menu?.Items != null)
            {
                ((INotifyCollectionChanged)item.Menu.Items).CollectionChanged += OnMenuItemsChanged;
            }
        }

        private void SetChildren(IAvnAppMenu menu, ICollection<NativeMenuItemBase> children)
        {
            foreach (var i in children)
            {
                if (i is NativeMenuItem item)
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
                else if (i is NativeMenuItemSeperator seperator)
                {
                    menu.AddItem(_factory.CreateMenuItemSeperator());
                }
            }
        }

        private void AddItemsToMenu(IAvnAppMenu menu, ICollection<NativeMenuItemBase> items, bool isMainMenu = false)
        {
            foreach (var i in items)
            {
                if (i is NativeMenuItem item)
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
                else if(i is NativeMenuItemSeperator seperator)
                {
                    menu.AddItem(_factory.CreateMenuItemSeperator());
                }
            }
        }

        private void SetMenu(NativeMenu menu)
        {
            var appMenu = _factory.ObtainAppMenu();

            if (appMenu is null)
            {
                appMenu = _factory.CreateMenu();
            }

            var menuItem = menu.Parent;

            if(menu.Parent is null)
            {
                menuItem = new NativeMenuItem();
            }

            menuItem.Menu = menu;

            appMenu.Clear();
            AddItemsToMenu(appMenu, new List<NativeMenuItemBase> { menuItem });

            _factory.SetAppMenu(appMenu);
        }

        private void SetMenu(IAvnWindow avnWindow, ICollection<NativeMenuItemBase> menuItems)
        {
            if (menuItems is null)
            {
                menuItems = new List<NativeMenuItemBase>();
            }

            var appMenu = avnWindow.ObtainMainMenu();

            if (appMenu is null)
            {
                appMenu = _factory.CreateMenu();
            }

            appMenu.Clear();
            AddItemsToMenu(appMenu, menuItems);

            avnWindow.SetMainMenu(appMenu);
        }
    }
}
