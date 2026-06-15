using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.FreeDesktop.AtSpi;
using Avalonia.Logging;
using Avalonia.Threading;

namespace Avalonia.X11
{
    internal sealed class X11AtSpiAccessibility
    {
        private readonly AvaloniaX11Platform _platform;
        private readonly List<X11Window> _trackedWindows = new();

        private AtSpiAccessibilityWatcher? _watcher;
        private AtSpiServer? _server;
        private bool _serverStartedUnconditionally;

        internal X11AtSpiAccessibility(AvaloniaX11Platform platform)
        {
            _platform = platform;
        }

        internal AtSpiServer? Server => _server;

        internal void Initialize()
        {
            _watcher = new AtSpiAccessibilityWatcher();
            _ = InitializeAsync();
        }

        internal void TrackWindow(X11Window window) => _trackedWindows.Add(window);
        internal void UntrackWindow(X11Window window) => _trackedWindows.Remove(window);

        private async Task InitializeAsync()
        {
            try
            {
                await WaitForUiThreadSettleAsync();

                // Path A: try unconditional connection first (GTK4 approach).
                // This avoids delaying startup on watcher/session-bus property calls.
                if (await TryStartServerAsync())
                {
                    _serverStartedUnconditionally = true;
                    return;
                }

                // Path A failed - fall back to watcher-driven enablement.
                await _watcher!.InitAsync();
                _watcher.IsEnabledChanged += OnAccessibilityEnabledChanged;
                if (_watcher.IsEnabled)
                    await EnableAccessibilityAsync();
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.X11Platform)?
                    .Log(_platform, "AT-SPI initialization failed and will be disabled: {Exception}", e);
            }
        }

        private async Task WaitForUiThreadSettleAsync()
        {
            try
            {
                // Wait until UI work is drained to context-idle so AT-SPI handlers
                // are responsive when clients start querying immediately after embed.
                var settle = Dispatcher.UIThread
                    .InvokeAsync(() => { }, DispatcherPriority.ContextIdle)
                    .GetTask();

                // Keep startup bounded in case the UI thread never reaches idle
                // (e.g., continuous high-priority work).
                await settle.WaitAsync(TimeSpan.FromMilliseconds(100));
            }
            catch (TimeoutException e)
            {
                Logger.TryGet(LogEventLevel.Debug, LogArea.X11Platform)?
                    .Log(_platform, "AT-SPI startup wait timed out before UI thread reached idle: {Exception}", e);
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Debug, LogArea.X11Platform)?
                    .Log(_platform, "AT-SPI startup wait failed, continuing without idle settle: {Exception}", e);
            }
        }

        private async void OnAccessibilityEnabledChanged(object? sender, bool enabled)
        {
            try
            {
                if (enabled)
                {
                    await EnableAccessibilityAsync();
                }
                else if (!_serverStartedUnconditionally)
                {
                    // Only tear down if server wasn't started unconditionally.
                    // When started unconditionally, event listener tracking handles suppression.
                    await DisableAccessibilityAsync();
                }
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.X11Platform)?
                    .Log(_platform, "AT-SPI dynamic enable/disable toggle failed: {Exception}", e);
            }
        }

        private async Task<bool> TryStartServerAsync()
        {
            if (_server is not null)
                return true;

            try
            {
                var server = new AtSpiServer();
                await server.StartAsync();
                _server = server;

                // Register any already-tracked windows.
                foreach (var window in _trackedWindows)
                {
                    var peer = TryGetWindowPeer(window);
                    if (peer is not null)
                        server.AddWindow(peer);
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.X11Platform)?
                    .Log(_platform, "AT-SPI server startup attempt failed: {Exception}", e);
                return false;
            }
        }

        private static AutomationPeer? TryGetWindowPeer(X11Window window)
        {
            try
            {
                if (window.InputRoot.FocusRoot is Control control)
                {
                    var peer = ControlAutomationPeer.CreatePeerForElement(control);
                    return peer?.GetAutomationRoot() ?? peer;
                }
            }
            catch (Exception e)
            {
                // Window can be tracked before input root is available.
                Logger.TryGet(LogEventLevel.Debug, LogArea.X11Platform)?
                    .Log(window, "AT-SPI could not resolve window automation peer yet: {Exception}", e);
            }

            return null;
        }

        private async Task EnableAccessibilityAsync()
        {
            await TryStartServerAsync();
        }

        private async Task DisableAccessibilityAsync()
        {
            if (_server is null)
                return;

            var server = _server;
            _server = null;
            await server.DisposeAsync();
        }
    }
}
