using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;

#nullable enable

namespace Avalonia.Benchmarks.Controls
{
    /// <summary>
    /// The four row kinds a heterogeneous form/feed list is made of. Four kinds is the point, not
    /// decoration: stock pools every container under a single <c>DefaultRecycleKey</c>, so a
    /// recycled container is routinely handed an item of a *different* kind and has to throw its
    /// subtree away and build the other one. Type-aware recycle keys are what stop that.
    /// </summary>
    internal enum RowKind
    {
        Header,
        TextRow,
        PhotoRow,
        FormRow,
    }

    /// <summary>
    /// A row with enough bound properties that re-evaluating them is real work, as in a form
    /// template. <see cref="IBenchItem.Height"/> is bound by the template onto the root border, so
    /// geometry stays predictable while the subtree above it stays expensive.
    /// </summary>
    internal sealed class ComplexItem : IBenchItem
    {
        public ComplexItem(int index, RowKind kind, double height)
        {
            Kind = kind;
            Height = height;
            Title = "Row " + index;
            Subtitle = "Secondary line for row " + index;
            Caption = "Field " + index;
            Value = "Value " + index;
            Hint = "hint " + index;
            Badge = (index % 7).ToString();
            Detail = "Detail text for row " + index + " spanning a little further";
        }

        public RowKind Kind { get; }

        public double Height { get; }

        public string Title { get; }

        public string Subtitle { get; }

        public string Caption { get; }

        public string Value { get; }

        public string Hint { get; }

        public string Badge { get; }

        public string Detail { get; }
    }

    // Public only because it is used as a BenchmarkDotNet [Params] type.
    public enum TemplateMode
    {
        /// <summary>
        /// A plain template that rebuilds its subtree whenever the presenter asks for content —
        /// stock behaviour, and what a template that does not opt in still gets on this branch.
        /// </summary>
        Plain,

        /// <summary>
        /// The fork's opt-in: <c>IVirtualizingDataTemplate</c> with a per-row-kind key, so
        /// containers pool by kind and a reused one keeps its child attached.
        /// </summary>
        Virtualized,
    }

    /// <summary>
    /// Builds the row subtrees and the plain (non-opted-in) template over them.
    /// </summary>
    /// <remarks>
    /// Stock API only, so this file can be copied into a merge-base worktree. The opt-in template
    /// lives in <c>ComplexVirtualizingTemplate.cs</c>, which is fork-only; leave it (and
    /// <c>ComplexScrollBenchmark.cs</c>) behind when copying. Nothing else needs editing — the
    /// opt-in template registers itself through <see cref="VirtualizingTemplateFactory"/>, so with
    /// that file absent <see cref="AvailableModes"/> simply reports the one arm that exists.
    /// </remarks>
    internal static class ComplexItems
    {
        /// <summary>
        /// Set by the fork-only opt-in template's module initializer. Null at the merge-base, where
        /// <c>IVirtualizingDataTemplate</c> does not exist.
        /// </summary>
        /// <remarks>
        /// A property rather than a field: with the fork-only file absent nothing assigns it, and a
        /// field would then be CS0649 — which this repo treats as an error.
        /// </remarks>
        public static Func<VirtualizationCounters, IDataTemplate>? VirtualizingTemplateFactory { get; set; }

        public static IReadOnlyList<TemplateMode> AvailableModes =>
            VirtualizingTemplateFactory is null
                ? new[] { TemplateMode.Plain }
                : new[] { TemplateMode.Plain, TemplateMode.Virtualized };

        public static IDataTemplate TemplateFor(TemplateMode mode, VirtualizationCounters counters) =>
            mode == TemplateMode.Virtualized
                ? VirtualizingTemplateFactory!(counters)
                : CreatePlainTemplate(counters);

        public static VirtualizationHarness CreateHarness(IReadOnlyList<IBenchItem> items, TemplateMode mode)
        {
            var counters = new VirtualizationCounters();
            return new VirtualizationHarness(items, TemplateFor(mode, counters), counters, viewportWidth: 480);
        }

        /// <summary>Nominal row height per kind — heterogeneous, so the size record has work to do too.</summary>
        private static double HeightFor(RowKind kind) => kind switch
        {
            RowKind.Header => 56,
            RowKind.TextRow => 76,
            RowKind.PhotoRow => 168,
            _ => 116,
        };

        public static List<IBenchItem> CreateItems(int count)
        {
            var items = new List<IBenchItem>(count);
            // Fixed seed so both arms of the comparison see byte-identical data.
            var random = new Random(20259);

            for (var i = 0; i < count; ++i)
            {
                // A header every so often, then a shuffle of the three content kinds — a grouped
                // list whose kinds are not evenly spread, which is also the shape that broke the
                // old head-sampling warmup.
                var kind = i % 12 == 0
                    ? RowKind.Header
                    : (RowKind)(1 + random.Next(0, 3));

                items.Add(new ComplexItem(i, kind, HeightFor(kind) + random.Next(0, 9)));
            }

            return items;
        }

        /// <summary>
        /// The template a normal Avalonia app writes: one template that switches on the item kind,
        /// building a fresh subtree every time it is asked. This is the "container virtualization
        /// does not exist" arm.
        /// </summary>
        public static IDataTemplate CreatePlainTemplate(VirtualizationCounters counters) =>
            new FuncDataTemplate<ComplexItem>((item, _) => Build(item, counters));

        /// <summary>
        /// Builds the subtree for <paramref name="item"/>'s kind. Every text and size comes from a
        /// binding so the tree is correct for whatever item its DataContext later becomes — which
        /// is precisely what makes it reusable across recycling.
        /// </summary>
        public static Control Build(ComplexItem? item, VirtualizationCounters counters)
        {
            counters.ChildBuilds++;

            var kind = item?.Kind ?? RowKind.TextRow;

            var content = kind switch
            {
                RowKind.Header => BuildHeader(counters),
                RowKind.TextRow => BuildTextRow(counters),
                RowKind.PhotoRow => BuildPhotoRow(counters),
                _ => BuildFormRow(counters),
            };

            return New(counters, new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Gainsboro,
                Background = Brushes.White,
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 2),
                [!Layoutable.HeightProperty] = new Binding(nameof(ComplexItem.Height)),
                Child = content,
            });
        }

        private static Control BuildHeader(VirtualizationCounters counters)
        {
            var stack = New(counters, new StackPanel { Orientation = Orientation.Vertical, Spacing = 2 });

            stack.Children.Add(Text(counters, nameof(ComplexItem.Title), 16, FontWeight.Bold));
            stack.Children.Add(Text(counters, nameof(ComplexItem.Subtitle), 11, FontWeight.Normal));
            stack.Children.Add(New(counters, new Rectangle
            {
                Height = 1,
                Fill = Brushes.Silver,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            }));

            return stack;
        }

        private static Control BuildTextRow(VirtualizationCounters counters)
        {
            var outer = New(counters, new StackPanel { Orientation = Orientation.Vertical, Spacing = 3 });

            var line = New(counters, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 });
            line.Children.Add(Badge(counters));
            line.Children.Add(Text(counters, nameof(ComplexItem.Caption), 13, FontWeight.SemiBold));
            line.Children.Add(Text(counters, nameof(ComplexItem.Value), 13, FontWeight.Normal));
            outer.Children.Add(line);

            outer.Children.Add(Text(counters, nameof(ComplexItem.Detail), 11, FontWeight.Normal));
            outer.Children.Add(New(counters, new Rectangle { Height = 1, Fill = Brushes.WhiteSmoke }));

            return outer;
        }

        private static Control BuildPhotoRow(VirtualizationCounters counters)
        {
            var row = New(counters, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 });

            // Stands in for a decoded image: a real one would make this arm slower still, and the
            // point is the subtree's construction cost, not the decoder's.
            row.Children.Add(New(counters, new Border
            {
                Width = 96,
                Height = 96,
                Background = Brushes.LightSteelBlue,
                CornerRadius = new CornerRadius(4),
                Child = New(counters, new Rectangle { Fill = Brushes.SteelBlue, Margin = new Thickness(12) }),
            }));

            var texts = New(counters, new StackPanel { Orientation = Orientation.Vertical, Spacing = 3 });
            texts.Children.Add(Text(counters, nameof(ComplexItem.Title), 14, FontWeight.SemiBold));
            texts.Children.Add(Text(counters, nameof(ComplexItem.Subtitle), 11, FontWeight.Normal));
            texts.Children.Add(Text(counters, nameof(ComplexItem.Detail), 11, FontWeight.Normal));

            var chips = New(counters, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 });
            for (var i = 0; i < 3; ++i)
                chips.Children.Add(Badge(counters));
            texts.Children.Add(chips);

            row.Children.Add(texts);

            return row;
        }

        private static Control BuildFormRow(VirtualizationCounters counters)
        {
            var outer = New(counters, new StackPanel { Orientation = Orientation.Vertical, Spacing = 4 });

            outer.Children.Add(Text(counters, nameof(ComplexItem.Caption), 12, FontWeight.SemiBold));

            outer.Children.Add(New(counters, new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Silver,
                Padding = new Thickness(6, 4),
                Child = Text(counters, nameof(ComplexItem.Value), 13, FontWeight.Normal),
            }));

            var buttons = New(counters, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 });
            for (var i = 0; i < 2; ++i)
            {
                buttons.Children.Add(New(counters, new Border
                {
                    Background = Brushes.WhiteSmoke,
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(8, 3),
                    Child = Text(counters, nameof(ComplexItem.Hint), 11, FontWeight.Normal),
                }));
            }

            outer.Children.Add(buttons);

            return outer;
        }

        private static Border Badge(VirtualizationCounters counters) =>
            New(counters, new Border
            {
                Background = Brushes.Gainsboro,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(5, 1),
                Child = Text(counters, nameof(ComplexItem.Badge), 10, FontWeight.Bold),
            });

        private static TextBlock Text(
            VirtualizationCounters counters,
            string property,
            double fontSize,
            FontWeight weight) =>
            New(counters, new TextBlock
            {
                FontSize = fontSize,
                FontWeight = weight,
                TextTrimming = TextTrimming.CharacterEllipsis,
                [!TextBlock.TextProperty] = new Binding(property),
            });

        private static T New<T>(VirtualizationCounters counters, T visual) where T : Control
        {
            counters.VisualsCreated++;
            return visual;
        }
    }
}
