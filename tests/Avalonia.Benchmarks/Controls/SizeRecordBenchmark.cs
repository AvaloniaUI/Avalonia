using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

#nullable enable

namespace Avalonia.Benchmarks.Controls
{
    /// <summary>
    /// Does a large per-item size record slow a measure pass down?
    /// </summary>
    /// <remarks>
    /// <para>
    /// The fork's <c>VirtualizingStackPanel</c> keeps one recorded size per item ever measured and
    /// claims a measure pass stays O(realized window) regardless, because the record is never swept
    /// — its sum is maintained incrementally at the single upsert site. This benchmark is that
    /// claim: the same 20 wheel steps, run once on a panel that has only ever seen the head of the
    /// collection and once on a panel that has already walked the whole thing.
    /// </para>
    /// <para>
    /// A per-pass sweep would show up as <c>Traversed=true</c> costing multiples of
    /// <c>Traversed=false</c>, growing with <see cref="ItemCount"/>. Flat rows mean the record is
    /// memory only. Stock has no record, so its two rows are flat by construction and give the
    /// reference shape.
    /// </para>
    /// </remarks>
    [MemoryDiagnoser]
    public class SizeRecordBenchmark : IDisposable
    {
        private const int WheelSteps = 20;

        private IDisposable? _app;
        private List<IBenchItem>? _items;
        private VirtualizationHarness? _harness;

        [Params(10_000, 100_000)]
        public int ItemCount { get; set; }

        /// <summary>Whether the whole collection was scrolled through before measuring.</summary>
        [Params(false, true)]
        public bool Traversed { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.RealFocus);
            _items = VirtualizationHarness.CreateItems(ItemCount, ItemSizeKind.Variable);
            _harness = new VirtualizationHarness(_items);

            if (Traversed)
                VirtualizationScenarios.TraverseEntireCollection(_harness);

            // Measure at the head of the collection either way, so the only difference between the
            // two rows is how much the panel remembers about the rest of it.
            _harness.ScrollTo(0);
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void SteadyStateWheelScroll()
        {
            for (var i = 1; i <= WheelSteps; ++i)
                _harness!.ScrollTo(i * VirtualizationScenarios.WheelStep);

            for (var i = WheelSteps - 1; i >= 0; --i)
                _harness!.ScrollTo(i * VirtualizationScenarios.WheelStep);
        }

        public void Dispose()
        {
            _app?.Dispose();
            _app = null;
        }
    }
}
