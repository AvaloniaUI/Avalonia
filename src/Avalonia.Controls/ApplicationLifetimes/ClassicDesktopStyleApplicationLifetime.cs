using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Avalonia.Controls.ApplicationLifetimes
{
    public class ClassicDesktopStyleApplicationLifetime : IClassicDesktopStyleApplicationLifetime
    {
        private int _exitCode;
        private CancellationTokenSource _cts;
        private bool _isShuttingDown;
        private readonly HashSet<Window> _windows = new HashSet<Window>();

        /// <inheritdoc/>
        public event EventHandler<ControlledApplicationLifetimeStartupEventArgs> Startup;
        /// <inheritdoc/>
        public event EventHandler<ControlledApplicationLifetimeExitEventArgs> Exit;

        /// <inheritdoc/>
        public ShutdownMode ShutdownMode { get; set; }

        /// <inheritdoc/>
        public Window MainWindow { get; set; }

        public IReadOnlyList<Window> Windows => _windows.ToList();

        private void HandleWindowClosed(Window window)
        {
            if (window == null)
                return;

            if (_isShuttingDown)
                return;

            if (ShutdownMode == ShutdownMode.OnLastWindowClose && _windows.Count == 0)
                Shutdown();
            else if (ShutdownMode == ShutdownMode.OnMainWindowClose && window == MainWindow)
                Shutdown();
        }

        public void Shutdown(int exitCode = 0)
        {
            if (_isShuttingDown)
                throw new InvalidOperationException("Application is already shutting down.");

            _exitCode = exitCode;
            _isShuttingDown = true;

            try
            {
                foreach (var w in Windows)
                    w.Close();
                var e = new ControlledApplicationLifetimeExitEventArgs(exitCode);
                Exit?.Invoke(this, e);
                _exitCode = e.ApplicationExitCode;
            }
            finally
            {
                _cts?.Cancel();
                _cts = null;
                _isShuttingDown = false;
            }
        }

        public int Start(string[] args)
        {
            Startup?.Invoke(this, new ControlledApplicationLifetimeStartupEventArgs(args));

            _cts = new CancellationTokenSource();

            MainWindow?.Show();

            Dispatcher.UIThread.MainLoop(_cts.Token);

            Environment.ExitCode = _exitCode;

            return _exitCode;
        }

        private void WindowClosedEvent(object sender, RoutedEventArgs e)
        {
            _windows.Remove((Window)sender);

            HandleWindowClosed((Window)sender);
        }

        private void OnWindowOpened(object sender, RoutedEventArgs e)
        {
            _windows.Add((Window)sender);
        }

        public void OnFrameworkInitializationCompleted()
        {
            Window.WindowOpenedEvent.AddClassHandler(typeof(Window), OnWindowOpened);

            Window.WindowClosedEvent.AddClassHandler(typeof(Window), WindowClosedEvent);
        }
    }
}

namespace Avalonia
{
    public static class ClassicDesktopStyleApplicationLifetimeExtensions
    {
        public static int StartWithClassicDesktopLifetime<T>(
            this T builder, string[] args, ShutdownMode shutdownMode = ShutdownMode.OnLastWindowClose)
            where T : AppBuilderBase<T>, new()
        {
            var lifetime = new ClassicDesktopStyleApplicationLifetime() { ShutdownMode = shutdownMode };
            builder.SetupWithLifetime(lifetime);
            return lifetime.Start(args);
        }
    }
}
