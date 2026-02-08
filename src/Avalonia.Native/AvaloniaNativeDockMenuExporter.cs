using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Native.Interop;
using Avalonia.Native.Interop.Impl;
using Avalonia.Reactive;
using Avalonia.Threading;

namespace Avalonia.Native
{
    internal class AvaloniaNativeDockMenuExporter : INativeMenuExporter
    {
        private readonly IAvaloniaNativeFactory _factory;
        private bool _resetQueued;
        private NativeMenu? _menu;
        private __MicroComIAvnMenuProxy? _nativeMenu;

        public AvaloniaNativeDockMenuExporter(IAvaloniaNativeFactory factory)
        {
            _factory = factory;

            var macOpts = AvaloniaLocator.Current.GetService<MacOSPlatformOptions>() ?? new MacOSPlatformOptions();

            if (macOpts.DisableNativeMenus)
            {
                return;
            }

            NativeMenu.DockMenuProperty.Changed.Subscribe(args =>
            {
                if (args.Sender is Application)
                {
                    SetNativeMenu(args.NewValue.GetValueOrDefault());
                }
            });

            var app = Application.Current;
            if (app is not null)
            {
                var dockMenu = NativeMenu.GetDockMenu(app);
                if (dockMenu is not null)
                {
                    SetNativeMenu(dockMenu);
                }
            }
        }

        public void SetNativeMenu(NativeMenu? menu)
        {
            _menu = menu;

            if (_menu is not null)
            {
                DoLayoutReset(true);
            }
        }

        public void UpdateIfNeeded()
        {
            if (_resetQueued)
            {
                DoLayoutReset();
            }
        }

        public void QueueReset()
        {
            if (_resetQueued)
                return;
            _resetQueued = true;
            Dispatcher.UIThread.Post(DoLayoutReset, DispatcherPriority.Background);
        }

        private void DoLayoutReset() => DoLayoutReset(false);

        private void DoLayoutReset(bool forceUpdate)
        {
            var macOpts = AvaloniaLocator.Current.GetService<MacOSPlatformOptions>() ?? new MacOSPlatformOptions();

            if (macOpts.DisableNativeMenus)
            {
                return;
            }

            if (_resetQueued || forceUpdate)
            {
                _resetQueued = false;

                if (_menu is null)
                {
                    return;
                }

                var setMenu = false;

                if (_nativeMenu is null)
                {
                    _nativeMenu = __MicroComIAvnMenuProxy.Create(_factory);

                    _nativeMenu.Initialize(QueueReset, UpdateIfNeeded, _menu, "");

                    setMenu = true;
                }

                _nativeMenu.Update(_factory, _menu);

                if (setMenu)
                {
                    _factory.SetDockMenu(_nativeMenu);
                }
            }
        }
    }
}
