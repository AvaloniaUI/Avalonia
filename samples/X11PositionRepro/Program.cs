using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Themes.Simple;
using Avalonia.Threading;

namespace X11PositionRepro;

// Repro: on X11 under a reparenting window manager (e.g. openbox), a window positioned before
// it is mapped can report Window.Position as (0, 0) for good, while the X server has it elsewhere.
//
// Opens a series of undecorated child windows with WindowStartupLocation.CenterOwner. Once each
// one has settled, compares Window.Position with the window's real position from
// XTranslateCoordinates. The windows have no frame extents, so the two should be equal.
// The exit code is the number of mismatches.
//
//   xvfb-run -s "-screen 0 1920x1080x24" sh -c 'openbox & sleep 1; dotnet X11PositionRepro.dll'
public static class Program
{
    private const int Iterations = 30;

    public static int Main(string[] args) => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .StartWithClassicDesktopLifetime(args);

    private class App : Application
    {
        public override void Initialize() => Styles.Add(new SimpleTheme());

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var main = new Window { Title = "Main", Width = 1000, Height = 700, Position = new PixelPoint(200, 150) };
                main.Opened += async (_, _) => desktop.Shutdown(await RunAsync(main));
                desktop.MainWindow = main;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }

    private static async Task<int> RunAsync(Window owner)
    {
        await Task.Delay(500);

        var mismatches = 0;

        for (var i = 1; i <= Iterations; i++)
        {
            var window = new Window
            {
                Title = $"Child {i}",
                Width = 600,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                WindowDecorations = WindowDecorations.None,
                Background = Brushes.SteelBlue,
            };

            var opened = new TaskCompletionSource();
            window.Opened += (_, _) => opened.SetResult();
            window.Show(owner);
            await opened.Task;

            // Give the window manager time to place the window and send any ConfigureNotify.
            await Task.Delay(300);

            var reported = window.Position;
            var actual = X11.GetRootPosition(window.TryGetPlatformHandle()!.Handle);
            var ok = reported == actual;

            if (!ok)
                mismatches++;

            Console.WriteLine($"{i,2}: Position={reported} actual={actual}{(ok ? "" : "  MISMATCH")}");
            window.Close();
            await Task.Delay(100);
        }

        Console.WriteLine($"{mismatches} of {Iterations} windows reported the wrong Position.");
        return mismatches;
    }

    private static class X11
    {
        private static readonly IntPtr s_display = XOpenDisplay(IntPtr.Zero);

        public static PixelPoint GetRootPosition(IntPtr window)
        {
            XTranslateCoordinates(s_display, window, XDefaultRootWindow(s_display), 0, 0, out var x, out var y, out _);
            return new PixelPoint(x, y);
        }

        [DllImport("libX11.so.6")]
        private static extern IntPtr XOpenDisplay(IntPtr name);

        [DllImport("libX11.so.6")]
        private static extern IntPtr XDefaultRootWindow(IntPtr display);

        [DllImport("libX11.so.6")]
        private static extern int XTranslateCoordinates(IntPtr display, IntPtr src, IntPtr dest, int srcX, int srcY,
            out int destX, out int destY, out IntPtr child);
    }
}
