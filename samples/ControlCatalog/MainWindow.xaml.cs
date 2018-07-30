using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ReactiveUI;
using System;
using System.Reactive.Linq;
using System.Threading;

namespace ControlCatalog
{
    public class MainWindow : Window
    {
		private Thread _initialThread;

        public MainWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();
            Renderer.DrawFps = true;
            Renderer.DrawDirtyRects = Renderer.DrawFps = true;

			_initialThread = Thread.CurrentThread;
        }

        private void InitializeComponent()
        {
            // TODO: iOS does not support dynamically loading assemblies
            // so we must refer to this resource DLL statically. For
            // now I am doing that here. But we need a better solution!!
            var theme = new Avalonia.Themes.Default.DefaultTheme();
            theme.TryGetResource("Button", out _);
            AvaloniaXamlLoader.Load(this);

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1),
            };

            Observable.FromEventPattern(timer, nameof(timer.Tick)).Throttle(TimeSpan.FromMilliseconds(500)).ObserveOn(RxApp.MainThreadScheduler).Subscribe(_ =>
            {
                if (Thread.CurrentThread != _initialThread)
                {
                    throw new SystemException();
                }
            });

            timer.Start();

        }
    }
}
