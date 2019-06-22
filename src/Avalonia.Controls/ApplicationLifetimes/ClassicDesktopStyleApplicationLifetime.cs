using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;

namespace Avalonia.Controls.ApplicationLifetimes
{
    public class ClassicDesktopStyleApplicationLifetime : IClassicDesktopStyleApplicationLifetime, IDisposable
    {
        private readonly Application _app;
        private int _exitCode;
        private CancellationTokenSource _cts;
        private bool _isShuttingDown;
        private HashSet<Window> _windows = new HashSet<Window>();

        private static ClassicDesktopStyleApplicationLifetime _activeLifetime;
        static ClassicDesktopStyleApplicationLifetime()
        {
            Window.WindowOpenedEvent.AddClassHandler(typeof(Window), OnWindowOpened);
            Window.WindowClosedEvent.AddClassHandler(typeof(Window), WindowClosedEvent);
        }

        private static void WindowClosedEvent(object sender, RoutedEventArgs e)
        {
            _activeLifetime?._windows.Remove((Window)sender);
            _activeLifetime?.HandleWindowClosed((Window)sender);
        }

        private static void OnWindowOpened(object sender, RoutedEventArgs e)
        {
            _activeLifetime?._windows.Add((Window)sender);
        }

        public ClassicDesktopStyleApplicationLifetime(Application app)
        {
            if (_activeLifetime != null)
                throw new InvalidOperationException(
                    "Can not have multiple active ClassicDesktopStyleApplicationLifetime instances and the previously created one was not disposed");
            _app = app;
            _activeLifetime = this;
        }
        
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
            _app.Run(_cts.Token);
            Environment.ExitCode = _exitCode;
            return _exitCode;
        }

        public void Dispose()
        {
            if (_activeLifetime == this)
                _activeLifetime = null;
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
            var lifetime = new ClassicDesktopStyleApplicationLifetime(builder.Instance) {ShutdownMode = shutdownMode};
            builder.Instance.ApplicationLifetime = lifetime;
            builder.SetupWithoutStarting();
            return lifetime.Start(args);
        }
    }
}
