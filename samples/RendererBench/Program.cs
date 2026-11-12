using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.Rendering;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using RendererBench.Scenes;
using RendererBench.Timing;

namespace RendererBench;

internal sealed record BenchOptions(
    string Backend,
    string Platform,
    string Scene,
    string Mode,
    double Warmup,
    double Duration,
    double Scale,
    int Width,
    int Height,
    string? Out)
{
    public bool Uncapped => Mode == "uncapped";

    public static BenchOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string>();
        for (var i = 0; i + 1 < args.Length; i += 2)
        {
            if (!args[i].StartsWith("--"))
                throw new ArgumentException($"Unexpected argument '{args[i]}'.");
            values[args[i][2..]] = args[i + 1];
        }

        string Get(string key, string fallback) => values.GetValueOrDefault(key, fallback);
        double Num(string key, double fallback) =>
            values.TryGetValue(key, out var v) ? double.Parse(v, CultureInfo.InvariantCulture) : fallback;

        var options = new BenchOptions(
            Get("backend", "impeller"), Get("platform", OperatingSystem.IsMacOS() ? "macos" : "x11"), Get("scene", "rects"), Get("mode", "vsync"),
            Num("warmup", 2), Num("duration", 5), Num("scale", 1), (int)Num("width", 1280),
            (int)Num("height", 800), values.GetValueOrDefault("out"));

        Check(options.Backend, "backend", "skia", "impeller");
        Check(options.Platform, "platform", "x11", "wayland", "macos");
        Check(options.Mode, "mode", "vsync", "uncapped");
        Check(options.Scene, "scene", Scenes.Scene.All.Keys.ToArray());
        return options;
    }

    private static void Check(string value, string name, params string[] allowed)
    {
        if (!allowed.Contains(value))
            throw new ArgumentException($"--{name} must be one of: {string.Join(", ", allowed)}.");
    }
}

internal static class Program
{
    public static BenchOptions Options { get; private set; } = null!;

    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Contains("--list-scenes"))
        {
            Console.WriteLine(string.Join(Environment.NewLine, Scene.All.Keys));
            return 0;
        }

        try
        {
            Options = BenchOptions.Parse(args);
        }
        catch (Exception e) when (e is ArgumentException or FormatException)
        {
            Console.Error.WriteLine(e.Message);
            return 2;
        }

        if (Options is { Uncapped: true, Platform: "wayland" or "macos" })
        {
            Console.Error.WriteLine(
                $"Uncapped mode is not supported on {Options.Platform}: frames are paced by the compositor.");
            return 3;
        }

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>();

        if (Options.Platform == "macos")
        {
            builder = builder.UseAvaloniaNative().With(new AvaloniaNativePlatformOptions
            {
                RenderingMode = new[] { AvaloniaNativeRenderingMode.Metal }
            });
        }
        else
        {
            builder = builder.UseX11().With(new X11PlatformOptions
            {
                // Both backends render through OpenGL ES, which Impeller needs.
                RenderingMode = new[] { X11RenderingMode.Egl },
                GlProfiles = new List<GlVersion>
                {
                    new(GlProfileType.OpenGLES, 3, 2),
                    new(GlProfileType.OpenGLES, 3, 0),
                    new(GlProfileType.OpenGLES, 2, 0),
                }
            });
            if (Options.Platform == "wayland")
                builder = builder.UseWaylandWithFallback();
        }

        builder = Options.Backend == "skia" ? builder.UseSkia() : builder.UseImpeller();

        var initRendering = builder.RenderingSubsystemInitializer!;
        builder.UseRenderingSubsystem(() =>
        {
            initRendering();
            TimingRenderInterface.Install(Options.Uncapped);
        }, builder.RenderingSubsystemName ?? Options.Backend);

        return builder
            .UseHarfBuzz()
            .WithInterFont()
            .LogToTrace();
    }
}

internal sealed class App : Application
{
    private Scene _scene = null!;
    private Window _window = null!;
    private SleepLoopRenderTimer? _uncappedTimer;
    private long _frame;
    private long _startFrame;
    private long _sceneFrames;
    private int _advancePending;
    private int _gen0, _gen1, _gen2;
    private long _allocated;
    private TimeSpan _gcPause;
    private (int Gen0, int Gen1, int Gen2, long Allocated) _gcDelta;

    public override void Initialize() => Styles.Add(new FluentTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        var options = Program.Options;
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            throw new InvalidOperationException("RendererBench needs the classic desktop lifetime.");

        if (options.Uncapped)
            _uncappedTimer = FindRenderTimer();

        _scene = Scene.All[options.Scene](options.Scale);
        _window = new Window
        {
            Title = $"RendererBench {options.Backend} {options.Scene}",
            Width = options.Width,
            Height = options.Height,
            CanResize = false,
            Background = Brushes.Black,
            Content = _scene.Build(),
        };
        _window.Opened += (_, _) => Start(desktop);
        desktop.MainWindow = _window;

        base.OnFrameworkInitializationCompleted();
    }

    // The sleep loop timer runs at the monitor refresh rate; raise it far above any refresh rate.
    private static SleepLoopRenderTimer? FindRenderTimer()
    {
        var loop = AvaloniaLocator.Current.GetService<IRenderLoop>();
        var timer = loop?.GetType().GetProperty("Timer", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(loop) as SleepLoopRenderTimer;
        if (timer is null)
            Console.Error.WriteLine("Warning: render timer not found; uncapped mode is limited to vsync pacing.");
        return timer;
    }

    private void Start(IClassicDesktopStyleApplicationLifetime desktop)
    {
        // Animation frames are not paced by presents everywhere, so the
        // scene advances once per rendered frame instead. It advances when the frame starts, not when it is
        // presented, so the UI thread prepares the next frame while the render thread draws this one.
        FrameRecorder.Instance.FrameStarted += () =>
        {
            if (Interlocked.Exchange(ref _advancePending, 1) == 0)
                Dispatcher.UIThread.Post(Advance);
        };
        Advance();

        var options = Program.Options;
        DispatcherTimer.RunOnce(() =>
        {
            _gen0 = GC.CollectionCount(0);
            _gen1 = GC.CollectionCount(1);
            _gen2 = GC.CollectionCount(2);
            _allocated = GC.GetTotalAllocatedBytes();
            _gcPause = GC.GetTotalPauseDuration();
            _startFrame = _frame;
            FrameRecorder.Instance.Start();

            DispatcherTimer.RunOnce(() =>
            {
                FrameRecorder.Instance.Stop();
                _sceneFrames = _frame - _startFrame;
                _gcDelta = (GC.CollectionCount(0) - _gen0, GC.CollectionCount(1) - _gen1,
                    GC.CollectionCount(2) - _gen2, GC.GetTotalAllocatedBytes() - _allocated);
                _gcPause = GC.GetTotalPauseDuration() - _gcPause;

                // Let outstanding GPU timer queries resolve before the samples are read.
                DispatcherTimer.RunOnce(() =>
                {
                    Finish();
                    desktop.Shutdown();
                }, TimeSpan.FromMilliseconds(300));
            }, TimeSpan.FromSeconds(options.Duration));
        }, TimeSpan.FromSeconds(options.Warmup));
    }

    private void Advance()
    {
        Volatile.Write(ref _advancePending, 0);
        if (_uncappedTimer is { DesiredFps: < 1000 })
            _uncappedTimer.DesiredFps = 1000;

        _scene.Advance(++_frame);
    }

    private void Finish()
    {
        var options = Program.Options;
        var (frames, seconds, memory) = FrameRecorder.Instance.Snapshot();
        var scaling = _window.RenderScaling;
        var result = new RunResult
        {
            Backend = options.Backend,
            Platform = options.Platform,
            Scene = options.Scene,
            Mode = options.Mode,
            Scale = options.Scale,
            PixelWidth = (int)(_window.ClientSize.Width * scaling),
            PixelHeight = (int)(_window.ClientSize.Height * scaling),
            RenderScaling = scaling,
            RefreshHz = OperatingSystem.IsMacOS() ? ReadMacRefreshHz(_window) : null,
            GlVendor = TimingRenderInterface.GlVendor,
            GlRenderer = TimingRenderInterface.GlRenderer,
            GlVersion = TimingRenderInterface.GlVersion,
            GpuTimingMethod = TimingRenderInterface.GpuTimingMethod,
            WarmupSeconds = options.Warmup,
            MeasuredSeconds = seconds,
            StartupMs = FrameRecorder.Instance.StartupMs ?? double.NaN,
            PeakRssMb = MemoryProbe.PeakRssMb(),
            ManagedHeapMb = GC.GetTotalMemory(false) / (1024.0 * 1024.0),
            AllocatedBytesPerFrame = frames.Count == 0 ? 0 : (double)_gcDelta.Allocated / frames.Count,
            RenderThreadAllocatedBytesPerFrame =
                frames.Count == 0 ? 0 : frames.Average(f => (double)f.RenderAllocatedBytes),
            GcPauseMs = _gcPause.TotalMilliseconds,
            FootprintStartMb = memory.FootprintStartMb,
            FootprintEndMb = memory.FootprintEndMb,
            FootprintPeakMb = memory.FootprintPeakMb,
            GpuMemoryStartMb = memory.GpuMemoryStartMb,
            GpuMemoryEndMb = memory.GpuMemoryEndMb,
            GpuMemoryPeakMb = memory.GpuMemoryPeakMb,
            SceneFrames = _sceneFrames,
            Gen0Collections = _gcDelta.Gen0,
            Gen1Collections = _gcDelta.Gen1,
            Gen2Collections = _gcDelta.Gen2,
            Frames = frames,
        };

        var json = JsonSerializer.Serialize(result, RunResultJsonContext.Default.RunResult);
        if (options.Out is { } path)
            File.WriteAllText(path, json);
        else
            Console.WriteLine(json);

        Console.Error.WriteLine(
            $"{options.Backend}/{options.Scene}/{options.Mode}: {frames.Count} frames in {seconds:F2}s");
    }

    // CVDisplayLink ticks at the screen's maximum rate, also on variable refresh displays.
    private static double? ReadMacRefreshHz(Window window)
    {
        if (window.TryGetPlatformHandle() is not { HandleDescriptor: "NSWindow", Handle: var nsWindow })
            return null;
        var screen = objc_msgSend(nsWindow, sel_registerName("screen"));
        if (screen == IntPtr.Zero)
            return null;
        var fps = objc_msgSend(screen, sel_registerName("maximumFramesPerSecond"));
        return fps > 0 ? fps : null;
    }

    [DllImport("/usr/lib/libobjc.dylib")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.dylib")]
    private static extern IntPtr sel_registerName(string name);
}
