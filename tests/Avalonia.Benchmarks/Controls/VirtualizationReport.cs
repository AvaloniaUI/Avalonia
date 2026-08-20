using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Avalonia.UnitTests;

#nullable enable

namespace Avalonia.Benchmarks.Controls
{
    /// <summary>
    /// Prints the deterministic half of the virtualization numbers: how many containers the panel
    /// prepares, clears and measures for a given scroll pattern, and how much memory it retains
    /// after walking a large collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These are counts, not timings — they do not vary between runs, so BenchmarkDotNet's
    /// statistical machinery buys nothing and its warmup would pollute the counters. Timings are in
    /// <see cref="VirtualizedScrollBenchmark"/> and <see cref="SizeRecordBenchmark"/>; the
    /// milliseconds printed here are only there to keep a count next to its rough cost.
    /// </para>
    /// <para>
    /// Run as <c>dotnet Avalonia.Benchmarks.dll --virtualization-report</c>. To get before/after
    /// figures, run it again in a worktree at <c>git merge-base master HEAD</c> with this directory
    /// copied in — everything here is stock API. Do not use <c>git stash</c> for the baseline: it
    /// leaves the branch's commits in place.
    /// </para>
    /// </remarks>
    internal static class VirtualizationReport
    {
        private static readonly int[] ItemCounts = { 1_000, 100_000 };

        public static void Run()
        {
            using var app = UnitTestApplication.Start(TestServices.RealFocus);

            Warmup();

            Console.WriteLine("## Container churn");
            Console.WriteLine();
            Console.WriteLine("| Items | Sizes | Scenario | Layout passes | Prepares | Clears | Container measures | ms |");
            Console.WriteLine("|---|---|---|---:|---:|---:|---:|---:|");

            foreach (var itemCount in ItemCounts)
            {
                foreach (var sizes in new[] { ItemSizeKind.Uniform, ItemSizeKind.Variable })
                    ReportChurn(itemCount, sizes);
            }

            Console.WriteLine();
            Console.WriteLine("## Container-level virtualization, complex heterogeneous rows");
            Console.WriteLine();
            Console.WriteLine("`Plain` = template does not opt in, i.e. stock recycling: one shared pool, content");
            Console.WriteLine("cleared, subtree rebuilt. `Virtualized` = IVirtualizingDataTemplate keyed by row kind.");
            Console.WriteLine("`Child builds` counts subtree constructions; `Visuals` counts the controls in them.");
            Console.WriteLine();
            Console.WriteLine("| Items | Mode | Scenario | Prepares | Child builds | Visuals | ms |");
            Console.WriteLine("|---|---|---|---:|---:|---:|---:|");

            // Only the Plain arm exists at the merge-base, where the opt-in template does not.
            foreach (var mode in ComplexItems.AvailableModes)
                ReportComplex(5_000, mode);

            Console.WriteLine();
            Console.WriteLine("## Memory retained by a live panel");
            Console.WriteLine();
            Console.WriteLine("Managed bytes held after the item collection itself is excluded — the panel, its");
            Console.WriteLine("containers, its recycle pool and (on this branch) its per-item size record.");
            Console.WriteLine();
            Console.WriteLine("| Items | At the head | After a full traversal | Delta | Delta/item |");
            Console.WriteLine("|---|---:|---:|---:|---:|");

            foreach (var itemCount in ItemCounts)
                ReportMemory(itemCount);
        }

        /// <summary>
        /// Drive the whole harness once before reporting anything. The counts do not need it, but
        /// the milliseconds column does: without it the first row absorbs JIT, static
        /// initialization and tiered-compilation cost and reads an order of magnitude slower than
        /// the identical row below it.
        /// </summary>
        private static void Warmup()
        {
            var items = VirtualizationHarness.CreateItems(200, ItemSizeKind.Variable);
            var harness = new VirtualizationHarness(items);

            VirtualizationScenarios.ScrollDownAndBack(harness);
            VirtualizationScenarios.JumpToOffsets(
                harness,
                VirtualizationScenarios.CreateJumpOffsets(items, harness.ViewportHeight));
            VirtualizationScenarios.TraverseEntireCollection(harness);
        }

        private static void ReportChurn(int itemCount, ItemSizeKind sizes)
        {
            var items = VirtualizationHarness.CreateItems(itemCount, sizes);

            var clock = Stopwatch.StartNew();
            var harness = new VirtualizationHarness(items);
            clock.Stop();
            WriteRow(itemCount, sizes, "First layout", harness.Counters, clock);

            var jumpOffsets = VirtualizationScenarios.CreateJumpOffsets(items, harness.ViewportHeight);

            harness.Counters.Reset();
            clock.Restart();
            for (var i = 1; i <= VirtualizationScenarios.WheelSteps; ++i)
                harness.ScrollTo(i * VirtualizationScenarios.WheelStep);
            clock.Stop();
            WriteRow(itemCount, sizes,
                $"Wheel down ({VirtualizationScenarios.WheelSteps}x{VirtualizationScenarios.WheelStep:0}px)",
                harness.Counters, clock);

            // The upward half is where RetainMatchingContainers can act: scrolling back puts the
            // anchor before FirstIndex, which is what marks a viewport disjunct. Note the measured
            // result is nothing like "stock re-prepares the whole window on every backwards step" —
            // it does not; the disjunct branch fires on only a few of these steps. Read the numbers.
            harness.Counters.Reset();
            clock.Restart();
            for (var i = VirtualizationScenarios.WheelSteps - 1; i >= 0; --i)
                harness.ScrollTo(i * VirtualizationScenarios.WheelStep);
            clock.Stop();
            WriteRow(itemCount, sizes,
                $"Wheel up ({VirtualizationScenarios.WheelSteps}x{VirtualizationScenarios.WheelStep:0}px)",
                harness.Counters, clock);

            // Half a viewport at a time. Between the wheel (which mostly stays inside the realized
            // window) and the jumps (which share no items with it), this is the shape retention is
            // for: a new window that overlaps the old one by about half.
            harness.ScrollTo(0);
            harness.Counters.Reset();
            clock.Restart();
            for (var i = 1; i <= VirtualizationScenarios.PageSteps; ++i)
                harness.ScrollTo(i * VirtualizationScenarios.PageStep);
            clock.Stop();
            WriteRow(itemCount, sizes,
                $"Page down ({VirtualizationScenarios.PageSteps}x{VirtualizationScenarios.PageStep:0}px)",
                harness.Counters, clock);

            harness.Counters.Reset();
            clock.Restart();
            for (var i = VirtualizationScenarios.PageSteps - 1; i >= 0; --i)
                harness.ScrollTo(i * VirtualizationScenarios.PageStep);
            clock.Stop();
            WriteRow(itemCount, sizes,
                $"Page up ({VirtualizationScenarios.PageSteps}x{VirtualizationScenarios.PageStep:0}px)",
                harness.Counters, clock);

            harness.Counters.Reset();
            clock.Restart();
            VirtualizationScenarios.JumpToOffsets(harness, jumpOffsets);
            clock.Stop();
            WriteRow(itemCount, sizes, $"Jumps ({jumpOffsets.Length})", harness.Counters, clock);
        }

        private static void WriteRow(
            int itemCount,
            ItemSizeKind sizes,
            string scenario,
            VirtualizationCounters counters,
            Stopwatch clock)
        {
            Console.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "| {0:N0} | {1} | {2} | {3:N0} | {4:N0} | {5:N0} | {6:N0} | {7:F1} |",
                itemCount,
                sizes,
                scenario,
                counters.LayoutPasses,
                counters.Prepares,
                counters.Clears,
                counters.ContainerMeasures,
                clock.Elapsed.TotalMilliseconds));
        }

        private static void ReportComplex(int itemCount, TemplateMode mode)
        {
            var items = ComplexItems.CreateItems(itemCount);

            var clock = Stopwatch.StartNew();
            var harness = ComplexItems.CreateHarness(items, mode);
            clock.Stop();
            WriteComplexRow(itemCount, mode, "First layout", harness.Counters, clock);

            var jumpOffsets = VirtualizationScenarios.CreateJumpOffsets(items, harness.ViewportHeight);

            harness.Counters.Reset();
            clock.Restart();
            VirtualizationScenarios.ScrollDownAndBack(harness);
            clock.Stop();
            WriteComplexRow(itemCount, mode, "Wheel down + up (80 steps)", harness.Counters, clock);

            // Half-viewport paging is where RetainMatchingContainers was seen to act at all, so the
            // complex arm has to include it: this is where an avoided prepare avoids a whole
            // subtree rebuild rather than one Canvas.
            harness.ScrollTo(0);
            harness.Counters.Reset();
            clock.Restart();
            for (var i = 1; i <= VirtualizationScenarios.PageSteps; ++i)
                harness.ScrollTo(i * VirtualizationScenarios.PageStep);
            for (var i = VirtualizationScenarios.PageSteps - 1; i >= 0; --i)
                harness.ScrollTo(i * VirtualizationScenarios.PageStep);
            clock.Stop();
            WriteComplexRow(itemCount, mode, "Page down + up (20 steps)", harness.Counters, clock);

            harness.Counters.Reset();
            clock.Restart();
            VirtualizationScenarios.JumpToOffsets(harness, jumpOffsets);
            clock.Stop();
            WriteComplexRow(itemCount, mode, $"Jumps ({jumpOffsets.Length})", harness.Counters, clock);
        }

        private static void WriteComplexRow(
            int itemCount,
            TemplateMode mode,
            string scenario,
            VirtualizationCounters counters,
            Stopwatch clock)
        {
            Console.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "| {0:N0} | {1} | {2} | {3:N0} | {4:N0} | {5:N0} | {6:F1} |",
                itemCount,
                mode,
                scenario,
                counters.Prepares,
                counters.ChildBuilds,
                counters.VisualsCreated,
                clock.Elapsed.TotalMilliseconds));
        }

        private static void ReportMemory(int itemCount)
        {
            // Built once and kept alive across both measurements, so the items themselves cancel
            // out of the delta and what is left is what the panel keeps.
            var items = VirtualizationHarness.CreateItems(itemCount, ItemSizeKind.Variable);

            var atHead = RetainedBytes(() => new VirtualizationHarness(items));
            var traversed = RetainedBytes(() =>
            {
                var harness = new VirtualizationHarness(items);
                VirtualizationScenarios.TraverseEntireCollection(harness);
                return harness;
            });

            GC.KeepAlive(items);

            var delta = traversed - atHead;

            Console.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "| {0:N0} | {1:N0} | {2:N0} | {3:N0} | {4:F1} |",
                itemCount,
                atHead,
                traversed,
                delta,
                (double)delta / itemCount));
        }

        private static long RetainedBytes(Func<object> build)
        {
            Settle();
            var before = GC.GetTotalMemory(true);

            var kept = build();

            Settle();
            var after = GC.GetTotalMemory(true);

            GC.KeepAlive(kept);
            return after - before;

            static void Settle()
            {
                for (var i = 0; i < 3; ++i)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
        }
    }
}
