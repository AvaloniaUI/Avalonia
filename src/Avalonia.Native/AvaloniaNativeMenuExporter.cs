using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Platform;
using Avalonia.Dialogs;
using Avalonia.Native.Interop;
using Avalonia.Threading;

namespace Avalonia.Native
{
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
            _menu = menu == null ? new NativeMenu() : menu;

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

            var menuItem = menu.Parent;

            if (menu.Parent is null)
            {
                menuItem = new NativeMenuItem();

                menuItem.Parent = new NativeMenu();
            }

            menuItem.Menu = menu;

            appMenu.Update(this, _factory, menuItem.Parent);
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
