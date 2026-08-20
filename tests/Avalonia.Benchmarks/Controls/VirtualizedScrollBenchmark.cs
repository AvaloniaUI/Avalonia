using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

#nullable enable

namespace Avalonia.Benchmarks.Controls
{
    /// <summary>
    /// Scroll cost for a virtualized <c>ItemsControl</c>: first layout, wheel scrolling down and
    /// back, and scrollbar-style jumps. Uses stock API only, so the same file can be run in a
    /// worktree at the merge-base to produce the "vs. stock" half of the comparison.
    /// </summary>
    [MemoryDiagnoser]
    public class VirtualizedScrollBenchmark : IDisposable
    {
        private IDisposable? _app;
        private List<IBenchItem>? _items;
        private VirtualizationHarness? _harness;
        private double[]? _jumpOffsets;

        [Params(1_000, 100_000)]
        public int ItemCount { get; set; }

        [Params(ItemSizeKind.Uniform, ItemSizeKind.Variable)]
        public ItemSizeKind Sizes { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.RealFocus);
            _items = VirtualizationHarness.CreateItems(ItemCount, Sizes);
            _harness = new VirtualizationHarness(_items);
            _jumpOffsets = VirtualizationScenarios.CreateJumpOffsets(_items, _harness.ViewportHeight);
        }

        /// <summary>Building the control and running the first layout pass — the startup cost a page pays.</summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public object FirstLayout() => new VirtualizationHarness(_items!);

        /// <summary>
        /// 40 wheel steps down and 40 back. The upward half is where a stock panel treats every
        /// step as a disjunct viewport and recycles the entire realized set.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ScrollDownAndBack() => VirtualizationScenarios.ScrollDownAndBack(_harness!);

        /// <summary>20 jumps spread across the whole collection.</summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void JumpToOffsets() => VirtualizationScenarios.JumpToOffsets(_harness!, _jumpOffsets!);

        public void Dispose()
        {
            _app?.Dispose();
            _app = null;
        }
    }
}
