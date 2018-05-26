// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Presenters
{
    public class ItemsPresenterTests_Virtualization_Simple
    {
        [Fact]
        public void Should_Return_Items_Count_For_Extent_Vertical()
        {
            var target = CreateTarget();

            target.ApplyTemplate();

            Assert.Equal(new Size(0, 20), ((ILogicalScrollable)target).Extent);
        }

        [Fact]
        public void Should_Return_Items_Count_For_Extent_Horizontal()
        {
            var target = CreateTarget(orientation: Orientation.Horizontal);

            target.ApplyTemplate();

            Assert.Equal(new Size(20, 0), ((ILogicalScrollable)target).Extent);
        }

        [Fact]
        public void Should_Have_Number_Of_Visible_Items_As_Viewport_Vertical()
        {
            var target = CreateTarget();

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Size(100, 10), ((ILogicalScrollable)target).Viewport);
        }

        [Fact]
        public void Should_Have_Number_Of_Visible_Items_As_Viewport_Horizontal()
        {
            var target = CreateTarget(orientation: Orientation.Horizontal);

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Size(10, 100), ((ILogicalScrollable)target).Viewport);
        }

        [Fact]
        public void Should_Add_Containers_When_Panel_Is_Not_Full()
        {
            var target = CreateTarget(itemCount: 5);
            var items = (IList<string>)target.Items;

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(5, target.Panel.Children.Count);

            items.Add("New Item");

            Assert.Equal(6, target.Panel.Children.Count);
        }

        [Fact]
        public void Should_Remove_Items_When_Control_Is_Shrank()
        {
            var target = CreateTarget();
            var items = (IList<string>)target.Items;

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(10, target.Panel.Children.Count);

            target.Measure(new Size(100, 80));
            target.Arrange(new Rect(0, 0, 100, 80));

            Assert.Equal(8, target.Panel.Children.Count);
        }

        [Fact]
        public void Should_Add_New_Containers_At_Top_When_Control_Is_Scrolled_To_Bottom_And_Enlarged()
        {
            var target = CreateTarget();
            var items = (IList<string>)target.Items;

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(10, target.Panel.Children.Count);

            ((IScrollable)target).Offset = new Vector(0, 10);
            target.Measure(new Size(120, 120));
            target.Arrange(new Rect(0, 0, 100, 120));

            Assert.Equal(12, target.Panel.Children.Count);

            for (var i = 0; i < target.Panel.Children.Count; ++i)
            {
                Assert.Equal(items[i + 8], target.Panel.Children[i].DataContext);
            }
        }

        [Fact]
        public void Should_Update_Containers_When_Items_Changes()
        {
            var target = CreateTarget();

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            target.Items = new[] { "foo", "bar", "baz" };

            Assert.Equal(3, target.Panel.Children.Count);
        }

        [Fact]
        public void Should_Decrease_The_Viewport_Size_By_One_If_There_Is_A_Partial_Item()
        {
            var target = CreateTarget();

            target.ApplyTemplate();
            target.Measure(new Size(100, 95));
            target.Arrange(new Rect(0, 0, 100, 95));

            Assert.Equal(new Size(100, 9), ((ILogicalScrollable)target).Viewport);
        }

        [Fact]
        public void Moving_To_And_From_The_End_With_Partial_Item_Should_Set_Panel_PixelOffset()
        {
            var target = CreateTarget();

            target.ApplyTemplate();
            target.Measure(new Size(100, 95));
            target.Arrange(new Rect(0, 0, 100, 95));

            ((ILogicalScrollable)target).Offset = new Vector(0, 11);

            var minIndex = target.ItemContainerGenerator.Containers.Min(x => x.Index);
            Assert.Equal(new Vector(0, 11), ((ILogicalScrollable)target).Offset);
            Assert.Equal(10, minIndex);
            Assert.Equal(10, ((IVirtualizingPanel)target.Panel).PixelOffset);

            ((ILogicalScrollable)target).Offset = new Vector(0, 10);

            minIndex = target.ItemContainerGenerator.Containers.Min(x => x.Index);
            Assert.Equal(new Vector(0, 10), ((ILogicalScrollable)target).Offset);
            Assert.Equal(10, minIndex);
            Assert.Equal(0, ((IVirtualizingPanel)target.Panel).PixelOffset);

            ((ILogicalScrollable)target).Offset = new Vector(0, 11);

            minIndex = target.ItemContainerGenerator.Containers.Min(x => x.Index);
            Assert.Equal(new Vector(0, 11), ((ILogicalScrollable)target).Offset);
            Assert.Equal(10, minIndex);
            Assert.Equal(10, ((IVirtualizingPanel)target.Panel).PixelOffset);
        }

        [Fact]
        public void Inserting_Items_Should_Update_Containers()
        {
            var target = CreateTarget();

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            ((ILogicalScrollable)target).Offset = new Vector(0, 5);

            var expected = Enumerable.Range(5, 10).Select(x => $"Item {x}").ToList();
            var items = (ObservableCollection<string>)target.Items;
            var actual = target.Panel.Children.Select(x => x.DataContext).ToList();

            Assert.Equal(expected, actual);

            items.Insert(6, "Inserted");
            expected.Insert(1, "Inserted");
            expected.RemoveAt(expected.Count - 1);

            actual = target.Panel.Children.Select(x => x.DataContext).ToList();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Inserting_Items_Before_Visibile_Containers_Should_Update_Containers()
        {
            var target = CreateTarget();

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            ((ILogicalScrollable)target).Offset = new Vector(0, 5);

            var expected = Enumerable.Range(5, 10).Select(x => $"Item {x}").ToList();
            var items = (ObservableCollection<string>)target.Items;
            var actual = target.Panel.Children.Select(x => x.DataContext).ToList();

            Assert.Equal(expected, actual);

            items.Insert(0, "Inserted");

            expected = Enumerable.Range(4, 10).Select(x => $"Item {x}").ToList();
            actual = target.Panel.Children.Select(x => x.DataContext).ToList();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Removing_First_Materialized_Item_Should_Update_Containers()
        {
            var target = CreateTarget();

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            var expected = Enumerable.Range(0, 10).Select(x => $"Item {x}").ToList();
            var items = (ObservableCollection<string>)target.Items;
            var actual = target.Panel.Children.Select(x => x.DataContext).ToList();

            Assert.Equal(expected, actual);

            items.RemoveAt(0);
            expected = Enumerable.Range(1, 10).Select(x => $"Item {x}").ToList();

            actual = target.Panel.Children.Select(x => x.DataContext).ToList();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Removing_Items_From_Middle_Should_Update_Containers_When_All_Items_Visible()
        {
            var target = CreateTarget();

            target.ApplyTemplate();
            target.Measure(new Size(100, 200));
            target.Arrange(new Rect(0, 0, 100, 200));

            var items = (ObservableCollection<string>)target.Items;
            var actual = target.Panel.Children.Select(x => x.DataContext).ToList();

            Assert.Equal(items, actual);

            items.RemoveAt(2);

            actual = target.Panel.Children.Select(x => x.DataContext).ToList();
            Assert.Equal(items, actual);

            items.RemoveAt(items.Count - 2);

            actual = target.Panel.Children.Select(x => x.DataContext).ToList();
            Assert.Equal(items, actual);
        }

        [Fact]
        public void Removing_Last_Item_Should_Update_Containers_When_All_Items_Visible()
        {
            var target = CreateTarget();

            target.ApplyTemplate();
            target.Measure(new Size(100, 200));
            target.Arrange(new Rect(0, 0, 100, 200));

            ((ILogicalScrollable)target).Offset = new Vector(0, 5);

            var expected = Enumerable.Range(0, 20).Select(x => $"Item {x}").ToList();
            var items = (ObservableCollection<string>)target.Items;
            var actual = target.Panel.Children.Select(x => x.DataContext).ToList();

            Assert.Equal(expected, actual);

            items.Remove(items.Last());
            expected.Remove(expected.Last());

            actual = target.Panel.Children.Select(x => x.DataContext).ToList();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Removing_Items_When_Scrolled_To_End_Should_Recyle_Containers_At_Top()
        {
            var target = CreateTarget(useAvaloniaList: true);

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            ((ILogicalScrollable)target).Offset = new Vector(0, 10);

            var expected = Enumerable.Range(10, 10).Select(x => $"Item {x}").ToList();
            var items = (AvaloniaList<string>)target.Items;
            var actual = target.Panel.Children.Select(x => x.DataContext).ToList();

            Assert.Equal(expected, actual);

            items.RemoveRange(18, 2);
            expected = Enumerable.Range(8, 10).Select(x => $"Item {x}").ToList();

            actual = target.Panel.Children.Select(x => x.DataContext).ToList();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Removing_Items_When_Scrolled_To_Near_End_Should_Recycle_Containers_At_Bottom_And_Top()
        {
            var target = CreateTarget(useAvaloniaList: true);

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            ((ILogicalScrollable)target).Offset = new Vector(0, 9);

            var expected = Enumerable.Range(9, 10).Select(x => $"Item {x}").ToList();
            var items = (AvaloniaList<string>)target.Items;
            var actual = target.Panel.Children.Select(x => x.DataContext).ToList();

            Assert.Equal(expected, actual);

            items.RemoveRange(15, 3);
            expected = Enumerable.Range(7, 8).Select(x => $"Item {x}")
                .Concat(Enumerable.Range(18, 2).Select(x => $"Item {x}"))
                .ToList();

            actual = target.Panel.Children.Select(x => x.DataContext).ToList();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Measuring_To_Infinity_When_Scrolled_To_End_Should_Not_Throw()
        {
            var target = CreateTarget(useAvaloniaList: true);

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            ((ILogicalScrollable)target).Offset = new Vector(0, 10);

            // Check for issue #589: this should not throw.
            target.Measure(Size.Infinity);

            var expected = Enumerable.Range(0, 20).Select(x => $"Item {x}").ToList();
            var items = (AvaloniaList<string>)target.Items;
            var actual = target.Panel.Children.Select(x => x.DataContext).ToList();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Replacing_Items_Should_Update_Containers()
        {
            var target = CreateTarget();

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            var expected = Enumerable.Range(0, 10).Select(x => $"Item {x}").ToList();
            var items = (ObservableCollection<string>)target.Items;
            var actual = target.Panel.Children.Select(x => x.DataContext).ToList();

            Assert.Equal(expected, actual);

            items[4] = expected[4] = "Replaced";

            actual = target.Panel.Children.Select(x => x.DataContext).ToList();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Moving_Items_Should_Update_Containers()
        {
            var target = CreateTarget();

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            var expected = Enumerable.Range(0, 10).Select(x => $"Item {x}").ToList();
            var items = (ObservableCollection<string>)target.Items;
            var actual = target.Panel.Children.Select(x => x.DataContext).ToList();

            Assert.Equal(expected, actual);

            items.Move(4, 8);
            var i = expected[4];
            expected.RemoveAt(4);
            expected.Insert(8, i);

            actual = target.Panel.Children.Select(x => x.DataContext).ToList();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Setting_Items_To_Null_Should_Remove_Containers()
        {
            var target = CreateTarget();

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            var expected = Enumerable.Range(0, 10).Select(x => $"Item {x}").ToList();
            var items = (ObservableCollection<string>)target.Items;
            var actual = target.Panel.Children.Select(x => x.DataContext).ToList();

            Assert.Equal(expected, actual);

            target.Items = null;

            Assert.Empty(target.Panel.Children);
        }

        [Fact]
        public void Reassigning_Items_Should_Create_Containers()
        {
            var target = CreateTarget(itemCount: 5);

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            var expected = Enumerable.Range(0, 5).Select(x => $"Item {x}").ToList();
            var items = (ObservableCollection<string>)target.Items;
            var actual = target.Panel.Children.Select(x => x.DataContext).ToList();

            Assert.Equal(expected, actual);

            expected = Enumerable.Range(0, 6).Select(x => $"Item {x}").ToList();
            target.Items = expected;

            actual = target.Panel.Children.Select(x => x.DataContext).ToList();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Inserting_Then_Removing_Should_Add_Remove_Containers()
        {
            var items = new AvaloniaList<string>(Enumerable.Range(0, 5).Select(x => $"Item {x}"));
            var toAdd = Enumerable.Range(0, 3).Select(x => $"Added Item {x}").ToArray();
            var target = new ItemsPresenter
            {
                VirtualizationMode = ItemVirtualizationMode.None,
                Items = items,
                ItemTemplate = new FuncDataTemplate<string>(x => new TextBlock { Height = 10 }),
            };

            target.ApplyTemplate();

            Assert.Equal(items.Count, target.Panel.Children.Count);

            int addIndex = 1;
            foreach (var item in toAdd)
            {
                items.Insert(addIndex++, item);
            }

            Assert.Equal(items.Count, target.Panel.Children.Count);

            foreach (var item in toAdd)
            {
                items.Remove(item);
            }

            Assert.Equal(items.Count, target.Panel.Children.Count);
        }

        [Fact]
        public void Reassigning_Items_Should_Remove_Containers()
        {
            var target = CreateTarget(itemCount: 6);

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            var expected = Enumerable.Range(0, 6).Select(x => $"Item {x}").ToList();
            var actual = target.Panel.Children.Select(x => x.DataContext).ToList();

            Assert.Equal(expected, actual);

            expected = Enumerable.Range(0, 5).Select(x => $"Item {x}").ToList();
            target.Items = expected;

            actual = target.Panel.Children.Select(x => x.DataContext).ToList();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Clearing_Items_And_ReAdding_Should_Remove_Containers()
        {
            var target = CreateTarget(itemCount: 6);

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            var expected = Enumerable.Range(0, 6).Select(x => $"Item {x}").ToList();
            var items = (ObservableCollection<string>)target.Items;
            var actual = target.Panel.Children.Select(x => x.DataContext).ToList();

            Assert.Equal(expected, actual);

            expected = Enumerable.Range(0, 5).Select(x => $"Item {x}").ToList();
            target.Items = null;
            target.Items = expected;

            actual = target.Panel.Children.Select(x => x.DataContext).ToList();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Scrolling_To_Partial_Last_Item_Then_Adding_Item_Updates_Containers()
        {
            var target = CreateTarget(itemCount: 10);
            var items = (IList<string>)target.Items;

            target.ApplyTemplate();
            target.Measure(new Size(100, 95));
            target.Arrange(new Rect(0, 0, 100, 95));

            ((ILogicalScrollable)target).Offset = new Vector(0, 1);
            Assert.Equal(new Vector(0, 1), ((ILogicalScrollable)target).Offset);

            var expected = Enumerable.Range(0, 10).Select(x => $"Item {x}").ToList();
            var actual = target.Panel.Children.Select(x => x.DataContext).ToList();
            Assert.Equal(expected, actual);
            Assert.Equal(10, ((IVirtualizingPanel)target.Panel).PixelOffset);

            items.Add("Item 10");

            expected = Enumerable.Range(1, 10).Select(x => $"Item {x}").ToList();
            actual = target.Panel.Children.Select(x => x.DataContext).ToList();
            Assert.Equal(expected, actual);
            Assert.Equal(0, ((IVirtualizingPanel)target.Panel).PixelOffset);
        }

        [Fact]
        public void Scrolling_To_Item_In_Zero_Sized_Presenter_Doesnt_Throw()
        {
            using (UnitTestApplication.Start(TestServices.RealLayoutManager))
            {
                var target = CreateTarget(itemCount: 10);
                var items = (IList<string>)target.Items;

                target.ApplyTemplate();
                target.Measure(Size.Empty);
                target.Arrange(Rect.Empty);

                // Check for issue #591: this should not throw.
                target.ScrollIntoView(items[0]);
            }
        }

        [Fact]
        public void InsertRange_Items_Should_Update_Containers()
        {
            var target = CreateTarget(useAvaloniaList: true);

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            var expected = Enumerable.Range(0, 10).Select(x => $"Item {x}").ToList();
            var items = (AvaloniaList<string>)target.Items;
            var actual = target.Panel.Children.Select(x => x.DataContext).ToList();

            Assert.Equal(expected, actual);

            var toAdd = Enumerable.Range(0, 3).Select(x => $"New Item {x}").ToList();

            int index = 1;

            items.InsertRange(index, toAdd);
            expected.InsertRange(index, toAdd);
            expected.RemoveRange(10, toAdd.Count);

            actual = target.Panel.Children.Select(x => x.DataContext).ToList();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void InsertRange_Items_Before_Last_Should_Update_Containers()
        {
            var target = CreateTarget(useAvaloniaList: true);

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            var expected = Enumerable.Range(0, 10).Select(x => $"Item {x}").ToList();
            var items = (AvaloniaList<string>)target.Items;
            var actual = target.Panel.Children.Select(x => x.DataContext).ToList();

            Assert.Equal(expected, actual);

            var toAdd = Enumerable.Range(0, 3).Select(x => $"New Item {x}").ToList();

            int index = 8;

            items.InsertRange(index, toAdd);
            expected.InsertRange(index, toAdd);
            expected.RemoveRange(10, toAdd.Count);

            actual = target.Panel.Children.Select(x => x.DataContext).ToList();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void RemoveRange_Items_Should_Update_Containers()
        {
            var target = CreateTarget(useAvaloniaList: true);

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            var expected = Enumerable.Range(0, 13).Select(x => $"Item {x}").ToList();
            var items = (AvaloniaList<string>)target.Items;
            var actual = target.Panel.Children.Select(x => x.DataContext).ToList();

            Assert.Equal(expected.Take(10), actual);

            int index = 5;
            int count = 3;

            items.RemoveRange(index, count);
            expected.RemoveRange(index, count);

            actual = target.Panel.Children.Select(x => x.DataContext).ToList();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void RemoveRange_Items_Before_Last_Should_Update_Containers()
        {
            var target = CreateTarget(useAvaloniaList: true);

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            var expected = Enumerable.Range(0, 13).Select(x => $"Item {x}").ToList();
            var items = (AvaloniaList<string>)target.Items;
            var actual = target.Panel.Children.Select(x => x.DataContext).ToList();

            Assert.Equal(expected.Take(10), actual);

            int index = 8;
            int count = 3;

            items.RemoveRange(index, count);
            expected.RemoveRange(index, count);

            actual = target.Panel.Children.Select(x => x.DataContext).ToList();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Should_Add_Containers_For_Items_After_Clear()
        {
            var target = CreateTarget(itemCount: 10);
            var defaultItems = (IList<string>)target.Items;
            var items = new AvaloniaList<string>(defaultItems);
            target.Items = items;

            target.ApplyTemplate();
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(10, target.Panel.Children.Count);

            items.Clear();

            target.Panel.Measure(new Size(100, 100));
            target.Panel.Arrange(new Rect(target.Panel.DesiredSize));

            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Empty(target.Panel.Children);

            items.AddRange(defaultItems.Select(s => s + " new"));

            target.Panel.Measure(new Size(100, 100));
            target.Panel.Arrange(new Rect(target.Panel.DesiredSize));

            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(10, target.Panel.Children.Count);
        }

        public class Vertical
        {
            [Fact]
            public void GetControlInDirection_Down_Should_Return_Existing_Container_If_Materialized()
            {
                var target = CreateTarget();

                target.ApplyTemplate();
                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(0, 0, 100, 100));

                var from = target.Panel.Children[5];
                var result = ((ILogicalScrollable)target).GetControlInDirection(
                    NavigationDirection.Down,
                    from);

                Assert.Same(target.Panel.Children[6], result);
            }

            [Fact]
            public void GetControlInDirection_Down_Should_Scroll_If_Necessary()
            {
                var target = CreateTarget();

                target.ApplyTemplate();
                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(0, 0, 100, 100));

                var from = target.Panel.Children[9];
                var result = ((ILogicalScrollable)target).GetControlInDirection(
                    NavigationDirection.Down,
                    from);

                Assert.Equal(new Vector(0, 1), ((ILogicalScrollable)target).Offset);
                Assert.Same(target.Panel.Children[9], result);
            }

            [Fact]
            public void GetControlInDirection_Down_Should_Scroll_If_Partially_Visible()
            {
                using (UnitTestApplication.Start(TestServices.RealLayoutManager))
                {
                    var target = CreateTarget();
                    var scroller = (ScrollContentPresenter)target.Parent;

                    scroller.Measure(new Size(100, 95));
                    scroller.Arrange(new Rect(0, 0, 100, 95));

                    var from = target.Panel.Children[8];
                    var result = ((ILogicalScrollable)target).GetControlInDirection(
                        NavigationDirection.Down,
                        from);

                    Assert.Equal(new Vector(0, 1), ((ILogicalScrollable)target).Offset);
                    Assert.Same(target.Panel.Children[8], result);
                }
            }

            [Fact]
            public void GetControlInDirection_Up_Should_Scroll_If_Partially_Visible_Item_Is_Currently_Shown()
            {
                using (UnitTestApplication.Start(TestServices.RealLayoutManager))
                {
                    var target = CreateTarget();
                    var scroller = (ScrollContentPresenter)target.Parent;

                    scroller.Measure(new Size(100, 95));
                    scroller.Arrange(new Rect(0, 0, 100, 95));
                    ((ILogicalScrollable)target).Offset = new Vector(0, 11);

                    var from = target.Panel.Children[1];
                    var result = ((ILogicalScrollable)target).GetControlInDirection(
                        NavigationDirection.Up,
                        from);

                    Assert.Equal(new Vector(0, 10), ((ILogicalScrollable)target).Offset);
                    Assert.Same(target.Panel.Children[0], result);
                }
            }

            [Fact]
            public void Should_Return_Horizontal_Extent_And_Viewport()
            {
                var target = CreateTarget();

                target.ApplyTemplate();
                target.Measure(new Size(5, 100));
                target.Arrange(new Rect(0, 0, 5, 100));

                Assert.Equal(new Size(10, 20), ((ILogicalScrollable)target).Extent);
                Assert.Equal(new Size(5, 10), ((ILogicalScrollable)target).Viewport);
            }

            [Fact]
            public void Horizontal_Scroll_Should_Update_Item_Position()
            {
                var target = CreateTarget();

                target.ApplyTemplate();

                target.Measure(new Size(5, 100));
                target.Arrange(new Rect(0, 0, 5, 100));

                ((ILogicalScrollable)target).Offset = new Vector(5, 0);

                target.Measure(new Size(5, 100));
                target.Arrange(new Rect(0, 0, 5, 100));

                Assert.Equal(new Rect(-5, 0, 10, 10), target.Panel.Children[0].Bounds);
            }
        }

        public class Horizontal
        {
            [Fact]
            public void GetControlInDirection_Right_Should_Return_Existing_Container_If_Materialized()
            {
                var target = CreateTarget(orientation: Orientation.Horizontal);

                target.ApplyTemplate();
                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(0, 0, 100, 100));

                var from = target.Panel.Children[5];
                var result = ((ILogicalScrollable)target).GetControlInDirection(
                    NavigationDirection.Right,
                    from);

                Assert.Same(target.Panel.Children[6], result);
            }

            [Fact]
            public void GetControlInDirection_Right_Should_Scroll_If_Necessary()
            {
                var target = CreateTarget(orientation: Orientation.Horizontal);

                target.ApplyTemplate();
                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(0, 0, 100, 100));

                var from = target.Panel.Children[9];
                var result = ((ILogicalScrollable)target).GetControlInDirection(
                    NavigationDirection.Right,
                    from);

                Assert.Equal(new Vector(1, 0), ((ILogicalScrollable)target).Offset);
                Assert.Same(target.Panel.Children[9], result);
            }

            [Fact]
            public void GetControlInDirection_Right_Should_Scroll_If_Partially_Visible()
            {
                using (UnitTestApplication.Start(TestServices.RealLayoutManager))
                {
                    var target = CreateTarget(orientation: Orientation.Horizontal);
                    var scroller = (ScrollContentPresenter)target.Parent;

                    scroller.Measure(new Size(95, 100));
                    scroller.Arrange(new Rect(0, 0, 95, 100));

                    var from = target.Panel.Children[8];
                    var result = ((ILogicalScrollable)target).GetControlInDirection(
                        NavigationDirection.Right,
                        from);

                    Assert.Equal(new Vector(1, 0), ((ILogicalScrollable)target).Offset);
                    Assert.Same(target.Panel.Children[8], result);
                }
            }

            [Fact]
            public void GetControlInDirection_Left_Should_Scroll_If_Partially_Visible_Item_Is_Currently_Shown()
            {
                var target = CreateTarget(orientation: Orientation.Horizontal);

                target.ApplyTemplate();
                target.Measure(new Size(95, 100));
                target.Arrange(new Rect(0, 0, 95, 100));
                ((ILogicalScrollable)target).Offset = new Vector(11, 0);

                var from = target.Panel.Children[1];
                var result = ((ILogicalScrollable)target).GetControlInDirection(
                    NavigationDirection.Left,
                    from);

                Assert.Equal(new Vector(10, 0), ((ILogicalScrollable)target).Offset);
                Assert.Same(target.Panel.Children[0], result);
            }
        }

        public class WithContainers
        {
            [Fact]
            public void Scrolling_Less_Than_A_Page_Should_Move_Recycled_Items()
            {
                var target = CreateTarget();
                var items = (IList<string>)target.Items;

                target.ApplyTemplate();
                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(0, 0, 100, 100));

                var containers = target.Panel.Children.ToList();
                var scroller = (ScrollContentPresenter)target.Parent;

                scroller.Offset = new Vector(0, 5);

                var scrolledContainers = containers
                    .Skip(5)
                    .Take(5)
                    .Concat(containers.Take(5)).ToList();

                Assert.Equal(new Vector(0, 5), ((ILogicalScrollable)target).Offset);
                Assert.Equal(scrolledContainers, target.Panel.Children);

                for (var i = 0; i < target.Panel.Children.Count; ++i)
                {
                    Assert.Equal(items[i + 5], target.Panel.Children[i].DataContext);
                }

                scroller.Offset = new Vector(0, 0);
                Assert.Equal(new Vector(0, 0), ((ILogicalScrollable)target).Offset);
                Assert.Equal(containers, target.Panel.Children);

                var dcs = target.Panel.Children.Select(x => x.DataContext).ToList();

                for (var i = 0; i < target.Panel.Children.Count; ++i)
                {
                    Assert.Equal(items[i], target.Panel.Children[i].DataContext);
                }
            }

            [Fact]
            public void Scrolling_More_Than_A_Page_Should_Recycle_Items()
            {
                var target = CreateTarget(itemCount: 50);
                var items = (IList<string>)target.Items;

                target.ApplyTemplate();
                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(0, 0, 100, 100));

                var containers = target.Panel.Children.ToList();
                var scroller = (ScrollContentPresenter)target.Parent;

                scroller.Offset = new Vector(0, 20);

                Assert.Equal(new Vector(0, 20), ((ILogicalScrollable)target).Offset);
                Assert.Equal(containers, target.Panel.Children);

                for (var i = 0; i < target.Panel.Children.Count; ++i)
                {
                    Assert.Equal(items[i + 20], target.Panel.Children[i].DataContext);
                }

                scroller.Offset = new Vector(0, 0);

                Assert.Equal(new Vector(0, 0), ((ILogicalScrollable)target).Offset);
                Assert.Equal(containers, target.Panel.Children);

                for (var i = 0; i < target.Panel.Children.Count; ++i)
                {
                    Assert.Equal(items[i], target.Panel.Children[i].DataContext);
                }
            }
        }

        private static ItemsPresenter CreateTarget(
            Orientation orientation = Orientation.Vertical,
            bool useContainers = true,
            int itemCount = 20,
            bool useAvaloniaList = false)
        {
            ItemsPresenter result;
            var itemsSource = Enumerable.Range(0, itemCount).Select(x => $"Item {x}");
            var items = useAvaloniaList ?
                (IEnumerable)new AvaloniaList<string>(itemsSource) :
                (IEnumerable)new ObservableCollection<string>(itemsSource);

            var scroller = new TestScroller
            {
                CanHorizontallyScroll = true,
                CanVerticallyScroll = true,
                Content = result = new TestItemsPresenter(useContainers)
                {
                    Items = items,
                    ItemsPanel = VirtualizingPanelTemplate(orientation),
                    ItemTemplate = ItemTemplate(),
                    VirtualizationMode = ItemVirtualizationMode.Simple,
                }
            };

            scroller.UpdateChild();

            return result;
        }

        private static IDataTemplate ItemTemplate()
        {
            return new FuncDataTemplate<string>(x => new Canvas
            {
                Width = 10,
                Height = 10,
            });
        }

        private static ITemplate<IPanel> VirtualizingPanelTemplate(
            Orientation orientation = Orientation.Vertical)
        {
            return new FuncTemplate<IPanel>(() => new VirtualizingStackPanel
            {
                Orientation = orientation,
            });
        }

        private class TestScroller : ScrollContentPresenter, IRenderRoot
        {
            public IRenderer Renderer { get; }
            public Size ClientSize { get; }
            public double RenderScaling => 1;

            public IRenderTarget CreateRenderTarget()
            {
                throw new NotImplementedException();
            }

            public void Invalidate(Rect rect)
            {
                throw new NotImplementedException();
            }

            public Point PointToClient(Point point)
            {
                throw new NotImplementedException();
            }

            public Point PointToScreen(Point point)
            {
                throw new NotImplementedException();
            }
        }

        private class TestItemsPresenter : ItemsPresenter
        {
            private bool _useContainers;

            public TestItemsPresenter(bool useContainers)
            {
                _useContainers = useContainers;
            }

            protected override IItemContainerGenerator CreateItemContainerGenerator()
            {
                return _useContainers ?
                    new ItemContainerGenerator<TestContainer>(this, TestContainer.ContentProperty, null) :
                    new ItemContainerGenerator(this);
            }
        }

        private class TestContainer : ContentControl
        {
            public TestContainer()
            {
                Width = 10;
                Height = 10;
            }
        }
    }
}