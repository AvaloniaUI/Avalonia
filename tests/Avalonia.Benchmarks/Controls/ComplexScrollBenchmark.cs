using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Controls.Templates;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

#nullable enable

namespace Avalonia.Benchmarks.Controls
{
    /// <summary>
    /// Container-level virtualization off vs. on, over a heterogeneous list of complex nested rows.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the comparison the feature is actually for. Over a one-visual template there is
    /// nothing to save by keeping a child attached — rebuilding it costs a single allocation — so a
    /// trivial template measures the panel and says nothing about the optimization. These rows are
    /// 10–20 visuals deep with bound text, which is what a form or feed row really looks like.
    /// </para>
    /// <para>
    /// The <c>Plain</c> arm is the "feature does not exist" side: a template that does not opt in
    /// takes stock's <c>DefaultRecycleKey</c> path and has its content cleared on recycle, so it
    /// rebuilds. Running this file at the merge-base too shows that arm really is stock, rather than
    /// only being claimed to be.
    /// </para>
    /// <para>
    /// Desktop x64 understates this. The saved work is subtree construction, binding setup and text
    /// layout — all of which cost proportionally more on a phone, which is where this fork's list
    /// actually runs.
    /// </para>
    /// </remarks>
    [MemoryDiagnoser]
    public class ComplexScrollBenchmark : IDisposable
    {
        private IDisposable? _app;
        private List<IBenchItem>? _items;
        private VirtualizationHarness? _harness;
        private double[]? _jumpOffsets;

        [Params(5_000)]
        public int ItemCount { get; set; }

        /// <summary>
        /// Sourced rather than hard-coded so this file is stock API too: at the merge-base the
        /// opt-in template is absent and this yields <c>Plain</c> alone, which is exactly the arm a
        /// baseline run should measure.
        /// </summary>
        public IEnumerable<TemplateMode> Modes => ComplexItems.AvailableModes;

        [ParamsSource(nameof(Modes))]
        public TemplateMode Mode { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.RealFocus);
            _items = ComplexItems.CreateItems(ItemCount);
            _harness = CreateHarness();
            _jumpOffsets = VirtualizationScenarios.CreateJumpOffsets(_items, _harness.ViewportHeight);
        }

        internal VirtualizationHarness CreateHarness() => ComplexItems.CreateHarness(_items!, Mode);

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ScrollDownAndBack() => VirtualizationScenarios.ScrollDownAndBack(_harness!);

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
