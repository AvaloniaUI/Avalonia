using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ClassicDesktopStyleApplicationLifetime : IClassicDesktopStyleApplicationLifetime, IDisposable
    {
        private int _exitCode;
        private CancellationTokenSource? _cts;
        private bool _isShuttingDown;
        private readonly AvaloniaList<Window> _windows = new();
        private CompositeDisposable? _compositeDisposable; 

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

        internal void SubscribeGlobalEvents()
        {
            if (_compositeDisposable is not null)
            {
                // There could be a case, when lifetime was setup without starting.
                // Until developer started it manually later. To avoid API breaking changes, it will execute Setup method twice.
                return;
            }

            _compositeDisposable = new CompositeDisposable(
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

        internal void SetupCore(string[] args)
        {
            SubscribeGlobalEvents();

            Startup?.Invoke(this, new ControlledApplicationLifetimeStartupEventArgs(args));

            var options = AvaloniaLocator.Current.GetService<ClassicDesktopStyleApplicationLifetimeOptions>();
            
            if(options != null && options.ProcessUrlActivationCommandLine && args.Length > 0)
            {
                if (Application.Current is IApplicationPlatformEvents events)
                {
                    events.RaiseUrlsOpened(args);
                }
            }

            var lifetimeEvents = AvaloniaLocator.Current.GetService<IPlatformLifetimeEventsImpl>(); 

            if (lifetimeEvents != null)
                lifetimeEvents.ShutdownRequested += OnShutdownRequested;
        }

        public int Start(string[] args)
        {
            SetupCore(args);
            
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
            _compositeDisposable?.Dispose();
            _compositeDisposable = null;
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

            try
            {
                // When an OS shutdown request is received, try to close all non-owned windows. Windows can cancel
                // shutdown by setting e.Cancel = true in the Closing event. Owned windows will be shutdown by their
                // owners.
                foreach (var w in Windows.ToArray())
                {
                    if (w.Owner is null)
                    {
                        w.CloseCore(WindowCloseReason.ApplicationShutdown, isProgrammatic);
                    }
                }

                if (!force && Windows.Count > 0)
                {
                    e.Cancel = true;
                    return false;
                }

                var args = new ControlledApplicationLifetimeExitEventArgs(exitCode);
                Exit?.Invoke(this, args);
                _exitCode = args.ApplicationExitCode;                
            }
            finally
            {
                _cts?.Cancel();
                _cts = null;
                _isShuttingDown = false;
                Dispatcher.UIThread.InvokeShutdown();
            }

            return true;
        }
        
        private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e) => DoShutdown(e, false);
    }
    
    public class ClassicDesktopStyleApplicationLifetimeOptions
    {
        public bool ProcessUrlActivationCommandLine { get; set; }
    }
}

namespace Avalonia
{
    /// <summary>
    /// IClassicDesktopStyleApplicationLifetime related AppBuilder extensions.
    /// </summary>
    public static class ClassicDesktopStyleApplicationLifetimeExtensions
    {
        private static ClassicDesktopStyleApplicationLifetime PrepareLifetime(AppBuilder builder, string[] args,
            Action<IClassicDesktopStyleApplicationLifetime>? lifetimeBuilder)
        {
            var lifetime = builder.LifetimeOverride?.Invoke(typeof(ClassicDesktopStyleApplicationLifetime)) as ClassicDesktopStyleApplicationLifetime 
                ?? new ClassicDesktopStyleApplicationLifetime();
            lifetime.SubscribeGlobalEvents();

            lifetime.Args = args;
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
            var lifetime = PrepareLifetime(builder, args, lifetimeBuilder);
            lifetime.SetupCore(args);
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
            var lifetime = PrepareLifetime(builder, args, lifetimeBuilder);
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
            var lifetime = PrepareLifetime(builder, args, l => l.ShutdownMode = shutdownMode);
            builder.SetupWithLifetime(lifetime);
            return lifetime.Start(args);
        }
    }
}
