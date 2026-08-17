using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

using XamlDataTemplate = Avalonia.Markup.Xaml.Templates.DataTemplate;

#nullable enable

namespace Avalonia.Controls.UnitTests
{
    /// <summary>
    /// Tests for opt-in container-level virtualization: recycle-key selection in
    /// <c>ItemsControl.NeedsContainer&lt;T&gt;</c>, the <c>MaxPoolSizePerKey</c> gating in
    /// <c>VirtualizingStackPanel.PushToRecyclePool</c>, the skip-clear in
    /// <c>ClearContainerForItemOverride</c>, and which templates the <c>ItemsControl</c> resolves
    /// onto a container at all.
    /// </summary>
    public class ContainerVirtualizationTests : ScopedTestBase
    {
        // ===== (a) A plain XAML DataTemplate must not cap the container pool =====

        [Fact]
        public void Plain_DataTemplate_Does_Not_Cap_Recycle_Pool()
        {
            using var app = App();

            // EnableVirtualization left at its default false: this is the stock, most common case.
            var template = CanvasTemplate(enableVirtualization: false);
            var items = CreateItems<TypeA_Item>(200);

            var (panel, _, _, _) = CreateTarget(items, template);

            var recycled = RecycleByShrinkingItems(items, panel, keep: 10);

            Assert.True(recycled > template.MaxPoolSizePerKey,
                $"Test is not exercising the cap: only {recycled} containers were recycled, " +
                $"which is not more than MaxPoolSizePerKey ({template.MaxPoolSizePerKey}).");

            // Every recycled container must be in the pool - the pool is uncapped for
            // DefaultRecycleKey, exactly as in stock Avalonia.
            Assert.Equal(recycled, PooledCount(panel));

            // And they all share the single stock pool.
            Assert.Same(TestItemsControl.ExposedDefaultRecycleKey, panel.RecyclePoolForTesting!.Keys.Single());
        }

        // ===== (b) MaxPoolSizePerKey is still honoured once the template opts in =====

        [Fact]
        public void MaxPoolSizePerKey_Is_Respected_For_DataTemplate_With_EnableVirtualization()
        {
            using var app = App();

            var template = CanvasTemplate(enableVirtualization: true, dataType: typeof(TypeA_Item));
            template.MaxPoolSizePerKey = 2;
            var items = CreateItems<TypeA_Item>(200);

            var (panel, _, _, _) = CreateTarget(items, template);

            var recycled = RecycleByShrinkingItems(items, panel, keep: 10);
            Assert.True(recycled > 2, $"Test is not exercising the cap: only {recycled} recycled.");

            // The key came from the template, so the cap applies.
            Assert.Equal(typeof(TypeA_Item), panel.RecyclePoolForTesting!.Keys.Single());
            Assert.Equal(2, PooledCount(panel));
        }

        /// <summary>
        /// <c>MinPoolSizePerKey</c> is the warmup-depth knob, and the markup it is meant to be
        /// configured from is a XAML <c>DataTemplate</c> - so it has to be settable there and the
        /// value has to reach <c>DiscoverTemplateKeys</c>, which is the only thing that reads it.
        /// </summary>
        [Fact]
        public void MinPoolSizePerKey_Set_On_A_DataTemplate_Reaches_Warmup()
        {
            using var app = App();

            var template = CanvasTemplate(enableVirtualization: true, dataType: typeof(TypeA_Item));
            template.MinPoolSizePerKey = 7;
            var items = CreateItems<TypeA_Item>(200);

            var (panel, _, _, _) = CreateTarget(items, template);

            var keys = panel.DiscoverTemplateKeys();

            Assert.True(keys.TryGetValue(typeof(TypeA_Item), out var depth),
                "The template's key was never encountered, so warmup depth was not resolved at all.");

            // 3 is DefaultWarmupPoolSizePerKey - i.e. what this asserts is that the template was
            // asked, not that some default happened to match.
            Assert.Equal(7, depth);
        }

        // ===== (c) IsEnabled = false is a kill switch back to stock behaviour =====

        [Fact]
        public void IsEnabled_False_Forces_Default_Recycle_Key_And_Clears_Content()
        {
            using var app = App();
            var original = ContainerVirtualization.IsEnabled;

            try
            {
                ContainerVirtualization.IsEnabled = false;

                // A template that opts in and keys per item type - with the kill switch off it must
                // be ignored entirely.
                var template = new FuncVirtualizingDataTemplate<object>((_, _) =>
                    new Canvas { Width = 100, Height = 10 });

                var items = new ObservableCollection<object>(
                    Enumerable.Range(0, 200).Select<int, object>(i => i % 2 == 0
                        ? new TypeA_Item { Name = $"A{i}" }
                        : new TypeB_Item { Name = $"B{i}" }));

                var (panel, _, itemsControl, _) = CreateTarget(items, template);

                // Both item types resolve to the single stock key...
                var generator = itemsControl.ItemContainerGenerator;
                Assert.True(generator.NeedsContainer(items[0], 0, out var keyA));
                Assert.True(generator.NeedsContainer(items[1], 1, out var keyB));
                Assert.Same(TestItemsControl.ExposedDefaultRecycleKey, keyA);
                Assert.Same(TestItemsControl.ExposedDefaultRecycleKey, keyB);

                var recycled = RecycleByShrinkingItems(items, panel, keep: 10);
                Assert.True(recycled > 0, "No container was recycled.");

                // ...so all containers land in one shared pool.
                Assert.Same(TestItemsControl.ExposedDefaultRecycleKey, panel.RecyclePoolForTesting!.Keys.Single());
                Assert.NotEmpty(PooledContainers(panel));

                // ...and Content/ContentTemplate are cleared on recycle, as in stock.
                Assert.All(PooledContainers(panel).Cast<ContentPresenter>(), pooled =>
                {
                    Assert.False(pooled.IsSet(ContentPresenter.ContentProperty));
                    Assert.False(pooled.IsSet(ContentPresenter.ContentTemplateProperty));
                    Assert.Null(pooled.Content);
                });
            }
            finally
            {
                ContainerVirtualization.IsEnabled = original;
            }
        }

        // ===== (d) A typed DataTemplate that did not opt in gets its content cleared =====

        [Fact]
        public void Typed_DataTemplate_Without_Opt_In_Clears_Content_On_Recycle()
        {
            using var app = App();

            // <DataTemplate DataType="local:TypeA_Item"> with no EnableVirtualization: the
            // DataType alone must not buy the skip-clear.
            var template = CanvasTemplate(enableVirtualization: false, dataType: typeof(TypeA_Item));
            var items = CreateItems<TypeA_Item>(200);

            var (panel, _, _, _) = CreateTarget(items, template);

            var recycled = RecycleByShrinkingItems(items, panel, keep: 10);
            Assert.True(recycled > 0, "No container was recycled.");
            Assert.NotEmpty(PooledContainers(panel));

            Assert.All(PooledContainers(panel).Cast<ContentPresenter>(), pooled =>
            {
                Assert.False(pooled.IsSet(ContentPresenter.ContentProperty));
                Assert.False(pooled.IsSet(ContentPresenter.ContentTemplateProperty));
                Assert.Null(pooled.Content);
            });
        }

        // ===== (e) An opted-in template keeps its Child attached across recycling =====

        [Fact]
        public void Opted_In_Template_Keeps_Child_Attached_Across_Recycling()
        {
            using var app = App();

            var template = CanvasTemplate(enableVirtualization: true, dataType: typeof(TypeA_Item));
            var items = CreateItems<TypeA_Item>(200);

            var (panel, scroll, itemsControl, _) = CreateTarget(items, template, new Size(100, 100));

            var container = Assert.IsType<ContentPresenter>(itemsControl.ContainerFromIndex(0));
            var child = container.Child;
            Assert.NotNull(child);

            // Scroll far enough that the container is recycled and then handed back out for a
            // different item.
            scroll.Offset = new Vector(0, 1000);
            Layout(panel);

            var index = itemsControl.IndexFromContainer(container);
            Assert.True(index > 0, "The container was not recycled and reused for a different item.");

            // The actual feature: no visual-tree mutation, the same child instance is still there.
            Assert.Same(child, container.Child);

            // ...and again on the way back.
            scroll.Offset = new Vector(0, 0);
            Layout(panel);

            Assert.True(itemsControl.IndexFromContainer(container) >= 0, "The container was not reused.");
            Assert.Same(child, container.Child);
        }

        // ===== (e2) FuncDataTemplate can opt in too, which is what makes the feature usable
        //            from code rather than only from XAML =====

        /// <summary>
        /// The guard that matters most on this file: <see cref="FuncDataTemplate"/> backs
        /// <c>FuncDataTemplate.Default</c>, every <c>ItemTemplate</c> written in code, and much of
        /// the framework's own templating. If implementing <c>IVirtualizingDataTemplate</c> on it
        /// turned virtualization on by default, every one of those would silently start skipping
        /// content clearing and paying the §9 lifecycle trade.
        /// </summary>
        [Fact]
        public void FuncDataTemplate_Without_A_Key_Selector_Is_Not_Opted_In()
        {
            using var app = App();

            var template = new FuncDataTemplate<TypeA_Item>((_, _) => new Canvas { Width = 100, Height = 10 });
            var items = CreateItems<TypeA_Item>(200);

            var (panel, _, itemsControl, _) = CreateTarget(items, template);

            Assert.Null(template.RecycleKeySelector);
            Assert.Null(((IVirtualizingDataTemplate)template).GetKey(items[0]));

            Assert.True(itemsControl.ItemContainerGenerator.NeedsContainer(items[0], 0, out var key));
            Assert.Same(TestItemsControl.ExposedDefaultRecycleKey, key);

            var recycled = RecycleByShrinkingItems(items, panel, keep: 10);
            Assert.True(recycled > 0, "No container was recycled.");

            // One shared, uncapped pool and cleared content: stock behaviour, unchanged.
            Assert.Same(TestItemsControl.ExposedDefaultRecycleKey, panel.RecyclePoolForTesting!.Keys.Single());
            Assert.Equal(recycled, PooledCount(panel));
            Assert.All(PooledContainers(panel).Cast<ContentPresenter>(), pooled =>
            {
                Assert.False(pooled.IsSet(ContentPresenter.ContentProperty));
                Assert.Null(pooled.Content);
            });
        }

        [Fact]
        public void FuncDataTemplate_With_A_Key_Selector_Keys_The_Pool_And_Keeps_The_Child()
        {
            using var app = App();

            var template = new FuncDataTemplate<TypeA_Item>((_, _) => new Canvas { Width = 100, Height = 10 })
            {
                RecycleKeySelector = d => d?.GetType(),
            };

            var items = CreateItems<TypeA_Item>(200);
            var (panel, scroll, itemsControl, _) = CreateTarget(items, template, new Size(100, 100));

            Assert.True(itemsControl.ItemContainerGenerator.NeedsContainer(items[0], 0, out var key));
            Assert.Equal(typeof(TypeA_Item), key);

            var container = Assert.IsType<ContentPresenter>(itemsControl.ContainerFromIndex(0));
            var child = container.Child;
            Assert.NotNull(child);

            scroll.Offset = new Vector(0, 1000);
            Layout(panel);

            Assert.True(itemsControl.IndexFromContainer(container) > 0,
                "The container was not recycled and reused for a different item.");

            // The feature: the subtree survived recycling rather than being rebuilt.
            Assert.Same(child, container.Child);
        }

        /// <summary>
        /// The reason a key *selector* is the opt-in rather than a bool. A XAML
        /// <c>DataTemplate</c> keys on <c>DataType</c>, so it cannot express "one CLR type, several
        /// subtree shapes" — which is exactly what a template that branches on a property produces,
        /// and what a heterogeneous list is made of.
        /// </summary>
        [Fact]
        public void FuncDataTemplate_Key_Selector_Can_Key_On_Something_Other_Than_The_Type()
        {
            using var app = App();

            var template = new FuncDataTemplate<KindedItem>(
                (item, _) => item?.Kind == "tall"
                    ? new Canvas { Width = 100, Height = 20 }
                    : new Canvas { Width = 100, Height = 10 })
            {
                RecycleKeySelector = d => ((KindedItem)d!).Kind,
            };

            var items = new ObservableCollection<object>(
                Enumerable.Range(0, 200).Select<int, object>(i => new KindedItem(i % 2 == 0 ? "tall" : "short")));

            var (panel, _, itemsControl, _) = CreateTarget(items, template);

            // Same CLR type, two pools — which DataType keying could not have produced.
            Assert.True(itemsControl.ItemContainerGenerator.NeedsContainer(items[0], 0, out var tall));
            Assert.True(itemsControl.ItemContainerGenerator.NeedsContainer(items[1], 1, out var @short));
            Assert.Equal("tall", tall);
            Assert.Equal("short", @short);

            RecycleByShrinkingItems(items, panel, keep: 10);

            Assert.Equal(
                new[] { "short", "tall" },
                panel.RecyclePoolForTesting!.Keys.Select(k => (string)k).OrderBy(k => k).ToArray());
        }

        [Fact]
        public void FuncDataTemplate_Key_Selector_Returning_Null_Falls_Back_To_Stock_For_That_Item()
        {
            using var app = App();

            // Opted in for "tall" only: the other kind must behave exactly as an un-opted-in item.
            var template = new FuncDataTemplate<KindedItem>((_, _) => new Canvas { Width = 100, Height = 10 })
            {
                RecycleKeySelector = d => ((KindedItem)d!).Kind == "tall" ? "tall" : null,
            };

            var items = new ObservableCollection<object>(
                Enumerable.Range(0, 200).Select<int, object>(i => new KindedItem(i % 2 == 0 ? "tall" : "short")));

            var (_, _, itemsControl, _) = CreateTarget(items, template);

            Assert.True(itemsControl.ItemContainerGenerator.NeedsContainer(items[0], 0, out var tall));
            Assert.True(itemsControl.ItemContainerGenerator.NeedsContainer(items[1], 1, out var @short));
            Assert.Equal("tall", tall);
            Assert.Same(TestItemsControl.ExposedDefaultRecycleKey, @short);
        }

        [Fact]
        public void FuncDataTemplate_MaxPoolSizePerKey_Is_Respected_Once_Opted_In()
        {
            using var app = App();

            var template = new FuncDataTemplate<TypeA_Item>((_, _) => new Canvas { Width = 100, Height = 10 })
            {
                RecycleKeySelector = d => d?.GetType(),
                MaxPoolSizePerKey = 2,
            };

            var items = CreateItems<TypeA_Item>(200);
            var (panel, _, _, _) = CreateTarget(items, template);

            var recycled = RecycleByShrinkingItems(items, panel, keep: 10);
            Assert.True(recycled > 2, $"Test is not exercising the cap: only {recycled} recycled.");

            Assert.Equal(typeof(TypeA_Item), panel.RecyclePoolForTesting!.Keys.Single());
            Assert.Equal(2, PooledCount(panel));
        }

        // ===== (f) `item is T` must win over the virtualization branch =====

        [Fact]
        public void Item_That_Is_Its_Own_Container_Is_Not_Wrapped_When_Virtualization_Enabled()
        {
            using var app = App();

            // GetKey returns a non-null key for every item, including Controls - so if the
            // `item is T` check were not first, these items would be wrapped.
            var template = new FuncVirtualizingDataTemplate<object>((_, _) =>
                new Canvas { Width = 100, Height = 10 });

            var items = new ObservableCollection<object>(
                Enumerable.Range(0, 20).Select(_ => (object)new Canvas { Width = 100, Height = 10 }));

            var (panel, _, itemsControl, _) = CreateTarget(items, template);

            var generator = itemsControl.ItemContainerGenerator;
            Assert.False(generator.NeedsContainer(items[0], 0, out var recycleKey));
            Assert.Null(recycleKey);

            // The item itself is the container - not a ContentPresenter wrapping it.
            Assert.Same(items[0], itemsControl.ContainerFromIndex(0));
        }

        // ===== (g) A recycled container reused for a different item picks up the new item =====

        [Fact]
        public void Recycled_Container_Reused_For_Different_Item_Gets_New_Content()
        {
            using var app = App();

            var template = CanvasTemplate(enableVirtualization: true, dataType: typeof(TypeA_Item));
            var items = CreateItems<TypeA_Item>(200);

            var (panel, scroll, itemsControl, _) = CreateTarget(items, template, new Size(100, 100));

            var container = Assert.IsType<ContentPresenter>(itemsControl.ContainerFromIndex(0));
            Assert.Same(items[0], container.Content);

            // Recycle it and bring it back for a different item. Content is never cleared on the
            // skip-clear path, so preparation has to overwrite the stale value.
            scroll.Offset = new Vector(0, 1000);
            Layout(panel);

            var index = itemsControl.IndexFromContainer(container);
            Assert.True(index > 0, "The container was not reused for a different item.");
            Assert.Same(items[index], container.Content);
            Assert.NotSame(items[0], container.Content);
        }

        // ===== (h) A DataTemplates-collection template is resolved by the presenter =====

        [Fact]
        public void DataTemplates_Collection_Template_Is_Never_Copied_Onto_The_Container()
        {
            using var app = App();

            var items = CreateItems<TypeA_Item>(200);
            var canvasTemplate = new FuncDataTemplate<TypeA_Item>((_, _) => new Canvas { Width = 100, Height = 10 });

            // No ItemTemplate: the template can only be found by walking up to the DataTemplates
            // collection, which is the ContentPresenter's job - the ItemsControl hands out
            // ContentTemplate only from ItemTemplate / DisplayMemberBinding, as in stock Avalonia.
            var (panel, scroll, itemsControl, _) = CreateTarget(
                items,
                itemTemplate: null,
                clientSize: new Size(100, 100),
                configure: ic => ic.DataTemplates.Add(canvasTemplate));

            // Re-seating the same template and re-preparing containers that are already in the tree
            // is what used to make the resolution succeed and stamp ContentTemplate on the
            // container - the one state in which the ItemsControl and the presenter could disagree
            // about which template an item uses.
            itemsControl.DataTemplates.Clear();
            itemsControl.DataTemplates.Add(canvasTemplate);
            scroll.Offset = new Vector(0, 500);
            Layout(panel);

            var realized = panel.GetRealizedContainers()!.Cast<ContentPresenter>().ToList();
            Assert.NotEmpty(realized);
            Assert.All(realized, c =>
            {
                Assert.False(c.IsSet(ContentPresenter.ContentTemplateProperty));
                // The item still displays through the collection's template, resolved by the
                // presenter itself.
                Assert.IsType<Canvas>(c.Child);
            });
        }

        [Fact]
        public void Template_Swapped_In_DataTemplates_At_Runtime_Is_Picked_Up()
        {
            using var app = App();

            var items = CreateItems<TypeA_Item>(200);

            var (panel, scroll, itemsControl, _) = CreateTarget(
                items,
                itemTemplate: null,
                clientSize: new Size(100, 100),
                configure: ic => ic.DataTemplates.Add(
                    new FuncDataTemplate<TypeA_Item>((_, _) => new Canvas { Width = 100, Height = 10 })));

            Assert.All(
                panel.GetRealizedContainers()!.Cast<ContentPresenter>(),
                c => Assert.IsType<Canvas>(c.Child));

            // Swap the template at runtime. Nothing memoizes the resolution per item type, so
            // containers realized from here on must show the new template.
            itemsControl.DataTemplates.Clear();
            itemsControl.DataTemplates.Add(
                new FuncDataTemplate<TypeA_Item>((_, _) => new Border { Width = 100, Height = 10 }));

            scroll.Offset = new Vector(0, 1000);
            Layout(panel);

            var prepared = panel.GetRealizedContainers()!.Cast<ContentPresenter>().ToList();
            Assert.NotEmpty(prepared);
            Assert.All(prepared, c => Assert.IsType<Border>(c.Child));
        }

        [Fact]
        public void ItemTemplate_Is_Still_Applied_To_The_Container()
        {
            using var app = App();

            // The counterpart to the two tests above: the template the ItemsControl *does* resolve
            // still lands on the container, so recycling has a ContentTemplate to key its
            // skip-clear decision on.
            var template = CanvasTemplate(enableVirtualization: true, dataType: typeof(TypeA_Item));
            var items = CreateItems<TypeA_Item>(200);

            var (panel, _, _, _) = CreateTarget(items, template, new Size(100, 100));

            var realized = panel.GetRealizedContainers()!.Cast<ContentPresenter>().ToList();
            Assert.NotEmpty(realized);
            Assert.All(realized, c => Assert.Same(template, c.ContentTemplate));
        }

        [Fact]
        public void DataTemplates_Collection_Does_Not_Cause_Repeated_Measures()
        {
            using var app = App();

            // The deleted per-type template cache was justified as "critical for avoiding layout
            // cycles when using DataTemplates collections". Resolving the template in the presenter
            // on every realization must not turn into a measure feedback loop: a settled panel
            // stays settled, and a scroll costs on the order of one measure per realized child.
            var measures = new MeasureCounter();
            var items = CreateItems<TypeA_Item>(200);

            var (panel, scroll, _, _) = CreateTarget(
                items,
                itemTemplate: null,
                clientSize: new Size(100, 100),
                configure: ic => ic.DataTemplates.Add(
                    new FuncDataTemplate<TypeA_Item>((_, _) => new CountingCanvas(measures))));

            var realizedCount = panel.GetRealizedContainers()!.Count();
            Assert.True(realizedCount > 0, "Nothing was realized.");

            // Settled: another pass at the same offset measures nothing.
            var afterInitial = measures.Count;
            Layout(panel);
            Assert.Equal(afterInitial, measures.Count);

            measures.Count = 0;
            scroll.Offset = new Vector(0, 500);
            Layout(panel);

            Assert.True(measures.Count <= realizedCount * 3,
                $"One scroll realizing ~{realizedCount} children cost {measures.Count} child measures; " +
                $"the template resolution is feeding back into layout.");

            // ...and it settles again.
            var afterScroll = measures.Count;
            Layout(panel);
            Assert.Equal(afterScroll, measures.Count);
        }

        // ===== (i) Keying and pool capping agree for a DisplayMemberBinding template =====

        [Fact]
        public void DisplayMemberBinding_Template_Keys_And_Caps_Consistently()
        {
            using var app = App();

            // Non-empty names: the synthesised TextBlock is what gives the container its width, and
            // a zero-width viewport stops the panel realizing.
            var items = new ObservableCollection<object>(
                Enumerable.Range(0, 200).Select(i => (object)new TypeA_Item { Name = $"Item {i}" }));

            // DisplayMemberBinding synthesises the effective item template. It is a plain
            // FuncDataTemplate, so it never opts in - and because keying and capping both resolve
            // through GetEffectiveItemTemplate() they must agree on that: stock key, no cap.
            var (panel, _, itemsControl, _) = CreateTarget(
                items,
                itemTemplate: null,
                configure: ic => ic.DisplayMemberBinding = new Binding(nameof(TypeA_Item.Name)),
                // The synthesised template is a TextBlock, which measures to nothing in a test
                // environment with no text shaping - pin the container height so a predictable
                // number of items is realized.
                styles: new[]
                {
                    new Style(x => x.OfType<ContentPresenter>())
                    {
                        Setters = { new Setter(Layoutable.HeightProperty, 10.0) },
                    },
                });

            Assert.True(itemsControl.ItemContainerGenerator.NeedsContainer(items[0], 0, out var key));
            Assert.Same(TestItemsControl.ExposedDefaultRecycleKey, key);

            var recycled = RecycleByShrinkingItems(items, panel, keep: 10);
            Assert.True(recycled > 5, $"Test is not exercising the cap: only {recycled} recycled.");

            // Uncapped, because the key is not one an IVirtualizingDataTemplate handed out.
            Assert.Same(TestItemsControl.ExposedDefaultRecycleKey, panel.RecyclePoolForTesting!.Keys.Single());
            Assert.Equal(recycled, PooledCount(panel));
        }

        // ===== (j) Nested virtualization: the retained Child is itself a virtualizing list =====

        [Fact]
        public void Nested_Virtualized_List_Retargets_When_Outer_Container_Is_Recycled()
        {
            using var app = App();

            var (panel, scroll, itemsControl, items) = CreateNestedTarget();

            var container = Assert.IsType<ContentPresenter>(itemsControl.ContainerFromIndex(0));
            var inner = Assert.IsType<ListBox>(container.Child);

            var first = (OuterItem)items[0];
            Assert.Same(first.Children, inner.ItemsSource);

            var innerItems = RealizedInnerItems(inner);
            Assert.NotEmpty(innerItems);
            Assert.True(innerItems.Count < first.Children.Count,
                $"The inner list is not virtualizing: {innerItems.Count} of {first.Children.Count} realized.");
            Assert.All(innerItems, x => Assert.Contains(x, first.Children));

            // Recycle the outer container and have it handed back out for a different outer item.
            scroll.Offset = new Vector(0, 1000);
            Layout(panel);

            var index = itemsControl.IndexFromContainer(container);
            Assert.True(index > 0, "The outer container was not reused for a different item.");

            // The whole point of the feature: the inner list survives the recycle as the same
            // instance, and is simply re-pointed at the new outer item's children.
            Assert.Same(inner, container.Child);

            var reused = (OuterItem)items[index];
            Assert.Same(reused.Children, inner.ItemsSource);

            var reusedInnerItems = RealizedInnerItems(inner);
            Assert.NotEmpty(reusedInnerItems);

            // No inner container is left carrying the previous outer item's data...
            Assert.All(reusedInnerItems, x => Assert.Contains(x, reused.Children));
            Assert.Empty(reusedInnerItems.Intersect(first.Children));

            // ...and none is realized twice.
            Assert.Equal(reusedInnerItems.Count, reusedInnerItems.Distinct().Count());
            var containers = inner.GetRealizedContainers().ToList();
            Assert.Equal(containers.Count, containers.Distinct().Count());
        }

        [Fact]
        public void Nested_Inner_Lists_Are_Never_Shared_Between_Outer_Containers()
        {
            using var app = App();

            var (panel, scroll, itemsControl, items) = CreateNestedTarget();

            scroll.Offset = new Vector(0, 1000);
            Layout(panel);

            var outerContainers = panel.GetRealizedContainers()!.Cast<ContentPresenter>().ToList();
            Assert.True(outerContainers.Count > 1, "Not enough outer containers realized to compare.");

            var innerLists = new List<ListBox>();
            var innerContainers = new List<Control>();

            foreach (var outerContainer in outerContainers)
            {
                var index = itemsControl.IndexFromContainer(outerContainer);
                Assert.True(index >= 0, "A realized outer container has no index.");

                var inner = Assert.IsType<ListBox>(outerContainer.Child);
                innerLists.Add(inner);
                innerContainers.AddRange(inner.GetRealizedContainers());

                // Each inner list shows only the children of the outer item its container
                // currently holds - no cross-contamination from the item it held before.
                var owner = (OuterItem)items[index];
                Assert.Same(owner.Children, inner.ItemsSource);
                Assert.All(RealizedInnerItems(inner), x => Assert.Contains(x, owner.Children));
            }

            // Two outer containers never end up sharing one retained child...
            Assert.Equal(innerLists.Count, innerLists.Distinct().Count());

            // ...and the per-key inner pools never hand the same inner container to two lists.
            Assert.Equal(innerContainers.Count, innerContainers.Distinct().Count());
        }

        [Fact]
        public void Nested_Virtualization_Survives_An_Outer_Scroll_Roundtrip()
        {
            using var app = App();

            var (panel, scroll, itemsControl, items) = CreateNestedTarget();

            var container = Assert.IsType<ContentPresenter>(itemsControl.ContainerFromIndex(0));
            var inner = Assert.IsType<ListBox>(container.Child);
            var first = (OuterItem)items[0];

            scroll.Offset = new Vector(0, 1000);
            Layout(panel);
            scroll.Offset = new Vector(0, 0);
            Layout(panel);

            var backAtTop = Assert.IsType<ContentPresenter>(itemsControl.ContainerFromIndex(0));
            var innerBackAtTop = Assert.IsType<ListBox>(backAtTop.Child);

            Assert.Same(first, backAtTop.Content);
            Assert.Same(first.Children, innerBackAtTop.ItemsSource);

            var innerItems = RealizedInnerItems(innerBackAtTop);
            Assert.NotEmpty(innerItems);
            Assert.All(innerItems, x => Assert.Contains(x, first.Children));
            Assert.Equal(innerItems.Count, innerItems.Distinct().Count());

            // The container that was scrolled away and back is still driving its own inner list.
            Assert.Same(inner, container.Child);
        }

        // ===== helpers =====

        private static IDisposable App() => UnitTestApplication.Start(TestServices.RealFocus);

        private static void Layout(Control target) => target.GetLayoutManager()?.ExecuteLayoutPass();

        private static ObservableCollection<object> CreateItems<T>(int count) where T : new() =>
            new(Enumerable.Range(0, count).Select(_ => (object)new T()));

        /// <summary>
        /// A real XAML <see cref="XamlDataTemplate"/>. Its <c>Content</c> is deferred content, which
        /// XAML compilation normally produces; a <c>Func&lt;IServiceProvider?, object?&gt;</c> is the
        /// shape <c>TemplateContent.Load</c> accepts, so the template can be built in code without a
        /// XAML compile step.
        /// </summary>
        private static XamlDataTemplate CanvasTemplate(bool enableVirtualization, Type? dataType = null) =>
            new()
            {
                DataType = dataType,
                EnableVirtualization = enableVirtualization,
                Content = (Func<IServiceProvider?, object?>)(_ =>
                    new TemplateResult<Control>(new Canvas { Width = 100, Height = 10 }, new NameScope())),
            };

        /// <summary>
        /// Shrinks the collection so that fewer containers are needed than are currently realized.
        /// The surplus is recycled without being immediately handed back out, which is what makes the
        /// pool observable at the end of a layout pass - during a scroll the panel drains the pool in
        /// the same pass it filled it. Returns how many containers were recycled.
        /// </summary>
        private static int RecycleByShrinkingItems(
            ObservableCollection<object> items,
            VirtualizingStackPanel panel,
            int keep)
        {
            var before = panel.GetRealizedContainers()!.Count();
            Assert.True(before > keep, $"Only {before} containers were realized, expected more than {keep}.");

            for (var i = items.Count - 1; i >= keep; i--)
                items.RemoveAt(i);

            Layout(panel);

            return before - panel.GetRealizedContainers()!.Count();
        }

        private static int PooledCount(VirtualizingStackPanel panel) =>
            panel.RecyclePoolForTesting?.Values.Sum(x => x.Count) ?? 0;

        private static IEnumerable<Control> PooledContainers(VirtualizingStackPanel panel) =>
            panel.RecyclePoolForTesting?.Values.SelectMany(x => x) ?? Enumerable.Empty<Control>();

        private static (VirtualizingStackPanel panel, ScrollViewer scroll, ItemsControl itemsControl, TestRoot root)
            CreateTarget(
                IEnumerable<object> items,
                IDataTemplate? itemTemplate,
                Size? clientSize = null,
                Action<ItemsControl>? configure = null,
                IEnumerable<Style>? styles = null)
        {
            var panel = new VirtualizingStackPanel { Orientation = Orientation.Vertical, CacheLength = 0 };

            var presenter = new ItemsPresenter
            {
                [~ItemsPresenter.ItemsPanelProperty] = new TemplateBinding(ItemsPresenter.ItemsPanelProperty),
            };

            var scroll = new ScrollViewer
            {
                Name = "PART_ScrollViewer",
                Content = presenter,
                Template = ScrollViewerTemplate(),
            };

            var itemsControl = new TestItemsControl
            {
                ItemsSource = items,
                Template = new FuncControlTemplate<TestItemsControl>((_, ns) => scroll.RegisterInNameScope(ns)),
                ItemsPanel = new FuncTemplate<Panel?>(() => panel),
                ItemTemplate = itemTemplate,
            };

            configure?.Invoke(itemsControl);

            var root = new TestRoot(true, itemsControl) { ClientSize = clientSize ?? new Size(100, 400) };

            if (styles is not null)
                root.Styles.AddRange(styles);

            root.LayoutManager.ExecuteInitialLayoutPass();

            return (panel, scroll, itemsControl, root);
        }

        private static IControlTemplate ScrollViewerTemplate() =>
            new FuncControlTemplate<ScrollViewer>((_, ns) =>
                new ScrollContentPresenter { Name = "PART_ScrollContentPresenter" }.RegisterInNameScope(ns));

        /// <summary>
        /// Exists only to make <c>ItemsControl.DefaultRecycleKey</c> - which is
        /// <c>protected static</c> - assertable.
        /// </summary>
        private class TestItemsControl : ItemsControl
        {
            public static object ExposedDefaultRecycleKey => DefaultRecycleKey;
        }

        private class FuncVirtualizingDataTemplate<T> : FuncDataTemplate<T>, IVirtualizingDataTemplate
        {
            public FuncVirtualizingDataTemplate(Func<T, INameScope, Control?> build)
                : base(build, supportsRecycling: true) { }

            public object? GetKey(object? data) => data?.GetType();
            public int MaxPoolSizePerKey { get; set; } = 5;
            public int MinPoolSizePerKey { get; set; } = 2;
        }

        // ===== nested-virtualization helpers =====

        private const int InnerItemHeight = 10;
        private const int InnerItemCount = 20;
        private const int OuterItemHeight = 20;   // = the inner ListBox's fixed height

        /// <summary>
        /// An outer virtualized <see cref="ItemsControl"/> whose opted-in item template builds an
        /// inner virtualized <see cref="ListBox"/> over the outer item's own children. Both levels
        /// opt into container virtualization, so both keep their <c>Child</c> attached on recycle.
        /// </summary>
        private static (VirtualizingStackPanel panel, ScrollViewer scroll, ItemsControl itemsControl,
            ObservableCollection<object> items) CreateNestedTarget()
        {
            var innerTemplate = new FuncVirtualizingDataTemplate<InnerItem>(
                (_, _) => new Canvas { Width = 50, Height = InnerItemHeight });

            var outerTemplate = new FuncVirtualizingDataTemplate<OuterItem>(
                (_, _) => CreateInnerListBox(innerTemplate));

            var items = new ObservableCollection<object>(
                Enumerable.Range(0, 200).Select(i => (object)new OuterItem(i, InnerItemCount)));

            var (panel, scroll, itemsControl, _) = CreateTarget(
                items,
                outerTemplate,
                new Size(100, 100),
                styles: new[]
                {
                    // The inner ListBoxItems need a template both to give the inner ContentPresenter
                    // a host (which is what the skip-clear on a ContentControl container keys on)
                    // and to have a height at all in a test environment.
                    new Style(x => x.OfType<ListBoxItem>())
                    {
                        Setters = { new Setter(ListBoxItem.TemplateProperty, ListBoxItemTemplate()) },
                    },
                });

            return (panel, scroll, itemsControl, items);
        }

        private static ListBox CreateInnerListBox(IDataTemplate innerTemplate) =>
            new()
            {
                Height = OuterItemHeight,
                ItemTemplate = innerTemplate,
                ItemsPanel = new FuncTemplate<Panel?>(() =>
                    new VirtualizingStackPanel { Orientation = Orientation.Vertical, CacheLength = 0 }),
                Template = new FuncControlTemplate<ListBox>((_, ns) => new ScrollViewer
                {
                    Name = "PART_ScrollViewer",
                    Content = new ItemsPresenter
                    {
                        [~ItemsPresenter.ItemsPanelProperty] =
                            new TemplateBinding(ItemsPresenter.ItemsPanelProperty),
                    },
                    Template = ScrollViewerTemplate(),
                }.RegisterInNameScope(ns)),
                [!ItemsControl.ItemsSourceProperty] = new Binding(nameof(OuterItem.Children)),
            };

        private static IControlTemplate ListBoxItemTemplate() =>
            new FuncControlTemplate<ListBoxItem>((_, ns) => new ContentPresenter
            {
                Name = "PART_ContentPresenter",
                Height = InnerItemHeight,
                [~ContentPresenter.ContentProperty] = new TemplateBinding(ContentControl.ContentProperty),
                [~ContentPresenter.ContentTemplateProperty] =
                    new TemplateBinding(ContentControl.ContentTemplateProperty),
            }.RegisterInNameScope(ns));

        private static List<object?> RealizedInnerItems(ListBox inner) =>
            inner.GetRealizedContainers().Cast<ListBoxItem>().Select(x => x.Content).ToList();

        private class OuterItem : NotifyingBase
        {
            public OuterItem(int index, int childCount)
            {
                Name = $"Outer {index}";
                Children = new ObservableCollection<InnerItem>(
                    Enumerable.Range(0, childCount).Select(i => new InnerItem { Name = $"Outer {index} / Inner {i}" }));
            }

            public string Name { get; }

            public ObservableCollection<InnerItem> Children { get; }
        }

        private class InnerItem : NotifyingBase
        {
            public string Name { get; set; } = string.Empty;
        }

        private class TypeA_Item : NotifyingBase
        {
            public string Name { get; set; } = string.Empty;
        }

        /// <summary>
        /// One CLR type whose template builds different subtrees depending on <see cref="Kind"/> —
        /// the case that needs a key selector rather than type-based keying.
        /// </summary>
        private class KindedItem : NotifyingBase
        {
            public KindedItem(string kind) => Kind = kind;

            public string Kind { get; }
        }

        private class MeasureCounter
        {
            public int Count { get; set; }
        }

        /// <summary>
        /// Template content that counts how often it is measured, so a measure feedback loop is
        /// visible as a count rather than as a hang.
        /// </summary>
        private class CountingCanvas : Canvas
        {
            private readonly MeasureCounter _counter;

            public CountingCanvas(MeasureCounter counter)
            {
                _counter = counter;
                Width = 100;
                Height = 10;
            }

            protected override Size MeasureOverride(Size availableSize)
            {
                _counter.Count++;
                return base.MeasureOverride(availableSize);
            }
        }

        private class TypeB_Item : NotifyingBase
        {
            public string Name { get; set; } = string.Empty;
        }
    }
}
