using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using UIKit;

namespace Avalonia.Benchmarks.iOS;

/// <summary>
/// Benchmarks for iOS hot paths used in the Avalonia iOS backend.
/// Results are output to NSLog (visible in Console.app / Xcode) and displayed on screen.
/// </summary>
[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    private const string Tag = "AvaloniaBench";
    private const int WarmupIterations = 100;
    private const int Iterations = 10_000;

    private UITextView? _output;

    public override UIWindow? Window { get; set; }

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        Window = new UIWindow(UIScreen.MainScreen.Bounds);

        _output = new UITextView(Window.Bounds)
        {
            Editable = false,
            Font = UIFont.FromName("Menlo", 11) ?? UIFont.SystemFontOfSize(11),
            TextColor = UIColor.Label,
            BackgroundColor = UIColor.SystemBackground
        };

        var viewController = new UIViewController();
        viewController.View!.AddSubview(_output);
        _output.TranslatesAutoresizingMaskIntoConstraints = false;
        NSLayoutConstraint.ActivateConstraints(new[]
        {
            _output.TopAnchor.ConstraintEqualTo(viewController.View.SafeAreaLayoutGuide.TopAnchor),
            _output.BottomAnchor.ConstraintEqualTo(viewController.View.SafeAreaLayoutGuide.BottomAnchor),
            _output.LeadingAnchor.ConstraintEqualTo(viewController.View.SafeAreaLayoutGuide.LeadingAnchor),
            _output.TrailingAnchor.ConstraintEqualTo(viewController.View.SafeAreaLayoutGuide.TrailingAnchor)
        });

        Window.RootViewController = viewController;
        Window.MakeKeyAndVisible();

        Task.Run(RunBenchmarks);

        return true;
    }

    private void RunBenchmarks()
    {
        try
        {
            Emit("=== Avalonia iOS Benchmarks ===");
            Emit($"Warmup: {WarmupIterations}, Iterations: {Iterations}");
            Emit($"Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
            Emit("");

            RunDispatcherSignalBenchmark();
            RunTimestampBenchmarks();
            RunNSStringCacheBenchmark();

            Emit("=== All benchmarks complete ===");
        }
        catch (Exception ex)
        {
            Emit($"FAILED: {ex}");
        }
    }

    // DispatcherImpl.Signal / CheckSignaled
    // Interlocked.CompareExchange + Interlocked.Exchange (replaces lock + bool)
    private void RunDispatcherSignalBenchmark()
    {
        Emit("[Dispatcher Signal (DispatcherImpl)]");
        var lockObj = new object();

        Measure("lock signal/reset", () =>
        {
            int signaled = 0;
            bool flag = false;
            for (int i = 0; i < Iterations; i++)
            {
                lock (lockObj)
                {
                    if (!flag)
                    {
                        flag = true;
                        signaled++;
                    }
                }
                lock (lockObj)
                {
                    flag = false;
                }
            }
            return signaled;
        });

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

    // InputHandler scroll event timestamps
    // Environment.TickCount64 (replaces DateTimeOffset.UtcNow.ToUnixTimeMilliseconds)
    // Alternatives tested for comparison
    private void RunTimestampBenchmarks()
    {
        Emit("[Scroll Timestamp (InputHandler)]");

        Measure("Environment.TickCount64", () =>
        {
            ulong result = 0;
            for (int i = 0; i < Iterations; i++)
            {
                result = (ulong)Environment.TickCount64;
            }
            return result;
        });

        Measure("Alt: DateTimeOffset.UtcNow.ToUnixTimeMs", () =>
        {
            ulong result = 0;
            for (int i = 0; i < Iterations; i++)
            {
                result = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
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

        Emit("");
    }

    // TextInputResponder.TextInputContextIdentifier
    // Cached NSString field (replaces new NSString(Guid.NewGuid().ToString()) per access)
    private void RunNSStringCacheBenchmark()
    {
        Emit("[TextInputContextIdentifier (TextInputResponder)]");

        Measure("new NSString(Guid) per call", () =>
        {
            NSString? last = null;
            for (int i = 0; i < Iterations; i++)
            {
                last = new NSString(Guid.NewGuid().ToString());
            }
            return last;
        });

        var cached = new NSString(Guid.NewGuid().ToString());
        Measure("Cached NSString field", () =>
        {
            NSString? last = null;
            for (int i = 0; i < Iterations; i++)
            {
                last = cached;
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
        NSLog($"{Tag}: {text}");
        InvokeOnMainThread(() =>
        {
            if (_output != null)
            {
                _output.Text += text + "\n";
            }
        });
    }

    private static void NSLog(string message)
    {
        Console.WriteLine(message);
    }
}
