using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using RendererBench;

namespace RendererBench.Report;

internal sealed record Options(
    string Platform,
    string[] Backends,
    string[] Scenes,
    string[] Modes,
    int Reps,
    double Warmup,
    double Duration,
    double Scale,
    string OutDir,
    bool Build,
    string Configuration);

/// <summary>Metrics of one run, or the median of several.</summary>
internal sealed record Metrics(
    double CpuP50, double CpuP95, double CpuP99, double DrawP50, double PresentP50,
    double? GpuP50, double? GpuP95, double Fps, double MissedPct, double StartupMs, double PeakRssMb,
    double AllocKbPerFrame, int Frames, double RenderAllocKbPerFrame, double GcPauseMsPerSecond,
    double Gen0, double Gen1, double Gen2, double FootprintMb, double FootprintGrowthMb, double FootprintPeakMb,
    double? GpuMemoryPeakMb, double? GpuMemoryGrowthMb, double ManagedHeapMb);

internal static class Program
{
    private static readonly string[] s_allScenes =
        { "rects", "paths", "text-static", "text-dynamic", "gradients", "images", "layers", "effects" };

    public static int Main(string[] args)
    {
        Options options;
        try
        {
            options = ParseOptions(args);
        }
        catch (Exception e) when (e is ArgumentException or FormatException)
        {
            Console.Error.WriteLine(e.Message);
            Console.Error.WriteLine(
                "Usage: RendererBench.Report [--platform x11|wayland|macos] [--backends skia,impeller] " +
                "[--scenes all|a,b] [--modes vsync,uncapped] [--reps 3] [--warmup 2] [--duration 5] " +
                "[--scale 1] [--out dir] [--configuration Release] [--no-build]");
            return 2;
        }

        var root = FindRepoRoot();
        var benchProject = Path.Combine(root, "samples", "RendererBench");
        if (options.Build && Run("dotnet", new[] { "build", benchProject, "-c", options.Configuration },
                root, TimeSpan.FromMinutes(20)).ExitCode != 0)
        {
            Console.Error.WriteLine("Building RendererBench failed.");
            return 1;
        }

        var dll = Directory.GetFiles(Path.Combine(benchProject, "bin", options.Configuration), "RendererBench.dll",
                SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
        if (dll is null)
        {
            Console.Error.WriteLine("RendererBench.dll not found; build it or drop --no-build.");
            return 1;
        }

        var runsDir = Path.Combine(options.OutDir, "runs");
        Directory.CreateDirectory(runsDir);

        var results = new List<RunResult>();
        var failures = new List<string>();
        var total = options.Reps * options.Scenes.Length * options.Modes.Length * options.Backends.Length;
        var index = 0;
        for (var rep = 1; rep <= options.Reps; rep++)
        {
            // Alternate which backend goes first so thermal drift does not favour one of them.
            var backends = rep % 2 == 1 ? options.Backends : options.Backends.Reverse().ToArray();
            foreach (var scene in options.Scenes)
            foreach (var mode in options.Modes)
            foreach (var backend in backends)
            {
                index++;
                var label = $"{scene}/{mode}/{backend} rep {rep}";
                Console.WriteLine($"[{index}/{total}] {label}");
                var outFile = Path.Combine(runsDir, $"{scene}-{mode}-{backend}-r{rep}.json");
                var run = Run("dotnet", new[]
                {
                    dll, "--backend", backend, "--platform", options.Platform, "--scene", scene, "--mode", mode,
                    "--warmup", Str(options.Warmup), "--duration", Str(options.Duration),
                    "--scale", Str(options.Scale), "--out", outFile,
                }, root, TimeSpan.FromSeconds(options.Warmup + options.Duration + 60));

                RunResult? result = null;
                if (run.ExitCode == 0 && File.Exists(outFile))
                    result = JsonSerializer.Deserialize(File.ReadAllText(outFile),
                        RunResultJsonContext.Default.RunResult);

                if (result is null || result.Frames.Count == 0)
                {
                    var why = run.TimedOut ? "timed out" : $"exit code {run.ExitCode}";
                    failures.Add($"{label}: {why}. {LastLines(run.Stderr, 3)}");
                    Console.WriteLine($"  failed: {why}");
                    continue;
                }

                results.Add(result);
            }
        }

        File.WriteAllText(Path.Combine(options.OutDir, "results.json"),
            JsonSerializer.Serialize(results, RunResultJsonContext.Default.ListRunResult));

        var report = BuildReport(options, root, results, failures);
        var reportPath = Path.Combine(options.OutDir, "report.md");
        File.WriteAllText(reportPath, report);
        Console.WriteLine($"Report: {reportPath}");
        return failures.Count == 0 ? 0 : 1;
    }

    private static Options ParseOptions(string[] args)
    {
        var values = new Dictionary<string, string>();
        var build = true;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--no-build")
            {
                build = false;
                continue;
            }

            if (!args[i].StartsWith("--") || i + 1 >= args.Length)
                throw new ArgumentException($"Unexpected argument '{args[i]}'.");
            values[args[i][2..]] = args[++i];
        }

        string[] List(string key, string fallback) =>
            values.GetValueOrDefault(key, fallback).Split(',', StringSplitOptions.RemoveEmptyEntries);
        double Num(string key, double fallback) =>
            values.TryGetValue(key, out var v) ? double.Parse(v, CultureInfo.InvariantCulture) : fallback;

        var platform = values.GetValueOrDefault("platform",
            OperatingSystem.IsMacOS() ? "macos"
            : Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") is { Length: > 0 } ? "wayland" : "x11");
        if (platform is not ("x11" or "wayland" or "macos"))
            throw new ArgumentException("--platform must be x11, wayland or macos.");

        // Some platforms pace frames from the compositor, so only vsync runs there.
        var paced = platform is "wayland" or "macos";
        var modes = List("modes", paced ? "vsync" : "vsync,uncapped");
        if (paced && modes.Contains("uncapped"))
            throw new ArgumentException($"Uncapped mode is not supported on {platform}.");
        if (modes.Any(m => m is not ("vsync" or "uncapped")))
            throw new ArgumentException("--modes takes vsync and/or uncapped.");

        var scenes = List("scenes", "all");
        if (scenes is ["all"])
            scenes = s_allScenes;
        if (scenes.Except(s_allScenes).FirstOrDefault() is { } unknown)
            throw new ArgumentException($"Unknown scene '{unknown}'. Scenes: {string.Join(", ", s_allScenes)}.");

        var backends = List("backends", "skia,impeller");
        if (backends.Any(b => b is not ("skia" or "impeller")))
            throw new ArgumentException("--backends takes skia and/or impeller.");

        var outDir = values.GetValueOrDefault("out") ?? Path.Combine(FindRepoRoot(), "artifacts", "renderer-bench",
            DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));

        return new Options(platform, backends, scenes, modes, (int)Num("reps", 3), Num("warmup", 2),
            Num("duration", 5), Num("scale", 1), Path.GetFullPath(outDir), build,
            values.GetValueOrDefault("configuration", "Release"));
    }

    private static Metrics Measure(RunResult r)
    {
        var cpu = r.Frames.Select(f => f.DrawMs + f.PresentMs).ToArray();
        var gpu = r.Frames.Where(f => f.GpuMs.HasValue).Select(f => f.GpuMs!.Value).ToArray();
        return new Metrics(
            Percentile(cpu, 50), Percentile(cpu, 95), Percentile(cpu, 99),
            Percentile(r.Frames.Select(f => f.DrawMs).ToArray(), 50),
            Percentile(r.Frames.Select(f => f.PresentMs).ToArray(), 50),
            gpu.Length > 0 ? Percentile(gpu, 50) : null, gpu.Length > 0 ? Percentile(gpu, 95) : null,
            r.Frames.Count / r.MeasuredSeconds, MissedPercent(r), r.StartupMs, r.PeakRssMb,
            r.AllocatedBytesPerFrame / 1024, r.Frames.Count, r.RenderThreadAllocatedBytesPerFrame / 1024,
            r.GcPauseMs / r.MeasuredSeconds, r.Gen0Collections, r.Gen1Collections, r.Gen2Collections,
            r.FootprintEndMb, r.FootprintEndMb - r.FootprintStartMb, r.FootprintPeakMb, r.GpuMemoryPeakMb,
            r.GpuMemoryEndMb - r.GpuMemoryStartMb, r.ManagedHeapMb);
    }

    /// <summary>
    /// Share of vsync intervals with no new frame. The refresh period comes from the display when the platform
    /// reports it. Otherwise it is taken as the 10th percentile of the frame intervals, which stays at the real
    /// period only when at least some frames make every vsync.
    /// </summary>
    private static double MissedPercent(RunResult r)
    {
        if (r.Frames.Count < 3)
            return double.NaN;
        var intervals = r.Frames.Zip(r.Frames.Skip(1), (a, b) => b.StartMs - a.StartMs).ToArray();
        var period = r.RefreshHz is > 0 ? 1000 / r.RefreshHz.Value : Percentile(intervals, 10);
        if (period <= 0)
            return double.NaN;
        var missed = intervals.Sum(dt => Math.Max(0, Math.Round(dt / period) - 1));
        return 100 * missed / (intervals.Length + missed);
    }

    private static Metrics Median(IReadOnlyList<Metrics> reps)
    {
        double M(Func<Metrics, double> f) => Percentile(reps.Select(f).ToArray(), 50);
        double? N(Func<Metrics, double?> f)
        {
            var values = reps.Where(r => f(r).HasValue).Select(r => f(r)!.Value).ToArray();
            return values.Length > 0 ? Percentile(values, 50) : null;
        }

        return new Metrics(M(r => r.CpuP50), M(r => r.CpuP95), M(r => r.CpuP99), M(r => r.DrawP50),
            M(r => r.PresentP50), N(r => r.GpuP50), N(r => r.GpuP95), M(r => r.Fps), M(r => r.MissedPct),
            M(r => r.StartupMs), M(r => r.PeakRssMb), M(r => r.AllocKbPerFrame), reps.Sum(r => r.Frames),
            M(r => r.RenderAllocKbPerFrame), M(r => r.GcPauseMsPerSecond), M(r => r.Gen0), M(r => r.Gen1),
            M(r => r.Gen2), M(r => r.FootprintMb), M(r => r.FootprintGrowthMb), M(r => r.FootprintPeakMb),
            N(r => r.GpuMemoryPeakMb), N(r => r.GpuMemoryGrowthMb), M(r => r.ManagedHeapMb));
    }

    private static string BuildReport(Options options, string root, List<RunResult> results, List<string> failures)
    {
        var metrics = results
            .GroupBy(r => (r.Scene, r.Mode, r.Backend))
            .ToDictionary(g => g.Key, g => Median(g.Select(Measure).ToList()));
        Metrics? Get(string scene, string mode, string backend) =>
            metrics.GetValueOrDefault((scene, mode, backend));

        var skia = options.Backends.Contains("skia") ? "skia" : options.Backends[0];
        var impeller = options.Backends.Contains("impeller") ? "impeller" : options.Backends[^1];
        var sb = new StringBuilder();
        sb.AppendLine("# Skia vs Impeller renderer benchmark");
        sb.AppendLine();

        var first = results.FirstOrDefault();
        sb.AppendLine("| | |");
        sb.AppendLine("|---|---|");
        sb.AppendLine($"| Date | {DateTime.Now:yyyy-MM-dd HH:mm} |");
        sb.AppendLine($"| Commit | `{Git(root, "rev-parse --short HEAD")}`{(Git(root, "status --porcelain") is { Length: > 0 } ? " (dirty)" : "")} on `{Git(root, "rev-parse --abbrev-ref HEAD")}` |");
        sb.AppendLine($"| CPU | {CpuModel()} ({Environment.ProcessorCount} threads) |");
        sb.AppendLine($"| OS | {OsName()} |");
        sb.AppendLine($"| .NET | {Environment.Version} |");
        sb.AppendLine($"| Platform | {options.Platform} |");
        foreach (var backend in options.Backends)
        {
            var r = results.FirstOrDefault(x => x.Backend == backend);
            if (r is not null)
                sb.AppendLine($"| {(r.GlVersion == "Metal" ? "GPU" : "GL")} ({backend}) | {r.GlRenderer} · {r.GlVersion} · GPU timing: {r.GpuTimingMethod} |");
        }

        if (first is not null)
            sb.AppendLine($"| Surface | {first.PixelWidth}×{first.PixelHeight} px, scaling {Str(first.RenderScaling)}" +
                          (first.RefreshHz is { } hz ? $", display {Str(hz)} Hz" : "") + " |");
        sb.AppendLine($"| Protocol | {options.Reps} reps, {Str(options.Warmup)} s warmup, {Str(options.Duration)} s measured, scene scale {Str(options.Scale)}, fresh process per run, median of reps |");
        sb.AppendLine();

        if (results.Any(r => r.GlRenderer?.Contains("llvmpipe", StringComparison.OrdinalIgnoreCase) == true
                             || r.GlRenderer?.Contains("softpipe", StringComparison.OrdinalIgnoreCase) == true))
        {
            sb.AppendLine("> **Warning:** software rasterizer (llvmpipe/softpipe). These numbers do not reflect GPU performance.");
            sb.AppendLine();
        }

        AppendSummary(sb, options, Get, skia, impeller);

        foreach (var mode in options.Modes)
        {
            sb.AppendLine($"## Frame times, {mode}");
            sb.AppendLine();
            sb.AppendLine("CPU = draw + present on the render thread. Times in ms. Ratio = Impeller / Skia; below 1 means Impeller is faster.");
            if (mode == "vsync" && options.Platform == "x11")
                sb.AppendLine("Present includes the wait for vsync, so compare Draw and GPU here; uncapped CPU times show the real cost.");
            else if (mode == "vsync" && options.Platform == "macos")
                sb.AppendLine("Both backends render through Metal. GPU = time the frame's render passes run, from timestamps sampled at the start and end of each pass. The wait for the next drawable is left out of Draw and CPU.");
            else if (mode == "vsync")
                sb.AppendLine("Wayland paces frames with frame callbacks and the swap does not block, so CPU times are the real cost.");
            sb.AppendLine();
            sb.AppendLine("| Scene | CPU p50 Skia | CPU p50 Impeller | Ratio | CPU p95 Skia | CPU p95 Impeller | Draw p50 S / I | Present p50 S / I | GPU p50 Skia | GPU p50 Impeller | Ratio |");
            sb.AppendLine("|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|");
            foreach (var scene in options.Scenes)
            {
                var s = Get(scene, mode, skia);
                var i = Get(scene, mode, impeller);
                sb.AppendLine($"| {scene} | {F(s?.CpuP50)} | {F(i?.CpuP50)} | {Ratio(i?.CpuP50, s?.CpuP50)} | " +
                              $"{F(s?.CpuP95)} | {F(i?.CpuP95)} | {F(s?.DrawP50)} / {F(i?.DrawP50)} | " +
                              $"{F(s?.PresentP50)} / {F(i?.PresentP50)} | {F(s?.GpuP50)} | {F(i?.GpuP50)} | " +
                              $"{Ratio(i?.GpuP50, s?.GpuP50)} |");
            }

            sb.AppendLine();
            if (mode == "uncapped")
            {
                sb.AppendLine("### Throughput, uncapped");
                sb.AppendLine();
                sb.AppendLine("| Scene | FPS Skia | FPS Impeller | Impeller / Skia |");
                sb.AppendLine("|---|--:|--:|--:|");
                foreach (var scene in options.Scenes)
                {
                    var s = Get(scene, mode, skia);
                    var i = Get(scene, mode, impeller);
                    sb.AppendLine($"| {scene} | {F(s?.Fps, 0)} | {F(i?.Fps, 0)} | {Ratio(i?.Fps, s?.Fps)} |");
                }
            }
            else
            {
                sb.AppendLine("### Frame pacing, vsync");
                sb.AppendLine();
                sb.AppendLine("Missed = vsync intervals without a new frame, as a share of all intervals" +
                              (first?.RefreshHz is { } hz ? $" at the display's {Str(hz)} Hz." : "."));
                sb.AppendLine();
                sb.AppendLine("| Scene | FPS Skia | FPS Impeller | Missed Skia | Missed Impeller |");
                sb.AppendLine("|---|--:|--:|--:|--:|");
                foreach (var scene in options.Scenes)
                {
                    var s = Get(scene, mode, skia);
                    var i = Get(scene, mode, impeller);
                    sb.AppendLine($"| {scene} | {F(s?.Fps, 0)} | {F(i?.Fps, 0)} | {F(s?.MissedPct, 1)}% | {F(i?.MissedPct, 1)}% |");
                }
            }

            sb.AppendLine();
        }

        Metrics? Across(string scene, string backend)
        {
            var all = options.Modes.Select(m => Get(scene, m, backend)).OfType<Metrics>().ToList();
            return all.Count > 0 ? Median(all) : null;
        }

        sb.AppendLine("## Memory");
        sb.AppendLine();
        sb.AppendLine("Medians over all runs of the scene. S = Skia, I = Impeller.");
        sb.AppendLine();
        sb.AppendLine("### Managed allocations and GC");
        sb.AppendLine();
        sb.AppendLine("Alloc = managed bytes allocated per frame by all threads; the render thread's share is also shown. " +
                      "GCs are counts over the measured window; pause = time the process was stopped by GC, per second.");
        sb.AppendLine();
        sb.AppendLine("| Scene | Alloc KB/frame S / I | Render thread KB/frame S / I | GCs gen0/1/2 S | GCs gen0/1/2 I | GC pause ms/s S / I |");
        sb.AppendLine("|---|--:|--:|--:|--:|--:|");
        foreach (var scene in options.Scenes)
        {
            var s = Across(scene, skia);
            var i = Across(scene, impeller);
            string Gcs(Metrics? m) => m is null ? "–" : $"{F(m.Gen0, 0)} / {F(m.Gen1, 0)} / {F(m.Gen2, 0)}";
            sb.AppendLine($"| {scene} | {F(s?.AllocKbPerFrame, 1)} / {F(i?.AllocKbPerFrame, 1)} | " +
                          $"{F(s?.RenderAllocKbPerFrame, 1)} / {F(i?.RenderAllocKbPerFrame, 1)} | {Gcs(s)} | {Gcs(i)} | " +
                          $"{F(s?.GcPauseMsPerSecond)} / {F(i?.GcPauseMsPerSecond)} |");
        }
        sb.AppendLine();

        sb.AppendLine("### Process and GPU memory");
        sb.AppendLine();
        sb.AppendLine(options.Platform == "macos"
            ? "Footprint = physical footprint (as in Activity Monitor): native, managed and GPU memory counted against the process. "
            : "Footprint = resident set size. ");
        sb.AppendLine("Growth = change over the measured window; steady growth suggests a leak. " +
                      "GPU = memory allocated on the GPU device, where the platform reports it.");
        sb.AppendLine();
        sb.AppendLine("| Scene | Footprint MB S / I | Growth MB S / I | Peak footprint MB S / I | GPU MB S / I | GPU growth MB S / I | Managed heap MB S / I |");
        sb.AppendLine("|---|--:|--:|--:|--:|--:|--:|");
        foreach (var scene in options.Scenes)
        {
            var s = Across(scene, skia);
            var i = Across(scene, impeller);
            sb.AppendLine($"| {scene} | {F(s?.FootprintMb, 0)} / {F(i?.FootprintMb, 0)} | " +
                          $"{F(s?.FootprintGrowthMb, 1)} / {F(i?.FootprintGrowthMb, 1)} | " +
                          $"{F(s?.FootprintPeakMb, 0)} / {F(i?.FootprintPeakMb, 0)} | " +
                          $"{F(s?.GpuMemoryPeakMb, 0)} / {F(i?.GpuMemoryPeakMb, 0)} | " +
                          $"{F(s?.GpuMemoryGrowthMb, 1)} / {F(i?.GpuMemoryGrowthMb, 1)} | " +
                          $"{F(s?.ManagedHeapMb, 1)} / {F(i?.ManagedHeapMb, 1)} |");
        }
        sb.AppendLine();

        sb.AppendLine("## Startup");
        sb.AppendLine();
        sb.AppendLine("Process start to first presented frame, and peak resident set over the whole run.");
        sb.AppendLine();
        sb.AppendLine("| Scene | Startup ms S / I | Peak RSS MB S / I |");
        sb.AppendLine("|---|--:|--:|");
        foreach (var scene in options.Scenes)
        {
            var s = Across(scene, skia);
            var i = Across(scene, impeller);
            sb.AppendLine($"| {scene} | {F(s?.StartupMs, 0)} / {F(i?.StartupMs, 0)} | " +
                          $"{F(s?.PeakRssMb, 0)} / {F(i?.PeakRssMb, 0)} |");
        }

        sb.AppendLine();
        if (failures.Count > 0)
        {
            sb.AppendLine("## Failed runs");
            sb.AppendLine();
            foreach (var f in failures)
                sb.AppendLine($"- {f}");
            sb.AppendLine();
        }

        sb.AppendLine("Raw per-frame samples: `results.json`.");
        return sb.ToString();
    }

    private static void AppendSummary(StringBuilder sb, Options options, Func<string, string, string, Metrics?> get,
        string skia, string impeller)
    {
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("Geometric mean over scenes of Impeller / Skia. For times and memory, below 1 means Impeller is faster or uses less; for FPS, above 1 means Impeller is faster.");
        sb.AppendLine();
        sb.AppendLine("| Metric | " + string.Join(" | ", options.Modes) + " |");
        sb.AppendLine("|---|" + string.Concat(options.Modes.Select(_ => "--:|")));

        void Row(string name, Func<Metrics, double?> pick)
        {
            var cells = options.Modes.Select(mode =>
            {
                var ratios = options.Scenes
                    .Select(s => (S: get(s, mode, skia), I: get(s, mode, impeller)))
                    .Where(p => p.S is not null && p.I is not null)
                    .Select(p => pick(p.I!) / pick(p.S!))
                    .Where(r => r is > 0 && double.IsFinite(r.Value))
                    .Select(r => r!.Value)
                    .ToArray();
                return ratios.Length == 0 ? "–" : F(Math.Exp(ratios.Average(Math.Log)), 2);
            });
            sb.AppendLine($"| {name} | {string.Join(" | ", cells)} |");
        }

        Row("CPU frame p50", m => m.CpuP50);
        Row("CPU frame p95", m => m.CpuP95);
        Row("GPU frame p50", m => m.GpuP50);
        Row("FPS", m => m.Fps);
        Row("Allocations per frame", m => m.AllocKbPerFrame);
        Row("Process footprint", m => m.FootprintMb);
        Row("GPU memory (peak)", m => m.GpuMemoryPeakMb);
        sb.AppendLine();

        var mode = options.Modes.Contains("uncapped") ? "uncapped" : options.Modes[0];
        var ranked = options.Scenes
            .Select(s => (Scene: s, S: get(s, mode, skia), I: get(s, mode, impeller)))
            .Where(p => p.S is not null && p.I is not null)
            .Select(p => (p.Scene, Ratio: Cost(p.I!) / Cost(p.S!)))
            .Where(p => double.IsFinite(p.Ratio))
            .OrderBy(p => p.Ratio)
            .ToList();
        if (ranked.Count == 0)
            return;

        // Frame cost is the GPU time when both backends report it, since the CPU side waits on the GPU at present.
        static double Cost(Metrics m) => m.GpuP50 ?? m.CpuP50;
        var wins = ranked.Where(p => p.Ratio < 1).Take(3).Select(p => $"{p.Scene} ({F(p.Ratio, 2)}×)").ToList();
        var losses = ranked.Where(p => p.Ratio > 1).Reverse().Take(3).Select(p => $"{p.Scene} ({F(p.Ratio, 2)}×)")
            .ToList();
        sb.AppendLine($"Frame cost ({mode}, GPU p50 where known, else CPU p50), Impeller / Skia:");
        sb.AppendLine();
        sb.AppendLine($"- Largest Impeller wins: {(wins.Count > 0 ? string.Join(", ", wins) : "none")}");
        sb.AppendLine($"- Largest Impeller losses: {(losses.Count > 0 ? string.Join(", ", losses) : "none")}");
        sb.AppendLine();
    }

    private static double Percentile(double[] values, double p)
    {
        if (values.Length == 0)
            return double.NaN;
        var sorted = values.OrderBy(v => v).ToArray();
        var rank = p / 100 * (sorted.Length - 1);
        var lo = (int)Math.Floor(rank);
        var hi = (int)Math.Ceiling(rank);
        return sorted[lo] + (sorted[hi] - sorted[lo]) * (rank - lo);
    }

    private static string F(double? value, int digits = 2) =>
        value is { } v && double.IsFinite(v) ? v.ToString("F" + digits, CultureInfo.InvariantCulture) : "–";

    private static string Ratio(double? a, double? b) =>
        a is { } x && b is { } y && y > 0 && double.IsFinite(x / y) ? F(x / y) + "×" : "–";

    private static string Str(double v) => v.ToString(CultureInfo.InvariantCulture);

    private static string FindRepoRoot()
    {
        foreach (var start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
        {
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, "Avalonia.slnx")))
                    return dir.FullName;
        }

        throw new InvalidOperationException("Run from inside the Avalonia repository.");
    }

    private static string Git(string root, string args)
    {
        try
        {
            return Run("git", args.Split(' '), root, TimeSpan.FromSeconds(10), echo: false).Stdout.Trim();
        }
        catch (Exception)
        {
            return "unknown";
        }
    }

    private static string? Capture(string file, string args)
    {
        try
        {
            var result = Run(file, args.Split(' '), Environment.CurrentDirectory, TimeSpan.FromSeconds(10),
                echo: false);
            return result.ExitCode == 0 ? result.Stdout.Trim() : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string CpuModel()
    {
        if (OperatingSystem.IsMacOS())
            return Capture("sysctl", "-n machdep.cpu.brand_string") ?? "unknown";
        try
        {
            return File.ReadLines("/proc/cpuinfo").FirstOrDefault(l => l.StartsWith("model name"))
                ?.Split(':', 2)[1].Trim() ?? "unknown";
        }
        catch (IOException)
        {
            return "unknown";
        }
    }

    private static string OsName()
    {
        if (OperatingSystem.IsMacOS())
            return $"macOS {Capture("sw_vers", "-productVersion")} " +
                   $"({System.Runtime.InteropServices.RuntimeInformation.OSDescription})";
        try
        {
            var pretty = File.ReadLines("/etc/os-release").FirstOrDefault(l => l.StartsWith("PRETTY_NAME="))
                ?.Split('=', 2)[1].Trim('"');
            return $"{pretty ?? "Linux"} ({System.Runtime.InteropServices.RuntimeInformation.OSDescription})";
        }
        catch (IOException)
        {
            return System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        }
    }

    private static string LastLines(string text, int count) =>
        string.Join(" ", text.Split('\n', StringSplitOptions.RemoveEmptyEntries).TakeLast(count)).Trim();

    private static (int ExitCode, bool TimedOut, string Stdout, string Stderr) Run(string file,
        IEnumerable<string> args, string workDir, TimeSpan timeout, bool echo = true)
    {
        var psi = new ProcessStartInfo(file)
        {
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var a in args)
            psi.ArgumentList.Add(a);

        using var process = Process.Start(psi)!;
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null)
                return;
            stdout.AppendLine(e.Data);
            if (echo)
                Console.WriteLine("  " + e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                stderr.AppendLine(e.Data);
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit(timeout))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            return (-1, true, stdout.ToString(), stderr.ToString());
        }

        process.WaitForExit();
        return (process.ExitCode, false, stdout.ToString(), stderr.ToString());
    }
}
