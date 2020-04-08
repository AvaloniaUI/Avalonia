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
        private AvaloniaNativeMenuExporter _exporter;

        public NativeMenuItemBase Managed { get; set; }

        internal void Update(AvaloniaNativeMenuExporter exporter, IAvaloniaNativeFactory factory, NativeMenuItem item)
        {
            _exporter = exporter;

            Managed = item;

            Managed.PropertyChanged += Item_PropertyChanged;

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

                _subMenu.Update(exporter, factory, item.Menu);
            }

            if (item.Menu == null && _subMenu != null)
            {
                _subMenu.Remove();

                // todo remove submenu.

                // needs implementing on native side also.
            }
        }

        private void Item_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            _exporter.QueueReset();
        }

        internal void Remove()
        {
            _exporter = null;
            Managed = null;
            Managed.PropertyChanged -= Item_PropertyChanged;
        }
    }

    public partial class IAvnAppMenu
    {
        private AvaloniaNativeMenuExporter _exporter;
        private NativeMenu _menu;
        private List<IAvnAppMenuItem> _menuItems = new List<IAvnAppMenuItem>();
        private Dictionary<NativeMenuItemBase, IAvnAppMenuItem> _menuItemLookup = new Dictionary<NativeMenuItemBase, IAvnAppMenuItem>();

        private void Remove(IAvnAppMenuItem item)
        {
            _menuItemLookup.Remove(item.Managed);
            _menuItems.Remove(item);
            item.Remove();

            RemoveItem(item);
        }

        internal void Remove()
        {
            ((INotifyCollectionChanged)_menu.Items).CollectionChanged -= IAvnAppMenu_CollectionChanged;
            _exporter = null;
            _menu = null;
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

        internal void Update(AvaloniaNativeMenuExporter exporter, IAvaloniaNativeFactory factory, NativeMenu menu, string title = "")
        {
            if (_menu == null)
            {
                _menu = menu;
            }
            else if (_menu != menu)
            {
                throw new Exception("Cannot update a menu from another instance");
            }

            _exporter = exporter;

            ((INotifyCollectionChanged)_menu.Items).CollectionChanged += IAvnAppMenu_CollectionChanged;

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
                    nativeItem.Update(exporter, factory, nmi);
                }
            }

            for (int i = menu.Items.Count; i < _menuItems.Count; i++)
            {
                _menuItems.Remove(_menuItems[i]);
            }
        }

        private void IAvnAppMenu_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _exporter.QueueReset();
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
        private bool _resetQueued;
        private bool _exported = false;
        private IAvnWindow _nativeWindow;
        private NativeMenu _menu;

        public AvaloniaNativeMenuExporter(IAvnWindow nativeWindow, IAvaloniaNativeFactory factory)
        {
            _factory = factory;
            _nativeWindow = nativeWindow;

            DoLayoutReset();
        }

        public AvaloniaNativeMenuExporter(IAvaloniaNativeFactory factory)
        {
            _factory = factory;

            DoLayoutReset();
        }

        public bool IsNativeMenuExported => _exported;

        public event EventHandler OnIsNativeMenuExportedChanged;

        public void SetNativeMenu(NativeMenu menu)
        {
            if (_menu == null)
                _menu = new NativeMenu();

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

        void DoLayoutReset()
        {
            _resetQueued = false;

            if (_nativeWindow is null)
            {
                var appMenu = NativeMenu.GetMenu(Application.Current);

                if (appMenu == null)
                {
                    appMenu = CreateDefaultAppMenu();
                    SetMenu(appMenu);
                }
            }
            else
            {
                SetMenu(_nativeWindow, _menu);
            }

            _exported = true;
        }

        internal void QueueReset()
        {
            if (_resetQueued)
                return;
            _resetQueued = true;
            Dispatcher.UIThread.Post(DoLayoutReset, DispatcherPriority.Background);
        }

        private void SetMenu(NativeMenu menu)
        {
            var appMenu = _factory.ObtainAppMenu();

            if (appMenu is null)
            {
                appMenu = _factory.CreateMenu();
                _factory.SetAppMenu(appMenu);
            }

            appMenu.Update(this, _factory, menu);
        }

        private void SetMenu(IAvnWindow avnWindow, NativeMenu menu)
        {
            var appMenu = avnWindow.ObtainMainMenu();

            if (appMenu is null)
            {
                appMenu = _factory.CreateMenu();
                avnWindow.SetMainMenu(appMenu);
            }

            appMenu.Update(this, _factory, menu);
        }
    }
}
