using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Avalonia.Harfbuzz;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using Avalonia.Skia;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Avalonia.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            // Profiling harness: bypasses BDN entirely so dotnet-trace's rundown
            // doesn't deadlock against BDN's measurement loop. Run as:
            //   dotnet-trace collect --providers Microsoft-DotNETCore-SampleProfiler
            //     --duration 00:00:25 -o trace.nettrace --
            //     dotnet Avalonia.Benchmarks.dll --profile-textlayout
            if (args.Contains("--profile-textlayout"))
            {
                ProfileTextLayout();
                return;
            }

            // Use reflection for a more maintainable way of creating the benchmark switcher,
            // Benchmarks are listed in namespace order first (e.g. BenchmarkDotNet.Samples.CPU,
            // BenchmarkDotNet.Samples.IL, etc) then by name, so the output is easy to understand
            var benchmarks = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                             .Any(m => m.GetCustomAttributes(typeof(BenchmarkAttribute), false).Any()))
                .OrderBy(t => t.Namespace)
                .ThenBy(t => t.Name)
                .ToArray();
            var benchmarkSwitcher = new BenchmarkSwitcher(benchmarks);
            IConfig config = null;

            if (args.Contains("--debug"))
            {
                config = new DebugInProcessConfig();
                var a = new List<string>(args);
                a.Remove("--debug");
                args = a.ToArray();
            }

            benchmarkSwitcher.Run(args, config);
        }

        private static void ProfileTextLayout()
        {
            // Bootstrap with the real Skia render interface (instead of the mock used
            // by TestServices.StyledWindow). That makes GlyphRunImpl construction —
            // which builds per-glyph absolute positions and walks the ink bounds —
            // appear on the hot path, so we can see whether the render-side prefix
            // sum is a real production cost. The mock platform returns trivial
            // values and hides this work.
            var services = TestServices.StyledWindow
                .With(
                    renderInterface: new PlatformRenderInterface(),
                    textShaperImpl: new HarfBuzzTextShaper(),
                    fontManagerImpl: new FontManagerImpl());
            using var app = UnitTestApplication.Start(services);

            // Real Avalonia consumers (e.g. TextBlock) share a TextRunCache across
            // re-layouts of the same string, so shaping cost amortises after the
            // first build. Profiling without a cache makes HarfBuzz dominate the
            // trace and hides the steady-state hot paths that matter.
            var cache = new TextRunCache();

            // Warm up the JIT, font loading, trie data, tiered compilation — and
            // populate the cache so the measurement loop runs cache-warm like a
            // real Avalonia paint pass would.
            for (var i = 0; i < 500; i++)
            {
                BuildOne(cache).Dispose();
            }

            // Steady-state measurement loop. ~18 s is well within dotnet-trace's
            // typical --duration 00:00:25 window, so the harness exits before the
            // collector — that gives the EventPipe rundown an idle target to flush
            // against, avoiding the back-pressure deadlock that happens when the
            // target is mid-loop at rundown time.
            var sw = Stopwatch.StartNew();
            var count = 0;
            while (sw.Elapsed < TimeSpan.FromSeconds(18))
            {
                BuildOne(cache).Dispose();
                count++;
            }
            sw.Stop();

            Console.WriteLine($"Built {count} layouts in {sw.Elapsed.TotalSeconds:F1}s " +
                              $"({sw.Elapsed.TotalMilliseconds / count:F3} ms/op).");
        }

        private static TextLayout BuildOne(TextRunCache cache)
        {
            return new TextLayout(
                Text.HugeTextLayout.EmojisText,
                Typeface.Default,
                12d,
                Brushes.Black,
                maxWidth: 120,
                textTrimming: TextTrimming.None,
                textWrapping: TextWrapping.WrapWithOverflow,
                textRunCache: cache);
        }
    }
}
