using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Reactive;
using Avalonia.Threading;

namespace Avalonia.Controls.ApplicationLifetimes
{
    public class ClassicDesktopStyleApplicationLifetime :
        IClassicDesktopStyleApplicationLifetime,
        ISetupApplicationLifetime,
        IDisposable
    {
        private int _exitCode;
        private CancellationTokenSource? _cts;
        private bool _isShuttingDown;
        private readonly AvaloniaList<Window> _windows = new();
        private CompositeDisposable? _globalEventsSubscriptions;
        private bool _beforeInitCalled;
        private bool _afterInitCalled;
        private IPlatformLifetimeEventsImpl? _platformLifetimeEventsImpl;

        /// <inheritdoc/>
        public event EventHandler<ControlledApplicationLifetimeStartupEventArgs>? Startup;

        /// <inheritdoc/>
        public event EventHandler<ShutdownRequestedEventArgs>? ShutdownRequested;

        /// <inheritdoc/>
        public event EventHandler<ControlledApplicationLifetimeExitEventArgs>? Exit;

        /// <summary>
        /// Gets the arguments passed to the AppBuilder Start method.
        /// </summary>
        public string[]? Args { get; set; }
        
        /// <inheritdoc/>
        public ShutdownMode ShutdownMode { get; set; }
        
        /// <inheritdoc/>
        public Window? MainWindow { get; set; }

        /// <inheritdoc />
        public IReadOnlyList<Window> Windows => _windows;

        private void HandleWindowClosed(Window? window)
        {
            if (window == null)
                return;
            
            if (_isShuttingDown)
                return;

            if (ShutdownMode == ShutdownMode.OnLastWindowClose && _windows.Count == 0)
                TryShutdown();
            else if (ShutdownMode == ShutdownMode.OnMainWindowClose && ReferenceEquals(window, MainWindow))
                TryShutdown();
        }

        public void Shutdown(int exitCode = 0)
        {
            DoShutdown(new ShutdownRequestedEventArgs(), true, true, exitCode);
        }

        public bool TryShutdown(int exitCode = 0)
        {
            return DoShutdown(new ShutdownRequestedEventArgs(), true, false, exitCode);
        }

        private void SubscribeGlobalEvents()
        {
            Debug.Assert(_globalEventsSubscriptions is null);

            _globalEventsSubscriptions = new CompositeDisposable(
                Window.WindowOpenedEvent.AddClassHandler(typeof(Window), (sender, _) =>
                {
                    var window = (Window)sender!;
                    if (!_windows.Contains(window))
                    {
                        _windows.Add(window);
                    }
                }),
                Window.WindowClosedEvent.AddClassHandler(typeof(Window), (sender, _) =>
                {
                    var window = (Window)sender!;
                    _windows.Remove(window);
                    HandleWindowClosed(window);
                }));            
        }

        private void BeforeInit()
        {
            if (_beforeInitCalled)
                return;

            _beforeInitCalled = true;
            SubscribeGlobalEvents();
        }

        void ISetupApplicationLifetime.BeforeAppInit()
            => BeforeInit();

        private void AfterInit()
        {
            if (_afterInitCalled)
                return;

            _afterInitCalled = true;

            _platformLifetimeEventsImpl = AvaloniaLocator.Current.GetService<IPlatformLifetimeEventsImpl>();
            _platformLifetimeEventsImpl?.ShutdownRequested += OnShutdownRequested;
        }

        void ISetupApplicationLifetime.AfterAppInit()
        {
            AfterInit();
        }

        public int Start(string[] args)
        {
            return StartCore(args);
        }

        /// <summary>
        /// Since the lifetime must be set up/prepared with 'args' before executing Start(), an overload with no parameters seems more suitable for integrating with some lifetime manager providers, such as MS HostApplicationBuilder.
        /// </summary>
        /// <returns>exit code</returns>
        public int Start()
        {
            return StartCore(Args ?? Array.Empty<string>());
        }

        internal int StartCore(string[] args)
        {
            // Before/AfterInit should have been called from ISetupApplicationLifetime.
            // If somehow they weren't (e.g., for a manually started lifetime), do it now.
            BeforeInit();
            AfterInit();
            Startup?.Invoke(this, new ControlledApplicationLifetimeStartupEventArgs(args));

            _cts = new CancellationTokenSource();

            // Note due to a bug in the JIT we wrap this in a method, otherwise MainWindow
            // gets stuffed into a local var and can not be GCed until after the program stops.
            // this method never exits until program end.
            ShowMainWindow();

            Dispatcher.UIThread.MainLoop(_cts.Token);
            Environment.ExitCode = _exitCode;
            return _exitCode;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ShowMainWindow()
        {
            MainWindow?.Show();
        }

        public void Dispose()
        {
            _globalEventsSubscriptions?.Dispose();
            _globalEventsSubscriptions = null;

            _platformLifetimeEventsImpl?.ShutdownRequested -= OnShutdownRequested;
            _platformLifetimeEventsImpl = null;
        }

        private bool DoShutdown(
            ShutdownRequestedEventArgs e,
            bool isProgrammatic,
            bool force = false,
            int exitCode = 0)
        {
            if (!force)
            {
                ShutdownRequested?.Invoke(this, e);

                if (e.Cancel)
                    return false;

                if (_isShuttingDown)
                    throw new InvalidOperationException("Application is already shutting down.");
            }

            _exitCode = exitCode;
            _isShuttingDown = true;
            var shutdownCancelled = false;

            try
            {
                // When an OS shutdown request is received, try to close all non-owned windows. Windows can cancel
                // shutdown by setting e.Cancel = true in the Closing event. Owned windows will be shutdown by their
                // owners.
                foreach (var w in new List<Window>(_windows))
                {
                    if (w.Owner is null)
                    {
                        var ignoreCancel = force || (ShutdownMode == ShutdownMode.OnMainWindowClose && w != MainWindow);
                        var reason = e.IsOSShutdown ?
                            WindowCloseReason.OSShutdown :
                            WindowCloseReason.ApplicationShutdown;
                        w.CloseCore(reason, isProgrammatic, ignoreCancel);
                    }
                }

                if (!force && Windows.Count > 0)
                {
                    e.Cancel = true;
                    shutdownCancelled = true;
                    return false;
                }

                var args = new ControlledApplicationLifetimeExitEventArgs(exitCode);
                Exit?.Invoke(this, args);
                _exitCode = args.ApplicationExitCode;                
            }
            finally
            {
                _isShuttingDown = false;

                if (!shutdownCancelled)
                {
                    _cts?.Cancel();
                    _cts = null;
                    Dispatcher.UIThread.InvokeShutdown();
                }
            }

            return true;
        }
        
        private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e) => DoShutdown(e, false);
    }
}

namespace Avalonia
{
    /// <summary>
    /// IClassicDesktopStyleApplicationLifetime related AppBuilder extensions.
    /// </summary>
    public static class ClassicDesktopStyleApplicationLifetimeExtensions
    {
        private static ClassicDesktopStyleApplicationLifetime CreateLifetime(
            string[] args,
            Action<IClassicDesktopStyleApplicationLifetime>? lifetimeBuilder)
        {
            var lifetime = new ClassicDesktopStyleApplicationLifetime { Args = args };
            lifetimeBuilder?.Invoke(lifetime);
            return lifetime;
        }

        /// <summary>
        /// Setups the Application with a IClassicDesktopStyleApplicationLifetime, but doesn't show the main window and doesn't run application main loop.
        /// </summary>
        /// <param name="builder">Application builder.</param>
        /// <param name="args">Startup arguments.</param>
        /// <param name="lifetimeBuilder">Lifetime builder to modify the lifetime before application started.</param>
        /// <returns>Exit code.</returns>
        public static AppBuilder SetupWithClassicDesktopLifetime(this AppBuilder builder, string[] args,
            Action<IClassicDesktopStyleApplicationLifetime>? lifetimeBuilder = null)
        {
            var lifetime = CreateLifetime(args, lifetimeBuilder);
            return builder.SetupWithLifetime(lifetime);
        }

        /// <summary>
        /// Starts the Application with a IClassicDesktopStyleApplicationLifetime, shows main window and runs application main loop.
        /// </summary>
        /// <param name="builder">Application builder.</param>
        /// <param name="args">Startup arguments.</param>
        /// <param name="lifetimeBuilder">Lifetime builder to modify the lifetime before application started.</param>
        /// <returns>Exit code.</returns>
        public static int StartWithClassicDesktopLifetime(
            this AppBuilder builder, string[] args,
            Action<IClassicDesktopStyleApplicationLifetime>? lifetimeBuilder = null)
        {
            var lifetime = CreateLifetime(args, lifetimeBuilder);
            builder.SetupWithLifetime(lifetime);
            return lifetime.Start(args);
        }

        /// <summary>
        /// Starts the Application with a IClassicDesktopStyleApplicationLifetime, shows main window and runs application main loop.
        /// </summary>
        /// <param name="builder">Application builder.</param>
        /// <param name="args">Startup arguments.</param>
        /// <param name="shutdownMode">Lifetime shutdown mode.</param>
        /// <returns>Exit code.</returns>
        public static int StartWithClassicDesktopLifetime(
            this AppBuilder builder, string[] args, ShutdownMode shutdownMode)
        {
            var lifetime = CreateLifetime(args, l => l.ShutdownMode = shutdownMode);
            builder.SetupWithLifetime(lifetime);
            return lifetime.Start(args);
        }
    }
}
