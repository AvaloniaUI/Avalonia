using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.UnitTests;

#nullable enable

namespace Avalonia.Benchmarks.Controls
{
    // Public only because it is used as a BenchmarkDotNet [Params] type on a public field.
    public enum ItemSizeKind
    {
        /// <summary>Every item the same height — the case stock's realized-window average is exact for.</summary>
        Uniform,

        /// <summary>Deterministic 20..120px spread — the case the per-item size record exists for.</summary>
        Variable,
    }

    /// <summary>
    /// Anything the harness can lay out. <see cref="Height"/> is what the item's template resolves
    /// to along the scrolling axis, so scroll offsets can be computed from the data rather than from
    /// whatever the panel currently estimates the extent to be.
    /// </summary>
    internal interface IBenchItem
    {
        double Height { get; }
    }

    internal sealed class SizedItem : IBenchItem
    {
        public SizedItem(int index, double height)
        {
            Caption = "Item " + index;
            Height = height;
        }

        public string Caption { get; }

        public double Height { get; }
    }

    internal sealed class VirtualizationCounters
    {
        public int Prepares;
        public int Clears;
        public int ContainerMeasures;
        public int LayoutPasses;

        /// <summary>
        /// Times a template actually built a child visual tree. This is the number container-level
        /// virtualization exists to drive down: keeping container and child together as one
        /// reusable unit means a recycled container does not rebuild its subtree.
        /// </summary>
        public int ChildBuilds;

        /// <summary>Visuals constructed while building those subtrees.</summary>
        public int VisualsCreated;

        public void Reset() =>
            Prepares = Clears = ContainerMeasures = LayoutPasses = ChildBuilds = VisualsCreated = 0;
    }

    /// <summary>
    /// Counts the container lifecycle calls the panel drives. <c>PrepareContainerForItemOverride</c>
    /// is the expensive one — it is what <c>RetainMatchingContainers</c> claims to avoid on a
    /// disjunct viewport jump, and the claim has never been measured.
    /// </summary>
    internal sealed class CountingItemsControl : ItemsControl
    {
        public VirtualizationCounters Counters { get; set; } = new();

        // `protected`, not `protected internal`: from outside Avalonia.Controls the internal half
        // of the base member's accessibility is not visible, so it cannot be repeated here.
        protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
        {
            Counters.Prepares++;
            base.PrepareContainerForItemOverride(container, item, index);
        }

        protected override void ClearContainerForItemOverride(Control container)
        {
            Counters.Clears++;
            base.ClearContainerForItemOverride(container);
        }
    }

    /// <summary>
    /// A rooted, scrollable, virtualized <see cref="ItemsControl"/> driven the same way
    /// <c>VirtualizingStackPanelTests</c> drives one: set <see cref="ScrollViewer.Offset"/>, then run
    /// a layout pass.
    /// </summary>
    /// <remarks>
    /// Deliberately built from stock API only — no <c>EnableVirtualization</c>, no
    /// <c>IVirtualizingDataTemplate</c>, no warmup — so this file compiles and runs unchanged in a
    /// worktree at <c>git merge-base master HEAD</c>. That is the only way to get honest before/after
    /// numbers: run the same benchmark on both, not a toggle inside one build.
    /// </remarks>
    internal sealed class VirtualizationHarness
    {
        public VirtualizationHarness(
            IReadOnlyList<IBenchItem> items,
            IDataTemplate? itemTemplate = null,
            // Templates are built before the harness exists, so one that counts its own child builds
            // has to be handed the same counter set the harness will report from.
            VirtualizationCounters? counters = null,
            double viewportWidth = 400,
            double viewportHeight = 600,
            double cacheLength = 0)
        {
            Items = items;
            Counters = counters ??= new VirtualizationCounters();

            Panel = new VirtualizingStackPanel
            {
                Orientation = Orientation.Vertical,
                CacheLength = cacheLength,
            };

            var presenter = new ItemsPresenter
            {
                [~ItemsPresenter.ItemsPanelProperty] = new TemplateBinding(ItemsPresenter.ItemsPanelProperty),
            };

            Scroll = new ScrollViewer
            {
                Name = "PART_ScrollViewer",
                Content = presenter,
                Template = ScrollViewerTemplate(),
            };

            ItemsControl = new CountingItemsControl
            {
                Counters = counters,
                ItemsSource = items,
                Template = new FuncControlTemplate<CountingItemsControl>((_, ns) => Scroll.RegisterInNameScope(ns)),
                ItemsPanel = new FuncTemplate<Panel?>(() => Panel),
                // Sizes must come from bindings, not from the item passed to the build function: a
                // recycled container keeps its child, so the build function runs once per container
                // while the item behind it changes many times.
                ItemTemplate = itemTemplate ?? new FuncDataTemplate<SizedItem>((_, _) =>
                {
                    counters.ChildBuilds++;
                    counters.VisualsCreated++;
                    return new MeasureCountingCanvas(counters)
                    {
                        Width = 100,
                        [!Layoutable.HeightProperty] = new Binding(nameof(SizedItem.Height)),
                    };
                }),
            };

            Root = new TestRoot(false, ItemsControl)
            {
                ClientSize = new Size(viewportWidth, viewportHeight),
                Renderer = new NullRenderer(),
            };

            Counters.LayoutPasses++;
            Root.LayoutManager.ExecuteInitialLayoutPass();
        }

        public IReadOnlyList<IBenchItem> Items { get; }

        public VirtualizationCounters Counters { get; }

        public VirtualizingStackPanel Panel { get; }

        public ScrollViewer Scroll { get; }

        public CountingItemsControl ItemsControl { get; }

        public TestRoot Root { get; }

        public double ViewportHeight => Root.ClientSize.Height;

        public void ScrollTo(double offsetY)
        {
            Scroll.Offset = new Vector(0, offsetY);
            Layout();
        }

        public void Layout()
        {
            Counters.LayoutPasses++;
            Root.LayoutManager.ExecuteLayoutPass();
        }

        public static List<IBenchItem> CreateItems(int count, ItemSizeKind kind)
        {
            var items = new List<IBenchItem>(count);
            // Fixed seed: the same collection on every run and on both branches, so a count
            // difference is the panel's doing and not the data's.
            var random = new Random(20259);

            for (var i = 0; i < count; ++i)
            {
                var height = kind == ItemSizeKind.Uniform ? 40d : 20d + random.Next(0, 101);
                items.Add(new SizedItem(i, height));
            }

            return items;
        }

        /// <summary>The true summed height of <paramref name="items"/>, independent of any estimate.</summary>
        public static double TotalHeight(IReadOnlyList<IBenchItem> items)
        {
            var total = 0d;

            for (var i = 0; i < items.Count; ++i)
                total += items[i].Height;

            return total;
        }

        private static IControlTemplate ScrollViewerTemplate()
        {
            return new FuncControlTemplate<ScrollViewer>((_, ns) =>
                new ScrollContentPresenter
                {
                    Name = "PART_ScrollContentPresenter",
                }.RegisterInNameScope(ns));
        }

        private sealed class MeasureCountingCanvas : Canvas
        {
            private readonly VirtualizationCounters _counters;

            public MeasureCountingCanvas(VirtualizationCounters counters) => _counters = counters;

            protected override Size MeasureOverride(Size availableSize)
            {
                _counters.ContainerMeasures++;
                return base.MeasureOverride(availableSize);
            }
        }
    }

    /// <summary>
    /// The scroll patterns the benchmarks and the count report share, so a timing figure and a
    /// container-count figure always describe the same work.
    /// </summary>
    internal static class VirtualizationScenarios
    {
        /// <summary>Roughly a mouse-wheel notch.</summary>
        public const double WheelStep = 120;

        public const int WheelSteps = 40;

        public const int JumpCount = 20;

        /// <summary>Half a viewport — a PageUp/PageDown, and the overlap case retention targets.</summary>
        public const double PageStep = 300;

        public const int PageSteps = 10;

        /// <summary>
        /// Wheel-scroll down and back up again. The way back is the interesting half: a backwards
        /// scroll can put the anchor before <c>FirstIndex</c>, which marks the viewport
        /// <em>disjunct</em> and recycles the whole realized set even though the new window overlaps
        /// the old one almost completely. That is the case <c>RetainMatchingContainers</c> targets.
        /// </summary>
        public static void ScrollDownAndBack(VirtualizationHarness harness)
        {
            for (var i = 1; i <= WheelSteps; ++i)
                harness.ScrollTo(i * WheelStep);

            for (var i = WheelSteps - 1; i >= 0; --i)
                harness.ScrollTo(i * WheelStep);
        }

        /// <summary>Scrollbar-drag style jumps across the whole collection: every one is disjunct.</summary>
        public static void JumpToOffsets(VirtualizationHarness harness, IReadOnlyList<double> offsets)
        {
            for (var i = 0; i < offsets.Count; ++i)
                harness.ScrollTo(offsets[i]);
        }

        /// <summary>
        /// Jump targets spread over the collection's <em>true</em> height, so the same offsets are
        /// used whatever the panel currently estimates the extent to be.
        /// </summary>
        public static double[] CreateJumpOffsets(IReadOnlyList<IBenchItem> items, double viewportHeight)
        {
            var scrollable = Math.Max(0, VirtualizationHarness.TotalHeight(items) - viewportHeight);
            var offsets = new double[JumpCount];
            var random = new Random(20993);

            for (var i = 0; i < offsets.Length; ++i)
                offsets[i] = Math.Round(random.NextDouble() * scrollable);

            return offsets;
        }

        /// <summary>
        /// Walk the whole collection a viewport at a time, so every item is measured once and the
        /// per-item size record ends up holding one entry per item.
        /// </summary>
        public static void TraverseEntireCollection(VirtualizationHarness harness)
        {
            var end = Math.Max(0, VirtualizationHarness.TotalHeight(harness.Items) - harness.ViewportHeight);
            var step = harness.ViewportHeight;

            for (var offset = step; offset < end; offset += step)
                harness.ScrollTo(offset);

            harness.ScrollTo(end);
        }
    }
}
