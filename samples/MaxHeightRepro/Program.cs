using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Themes.Simple;
using Avalonia.Threading;

namespace MaxHeightRepro;

public class App : Application
{
    public override void Initialize()
    {
        Styles.Add(new SimpleTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mode = Environment.GetEnvironmentVariable("MODE") ?? "base";
            var owner = new Window
            {
                Title = "Owner",
                Width = 1600,
                Height = 1000,
                Content = new TextBlock { Text = "Owner", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center }
            };
            Program.Log("owner", owner);
            desktop.MainWindow = owner;

            var useMaxHeight = Environment.GetEnvironmentVariable("NO_MAXHEIGHT") != "1";
            var dlg = new Window
            {
                Title = "Dialog",
                Width = 350,
                Height = 140,
                MinWidth = 300,
                MinHeight = 140,
                SizeToContent = SizeToContent.Manual,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ShowInTaskbar = false,
                Content = new TextBlock { Text = "Dialog" }
            };
            if (useMaxHeight)
                dlg.MaxHeight = 140;
            Program.Log("dialog", dlg);

            async Task ShowAndReport(Action show)
            {
                Console.WriteLine($"[dialog] showing, MaxHeight={dlg.MaxHeight} mode={mode}");
                show();
                await Task.Delay(2500);
                Console.WriteLine($"[dialog] FINAL Width={dlg.Width} Height={dlg.Height} ClientSize={dlg.ClientSize} Position={dlg.Position} FrameSize={dlg.FrameSize}");
                Console.Out.Flush();
                await Task.Delay(300);
                if (Environment.GetEnvironmentVariable("KEEP") != "1")
                    desktop.Shutdown();
            }

            if (mode == "activated")
            {
                // Emulates: host code moves the (not yet shown) window, reads the position back
                // (a round trip that pulls the ConfigureNotify into Xlib's queue) and shows the window
                // from a Normal-priority dispatcher job, all from inside an X11 event callback.
                var done = false;
                owner.Activated += (_, _) =>
                {
                    if (done) return;
                    done = true;
                    Console.WriteLine("[owner] Activated -> moving dialog to 0,0 and posting ShowDialog");
                    dlg.Position = new PixelPoint(0, 0);
                    var p = dlg.Position;
                    Dispatcher.UIThread.Post(() => _ = ShowAndReport(() => _ = dlg.ShowDialog(owner)), DispatcherPriority.Normal);
                };
            }
            else
            {
                owner.Opened += async (_, _) =>
                {
                    await Task.Delay(500);
                    if (mode == "pos0")
                        dlg.Position = new PixelPoint(0, 0);
                    await ShowAndReport(() => _ = dlg.ShowDialog(owner));
                };
            }
        }
        base.OnFrameworkInitializationCompleted();
    }
}

public static class Program
{
    public static void Log(string name, Window w)
    {
        w.PropertyChanged += (_, e) =>
        {
            if (e.Property == Window.WidthProperty || e.Property == Window.HeightProperty
                || e.Property == TopLevel.ClientSizeProperty)
                Console.WriteLine($"[{name}] {e.Property.Name}: {e.OldValue} -> {e.NewValue}");
        };
        w.PositionChanged += (_, e) => Console.WriteLine($"[{name}] Position -> {e.Point}");
        w.Resized += (_, e) => Console.WriteLine($"[{name}] Resized {e.ClientSize} reason={e.Reason}");
    }

    [STAThread]
    public static void Main(string[] args) => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToTrace()
        .StartWithClassicDesktopLifetime(args);
}
