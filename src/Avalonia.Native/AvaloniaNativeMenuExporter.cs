using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Native.Interop;
using Avalonia.Platform.Interop;
using Avalonia.Threading;
using Avalonia.Dialogs;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Native
{
    enum OsxUnicodeSpecialKey
    {
        NSUpArrowFunctionKey = 0xF700,
        NSDownArrowFunctionKey = 0xF701,
        NSLeftArrowFunctionKey = 0xF702,
        NSRightArrowFunctionKey = 0xF703,
        NSF1FunctionKey = 0xF704,
        NSF2FunctionKey = 0xF705,
        NSF3FunctionKey = 0xF706,
        NSF4FunctionKey = 0xF707,
        NSF5FunctionKey = 0xF708,
        NSF6FunctionKey = 0xF709,
        NSF7FunctionKey = 0xF70A,
        NSF8FunctionKey = 0xF70B,
        NSF9FunctionKey = 0xF70C,
        NSF10FunctionKey = 0xF70D,
        NSF11FunctionKey = 0xF70E,
        NSF12FunctionKey = 0xF70F,
        NSF13FunctionKey = 0xF710,
        NSF14FunctionKey = 0xF711,
        NSF15FunctionKey = 0xF712,
        NSF16FunctionKey = 0xF713,
        NSF17FunctionKey = 0xF714,
        NSF18FunctionKey = 0xF715,
        NSF19FunctionKey = 0xF716,
        NSF20FunctionKey = 0xF717,
        NSF21FunctionKey = 0xF718,
        NSF22FunctionKey = 0xF719,
        NSF23FunctionKey = 0xF71A,
        NSF24FunctionKey = 0xF71B,
        NSF25FunctionKey = 0xF71C,
        NSF26FunctionKey = 0xF71D,
        NSF27FunctionKey = 0xF71E,
        NSF28FunctionKey = 0xF71F,
        NSF29FunctionKey = 0xF720,
        NSF30FunctionKey = 0xF721,
        NSF31FunctionKey = 0xF722,
        NSF32FunctionKey = 0xF723,
        NSF33FunctionKey = 0xF724,
        NSF34FunctionKey = 0xF725,
        NSF35FunctionKey = 0xF726,
        NSInsertFunctionKey = 0xF727,
        NSDeleteFunctionKey = 0xF728,
        NSHomeFunctionKey = 0xF729,
        NSBeginFunctionKey = 0xF72A,
        NSEndFunctionKey = 0xF72B,
        NSPageUpFunctionKey = 0xF72C,
        NSPageDownFunctionKey = 0xF72D,
        NSPrintScreenFunctionKey = 0xF72E,
        NSScrollLockFunctionKey = 0xF72F,
        NSPauseFunctionKey = 0xF730,
        NSSysReqFunctionKey = 0xF731,
        NSBreakFunctionKey = 0xF732,
        NSResetFunctionKey = 0xF733,
        NSStopFunctionKey = 0xF734,
        NSMenuFunctionKey = 0xF735,
        NSUserFunctionKey = 0xF736,
        NSSystemFunctionKey = 0xF737,
        NSPrintFunctionKey = 0xF738,
        NSClearLineFunctionKey = 0xF739,
        NSClearDisplayFunctionKey = 0xF73A,
        NSInsertLineFunctionKey = 0xF73B,
        NSDeleteLineFunctionKey = 0xF73C,
        NSInsertCharFunctionKey = 0xF73D,
        NSDeleteCharFunctionKey = 0xF73E,
        NSPrevFunctionKey = 0xF73F,
        NSNextFunctionKey = 0xF740,
        NSSelectFunctionKey = 0xF741,
        NSExecuteFunctionKey = 0xF742,
        NSUndoFunctionKey = 0xF743,
        NSRedoFunctionKey = 0xF744,
        NSFindFunctionKey = 0xF745,
        NSHelpFunctionKey = 0xF746,
        NSModeSwitchFunctionKey = 0xF747
    }

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

        private static Dictionary<Key, OsxUnicodeSpecialKey> osxKeys = new Dictionary<Key, OsxUnicodeSpecialKey>
        {
            {Key.Up, OsxUnicodeSpecialKey.NSUpArrowFunctionKey },
            {Key.Down, OsxUnicodeSpecialKey.NSDownArrowFunctionKey },
            {Key.Left, OsxUnicodeSpecialKey.NSLeftArrowFunctionKey },
            {Key.Right, OsxUnicodeSpecialKey.NSRightArrowFunctionKey },
            { Key.F1, OsxUnicodeSpecialKey.NSF1FunctionKey },
            { Key.F2, OsxUnicodeSpecialKey.NSF2FunctionKey },
            { Key.F3, OsxUnicodeSpecialKey.NSF3FunctionKey },
            { Key.F4, OsxUnicodeSpecialKey.NSF4FunctionKey },
            { Key.F5, OsxUnicodeSpecialKey.NSF5FunctionKey },
            { Key.F6, OsxUnicodeSpecialKey.NSF6FunctionKey },
            { Key.F7, OsxUnicodeSpecialKey.NSF7FunctionKey },
            { Key.F8, OsxUnicodeSpecialKey.NSF8FunctionKey },
            { Key.F9, OsxUnicodeSpecialKey.NSF9FunctionKey },
            { Key.F10, OsxUnicodeSpecialKey.NSF10FunctionKey },
            { Key.F11, OsxUnicodeSpecialKey.NSF11FunctionKey },
            { Key.F12, OsxUnicodeSpecialKey.NSF12FunctionKey },
            { Key.F13, OsxUnicodeSpecialKey.NSF13FunctionKey },
            { Key.F14, OsxUnicodeSpecialKey.NSF14FunctionKey },
            { Key.F15, OsxUnicodeSpecialKey.NSF15FunctionKey },
            { Key.F16, OsxUnicodeSpecialKey.NSF16FunctionKey },
            { Key.F17, OsxUnicodeSpecialKey.NSF17FunctionKey },
            { Key.F18, OsxUnicodeSpecialKey.NSF18FunctionKey },
            { Key.F19, OsxUnicodeSpecialKey.NSF19FunctionKey },
            { Key.F20, OsxUnicodeSpecialKey.NSF20FunctionKey },
            { Key.F21, OsxUnicodeSpecialKey.NSF21FunctionKey },
            { Key.F22, OsxUnicodeSpecialKey.NSF22FunctionKey },
            { Key.F23, OsxUnicodeSpecialKey.NSF23FunctionKey },
            { Key.F24, OsxUnicodeSpecialKey.NSF24FunctionKey },
            { Key.Insert, OsxUnicodeSpecialKey.NSInsertFunctionKey },
            { Key.Delete, OsxUnicodeSpecialKey.NSDeleteFunctionKey },
            { Key.Home, OsxUnicodeSpecialKey.NSHomeFunctionKey },
            //{ Key.Begin, OsxUnicodeSpecialKey.NSBeginFunctionKey },
            { Key.End, OsxUnicodeSpecialKey.NSEndFunctionKey },
            { Key.PageUp, OsxUnicodeSpecialKey.NSPageUpFunctionKey },
            { Key.PageDown, OsxUnicodeSpecialKey.NSPageDownFunctionKey },
            { Key.PrintScreen, OsxUnicodeSpecialKey.NSPrintScreenFunctionKey },
            { Key.Scroll, OsxUnicodeSpecialKey.NSScrollLockFunctionKey },
            //{ Key.SysReq, OsxUnicodeSpecialKey.NSSysReqFunctionKey },
            //{ Key.Break, OsxUnicodeSpecialKey.NSBreakFunctionKey },
            //{ Key.Reset, OsxUnicodeSpecialKey.NSResetFunctionKey },
            //{ Key.Stop, OsxUnicodeSpecialKey.NSStopFunctionKey },
            //{ Key.Menu, OsxUnicodeSpecialKey.NSMenuFunctionKey },
            //{ Key.UserFunction, OsxUnicodeSpecialKey.NSUserFunctionKey },
            //{ Key.SystemFunction, OsxUnicodeSpecialKey.NSSystemFunctionKey },
            { Key.Print, OsxUnicodeSpecialKey.NSPrintFunctionKey },
            //{ Key.ClearLine, OsxUnicodeSpecialKey.NSClearLineFunctionKey },
            //{ Key.ClearDisplay, OsxUnicodeSpecialKey.NSClearDisplayFunctionKey },
        };

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

            if(_nativeWindow is null)
            {
                _menu = NativeMenu.GetMenu(Application.Current);

                if(_menu != null)
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

        private static string ConvertOSXSpecialKeyCodes(Key key)
        {
            if (osxKeys.ContainsKey(key))
            {
                return ((char)osxKeys[key]).ToString();
            }
            else
            {
                return key.ToString().ToLower();
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
                        using (var buffer = new Utf8Buffer(ConvertOSXSpecialKeyCodes(item.Gesture.Key)))
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

                    if (item.Menu?.Items?.Count >= 0)
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

                    if (item.Menu?.Items.Count >= 0 || isMainMenu)
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
