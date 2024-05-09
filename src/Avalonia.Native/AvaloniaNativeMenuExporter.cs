using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Platform;
using Avalonia.Dialogs;
using Avalonia.Input;
using Avalonia.Native.Interop;
using Avalonia.Native.Interop.Impl;
using Avalonia.Threading;

namespace Avalonia.Native
{
    internal class AvaloniaNativeMenuExporter : ITopLevelNativeMenuExporter
    {
        private readonly IAvaloniaNativeFactory _factory;
        private bool _resetQueued = true;
        private bool _exported;
        private readonly IAvnWindow _nativeWindow;
        private NativeMenu _menu;
        private __MicroComIAvnMenuProxy _nativeMenu;
        private readonly IAvnTrayIcon _trayIcon;
        private readonly IAvnApplicationCommands _applicationCommands;

        public AvaloniaNativeMenuExporter(IAvnWindow nativeWindow, IAvaloniaNativeFactory factory)
        {
            _factory = factory;
            _nativeWindow = nativeWindow;
            _applicationCommands = _factory.CreateApplicationCommands();

            DoLayoutReset();
        }

        public AvaloniaNativeMenuExporter(IAvaloniaNativeFactory factory)
        {
            _factory = factory;
            _applicationCommands = _factory.CreateApplicationCommands();

            DoLayoutReset();
        }

        public AvaloniaNativeMenuExporter(IAvnTrayIcon trayIcon, IAvaloniaNativeFactory factory)
        {
            _factory = factory;
            _trayIcon = trayIcon;
            _applicationCommands = _factory.CreateApplicationCommands();

            DoLayoutReset();
        }

        public bool IsNativeMenuExported => _exported;

        public event EventHandler OnIsNativeMenuExportedChanged { add { } remove { } }

        public void SetNativeMenu(NativeMenu menu)
        {
            _menu = menu ?? new NativeMenu();
            DoLayoutReset(true);
        }

        internal void UpdateIfNeeded()
        {
            if (_resetQueued)
            {
                DoLayoutReset();
            }
        }

        private static NativeMenu CreateDefaultAppMenu()
        {
            var result = new NativeMenu();

            var aboutItem = new NativeMenuItem("About Avalonia");
            
            aboutItem.Click += async (_, _) =>
            {
                var dialog = new AboutAvaloniaDialog();

                if (Application.Current is
                    { ApplicationLifetime: IClassicDesktopStyleApplicationLifetime { MainWindow: { } mainWindow } })
                {
                    await dialog.ShowDialog(mainWindow);   
                }
            };
            
            result.Add(aboutItem);

            return result;
        }

        private void PopulateStandardOSXMenuItems(NativeMenu appMenu)
        {
            appMenu.Add(new NativeMenuItemSeparator());

            var servicesMenu = new NativeMenuItem("Services");
            servicesMenu.Menu = new NativeMenu { [MacOSNativeMenuCommands.IsServicesSubmenuProperty] = true };

            appMenu.Add(servicesMenu);

            appMenu.Add(new NativeMenuItemSeparator());

            var hideItem = new NativeMenuItem("Hide " + (Application.Current?.Name ?? "Application"))
            {
                Gesture = new KeyGesture(Key.H, KeyModifiers.Meta)
            };

            hideItem.Click += (_, _) =>
            {
                _applicationCommands.HideApp();
            };

            appMenu.Add(hideItem);

            var hideOthersItem = new NativeMenuItem("Hide Others")
            {
                Gesture = new KeyGesture(Key.Q, KeyModifiers.Meta | KeyModifiers.Alt)
            };
            hideOthersItem.Click += (_, _) =>
            {
                _applicationCommands.HideOthers();
            };
            appMenu.Add(hideOthersItem);

            var showAllItem = new NativeMenuItem("Show All");
            showAllItem.Click += (_, _) =>
            {
                _applicationCommands.ShowAll();
            };

            appMenu.Add(showAllItem);

            appMenu.Add(new NativeMenuItemSeparator());

            var quitItem = new NativeMenuItem("Quit") { Gesture = new KeyGesture(Key.Q, KeyModifiers.Meta) };
            quitItem.Click += (_, _) =>
            {
                if (Application.Current is { ApplicationLifetime: IClassicDesktopStyleApplicationLifetime lifetime })
                {
                    lifetime.TryShutdown();
                }
                else if(Application.Current is {ApplicationLifetime: IControlledApplicationLifetime controlledLifetime})
                {
                    controlledLifetime.Shutdown();
                }
            };

            appMenu.Add(quitItem);
        }

        private void DoLayoutReset() => DoLayoutReset(false);

        private void DoLayoutReset(bool forceUpdate = false)
        {
            var macOpts = AvaloniaLocator.Current.GetService<MacOSPlatformOptions>() ?? new MacOSPlatformOptions();

            if (macOpts.DisableNativeMenus)
            {
                return;
            }

            if (_resetQueued || forceUpdate)
            {
                _resetQueued = false;

                if (_nativeWindow is null)
                {
                    if (_trayIcon is null)
                    {
                        var appMenu = NativeMenu.GetMenu(Application.Current);

                        if (appMenu == null)
                        {
                            appMenu = CreateDefaultAppMenu();
                            NativeMenu.SetMenu(Application.Current, appMenu);
                        }

                        SetMenu(appMenu);
                    }
                    else if (_menu != null)
                    {
                        SetMenu(_trayIcon, _menu);
                    }
                }
                else
                {
                    if (_menu != null)
                    {
                        SetMenu(_nativeWindow, _menu);
                    }
                }

                _exported = true;
            }
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
            var menuItem = menu.Parent;

            var appMenuHolder = menuItem?.Parent;

            if (menuItem is null)
            {
                menuItem = new NativeMenuItem();
            }

            if (appMenuHolder is null)
            {
                appMenuHolder = new NativeMenu();

                appMenuHolder.Add(menuItem);
            }

            menuItem.Menu = menu;

            var setMenu = false;

            if (_nativeMenu is null)
            {
                _nativeMenu = __MicroComIAvnMenuProxy.Create(_factory);

                _nativeMenu.Initialize(this, appMenuHolder, "");

                var macOpts = AvaloniaLocator.Current.GetService<MacOSPlatformOptions>();

                if (macOpts == null || !macOpts.DisableDefaultApplicationMenuItems)
                {
                    PopulateStandardOSXMenuItems(menu);
                }

                setMenu = true;
            }

            _nativeMenu.Update(_factory, appMenuHolder);

            if (setMenu)
            {
                _factory.SetAppMenu(_nativeMenu);
            }
        }

        private void SetMenu(IAvnWindow avnWindow, NativeMenu menu)
        {
            var setMenu = false;

            if (_nativeMenu is null)
            {
                _nativeMenu = __MicroComIAvnMenuProxy.Create(_factory);

                _nativeMenu.Initialize(this, menu, "");

                setMenu = true;
            }

            _nativeMenu.Update(_factory, menu);

            if(setMenu)
            {
                avnWindow.SetMainMenu(_nativeMenu);
            }
        }

        private void SetMenu(IAvnTrayIcon trayIcon, NativeMenu menu)
        {
            var setMenu = false;

            if (_nativeMenu is null)
            {
                _nativeMenu = __MicroComIAvnMenuProxy.Create(_factory);

                _nativeMenu.Initialize(this, menu, "");

                setMenu = true;
            }

            _nativeMenu.Update(_factory, menu);

            if(setMenu)
            {
                trayIcon.SetMenu(_nativeMenu);
            }
        }
    }
}
