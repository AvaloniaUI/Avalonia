using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Platform;
using Avalonia.Dialogs;
using Avalonia.Native.Interop;
using Avalonia.Platform.Interop;
using Avalonia.Threading;

namespace Avalonia.Native.Interop
{
    public partial class IAvnAppMenuItem
    {
        private IAvnAppMenu _subMenu;

        public NativeMenuItemBase Managed { get; set; }

        public void Update(IAvaloniaNativeFactory factory, NativeMenuItem item)
        {
            using (var buffer = new Utf8Buffer(item.Header))
            {
                Title = buffer.DangerousGetHandle();
            }

            if (item.Gesture != null)
            {
                using (var buffer = new Utf8Buffer(OsxUnicodeKeys.ConvertOSXSpecialKeyCodes(item.Gesture.Key)))
                {
                    SetGesture(buffer.DangerousGetHandle(), (AvnInputModifiers)item.Gesture.KeyModifiers);
                }
            }

            SetAction(new PredicateCallback(() =>
            {
                if (item.Command != null || item.HasClickHandlers)
                {
                    return item.Enabled;
                }

                return false;
            }), new MenuActionCallback(() => { item.RaiseClick(); }));

            if (item.Menu != null)
            {
                if (_subMenu == null)
                {
                    _subMenu = factory.CreateMenu();
                }

                _subMenu.Update(factory, item.Menu);
            }

            if (item.Menu == null && _subMenu != null)
            {
                // todo remove submenu.

                // needs implementing on native side also.
            }
        }
    }

    public partial class IAvnAppMenu
    {
        private NativeMenu _menu;
        private List<IAvnAppMenuItem> _menuItems = new List<IAvnAppMenuItem>();
        private Dictionary<NativeMenuItemBase, IAvnAppMenuItem> _menuItemLookup = new Dictionary<NativeMenuItemBase, IAvnAppMenuItem>();

        private void Remove(IAvnAppMenuItem item)
        {
            _menuItemLookup.Remove(item.Managed);
            _menuItems.Remove(item);

            RemoveItem(item);
        }

        private void InsertAt(int index, IAvnAppMenuItem item)
        {
            if (item.Managed == null)
            {
                throw new InvalidOperationException("Cannot insert item that with Managed link null");
            }

            _menuItemLookup.Add(item.Managed, item);
            _menuItems.Insert(index, item);

            AddItem(item); // todo change to insertatimpl
        }

        private IAvnAppMenuItem CreateNew(IAvaloniaNativeFactory factory, NativeMenuItemBase item)
        {
            var nativeItem = item is NativeMenuItemSeperator ? factory.CreateMenuItemSeperator() : factory.CreateMenuItem();
            nativeItem.Managed = item;

            return nativeItem;
        }

        public void Update(IAvaloniaNativeFactory factory, NativeMenu menu, string title = "")
        {
            if (_menu == null)
            {
                _menu = menu;
            }
            else if (_menu != menu)
            {
                throw new Exception("Cannot update a menu from another instance");
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                using (var buffer = new Utf8Buffer(title))
                {
                    Title = buffer.DangerousGetHandle();
                }
            }

            for (int i = 0; i < menu.Items.Count; i++)
            {
                IAvnAppMenuItem nativeItem = null;
                if (i >= _menuItems.Count || menu.Items[i] != _menuItems[i].Managed)
                {
                    if (_menuItemLookup.TryGetValue(menu.Items[i], out nativeItem))
                    {
                        Remove(nativeItem);
                        InsertAt(i, nativeItem);
                    }
                    else
                    {
                        nativeItem = CreateNew(factory, menu.Items[i]);
                        InsertAt(i, nativeItem);
                    }
                }

                if (menu.Items[i] is NativeMenuItem nmi)
                {
                    nativeItem.Update(factory, nmi);
                }
            }

            for (int i = menu.Items.Count; i < _menuItems.Count; i++)
            {
                _menuItems.Remove(_menuItems[i]);
            }
        }
    }
}

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

        private static NativeMenu CreateDefaultAppMenu()
        {
            var result = new NativeMenu();

            var aboutItem = new NativeMenuItem
            {
                Header = "About Avalonia",
            };

            aboutItem.Clicked += async (sender, e) =>
            {
                var dialog = new AboutAvaloniaDialog();

                var mainWindow = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

                await dialog.ShowDialog(mainWindow);
            };

            result.Add(aboutItem);

            return result;
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

            if (_nativeWindow is null)
            {
                _menu = NativeMenu.GetMenu(Application.Current);

                if (_menu != null)
                {
                    SetMenu(_menu);
                }
                else
                {
                    SetMenu(CreateDefaultAppMenu());
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

            //SetChildren(menu, children);

            return menu;
        }

        private void AddMenuItem(NativeMenuItem item)
        {
            if (item.Menu?.Items != null)
            {
                ((INotifyCollectionChanged)item.Menu.Items).CollectionChanged += OnMenuItemsChanged;
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

            if (menu.Parent is null)
            {
                menuItem = new NativeMenuItem();
            }

            menuItem.Menu = menu;

            //appMenu.Clear();
            //AddItemsToMenu(appMenu, new List<NativeMenuItemBase> { menuItem });

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

            //appMenu.Clear();
            //AddItemsToMenu(appMenu, menuItems);

            avnWindow.SetMainMenu(appMenu);
        }
    }
}
