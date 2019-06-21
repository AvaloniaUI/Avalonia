using System;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Controls.ApplicationLifetimes
{
    public class ClassicDesktopStyleApplicationLifetime : IClassicDesktopStyleApplicationLifetime
    {
        private readonly Application _app;
        private int _exitCode;
        private CancellationTokenSource _cts;
        private bool _isShuttingDown;

        public ClassicDesktopStyleApplicationLifetime(Application app)
        {
            _app = app;
            app.Windows.OnWindowClosed += HandleWindowClosed;
        }
        
        /// <inheritdoc/>
        public event EventHandler<ControlledApplicationLifetimeStartupEventArgs> Startup;
        /// <inheritdoc/>
        public event EventHandler<ControlledApplicationLifetimeExitEventArgs> Exit;

        /// <inheritdoc/>
        public ShutdownMode ShutdownMode { get; set; }
        
        /// <inheritdoc/>
        public Window MainWindow { get; set; }

        private void HandleWindowClosed(Window window)
        {
            if (window == null)
                return;
            
            if (_isShuttingDown)
                return;

            if (ShutdownMode == ShutdownMode.OnLastWindowClose && _app.Windows.Count == 0)
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
                _app.Windows.CloseAll();
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
