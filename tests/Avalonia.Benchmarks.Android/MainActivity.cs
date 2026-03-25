using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Widget;

namespace Avalonia.Benchmarks.Android;

/// <summary>
/// Benchmarks for Android hot paths used in the Avalonia Android backend.
/// Results are output to logcat (tag: AvaloniaBench) and displayed on screen.
/// </summary>
[Activity(Label = "Avalonia.Benchmarks", MainLauncher = true, Exported = true)]
public class MainActivity : global::Android.App.Activity
{
    private const string Tag = "AvaloniaBench";
    private const int WarmupIterations = 100;
    private const int Iterations = 10_000;

    private TextView? _output;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var scroll = new ScrollView(this);
        var layout = new LinearLayout(this) { Orientation = Orientation.Vertical };
        layout.SetPadding(32, 32, 32, 32);

        _output = new TextView(this) { TextSize = 11 };
        _output.Typeface = Typeface.Monospace;
        layout.AddView(_output);
        scroll.AddView(layout);

        SetContentView(scroll);

        Task.Run(RunBenchmarks);
    }

    private void RunBenchmarks()
    {
        try
        {
            Emit("=== Avalonia Android Benchmarks ===");
            Emit($"Warmup: {WarmupIterations}, Iterations: {Iterations}");
            Emit($"Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
            Emit("");

            RunDispatchDrawBenchmark();
            RunDispatcherSignalBenchmark();
            RunTimestampBenchmarks();
            RunAccessibilityActionBenchmark();
            RunKeyboardSymbolBenchmark();

            Emit("=== All benchmarks complete ===");
        }
        catch (Exception ex)
        {
            Emit($"FAILED: {ex}");
        }
    }

    // TopLevelImpl.SurfaceViewImpl.DispatchDraw
    // Cached Paint with BlendMode.Clear (API 29+)
    private void RunDispatchDrawBenchmark()
    {
        Emit("[DispatchDraw (TopLevelImpl.SurfaceViewImpl)]");

        Paint? cached = null;
        Measure("Cached Paint + BlendMode.Clear", () =>
        {
            cached = null;
            for (int i = 0; i < Iterations; i++)
            {
                if (cached == null)
                {
                    cached = new Paint();
                    cached.SetColor(0);
                    if (OperatingSystem.IsAndroidVersionAtLeast(29))
                    {
                        cached.BlendMode = BlendMode.Clear;
                    }
                }
            }
            return cached;
        });

        Emit("");
    }

    // AndroidDispatcherImpl.Signal / OnSignaled / OnIdle
    // Interlocked.CompareExchange + Interlocked.Exchange
    private void RunDispatcherSignalBenchmark()
    {
        Emit("[Dispatcher Signal (AndroidDispatcherImpl)]");

        Measure("Interlocked signal/reset", () =>
        {
            int signaled = 0;
            int flag = 0;
            for (int i = 0; i < Iterations; i++)
            {
                if (Interlocked.CompareExchange(ref flag, 1, 0) == 0)
                {
                    signaled++;
                }
                Interlocked.Exchange(ref flag, 0);
            }
            return signaled;
        });

        Emit("");
    }

    // TopLevelImpl.TextInput timestamp
    // SystemClock.UptimeMillis() (monotonic ms, matches KeyEvent.EventTime)
    // Alternatives tested for comparison
    private void RunTimestampBenchmarks()
    {
        Emit("[TextInput Timestamp (TopLevelImpl.TextInput)]");

        Measure("SystemClock.UptimeMillis", () =>
        {
            ulong result = 0;
            for (int i = 0; i < Iterations; i++)
            {
                result = (ulong)SystemClock.UptimeMillis();
            }
            return result;
        });

        Measure("Alt: DateTime.UtcNow.Ticks", () =>
        {
            ulong result = 0;
            for (int i = 0; i < Iterations; i++)
            {
                result = (ulong)DateTime.UtcNow.Ticks;
            }
            return result;
        });

        Measure("Alt: Stopwatch.GetTimestamp", () =>
        {
            long result = 0;
            for (int i = 0; i < Iterations; i++)
            {
                result = Stopwatch.GetTimestamp();
            }
            return result;
        });

        Measure("Alt: Environment.TickCount64", () =>
        {
            long result = 0;
            for (int i = 0; i < Iterations; i++)
            {
                result = System.Environment.TickCount64;
            }
            return result;
        });

        Emit("");
    }

    // AvaloniaAccessHelper.OnPerformActionForVirtualView
    // foreach loop with null check
    private void RunAccessibilityActionBenchmark()
    {
        Emit("[Accessibility Action (AvaloniaAccessHelper)]");
        var providers = new List<int> { 1, 2, 3 };

        Measure("foreach loop", () =>
        {
            bool result = false;
            for (int i = 0; i < Iterations; i++)
            {
                foreach (var p in providers)
                {
                    result |= p > 1;
                }
            }
            return result;
        });

        Emit("");
    }

    // AndroidKeyboardEventsHelper.CharToString
    // Pre-cached ASCII string[128] with fallback to char.ConvertFromUtf32
    private void RunKeyboardSymbolBenchmark()
    {
        Emit("[Key Symbol (AndroidKeyboardEventsHelper)]");

        var asciiCache = new string[128];
        for (int i = 0; i < 128; i++)
        {
            asciiCache[i] = ((char)i).ToString();
        }

        Measure("Cached ASCII lookup", () =>
        {
            string? last = null;
            for (int i = 0; i < Iterations; i++)
            {
                int code = 32 + (i % 95);
                if (code >= 0 && code < asciiCache.Length)
                {
                    last = asciiCache[code];
                }
                else
                {
                    last = char.ConvertFromUtf32(code);
                }
            }
            return last;
        });

        Emit("");
    }

    private void Measure(string name, Func<object?> action)
    {
        for (int w = 0; w < WarmupIterations; w++)
        {
            action();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var allocBefore = GC.GetTotalAllocatedBytes(precise: true);
        var sw = Stopwatch.StartNew();

        action();

        sw.Stop();
        var allocAfter = GC.GetTotalAllocatedBytes(precise: true);
        var allocBytes = allocAfter - allocBefore;

        var perCallNs = (double)sw.ElapsedTicks / Stopwatch.Frequency * 1_000_000_000 / Iterations;

        var line = $"  {name,-42} {sw.Elapsed.TotalMilliseconds,8:F2} ms | {perCallNs,8:F1} ns/call | {allocBytes,8:N0} B";
        Emit(line);
    }

    private void Emit(string text)
    {
        Log.Info(Tag, text);
        RunOnUiThread(() => _output!.Append(text + "\n"));
    }
}
