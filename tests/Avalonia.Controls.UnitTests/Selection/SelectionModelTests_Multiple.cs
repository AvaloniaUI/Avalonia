using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.Controls.Selection;
using Xunit;

#nullable enable

namespace Avalonia.Controls.UnitTests.Selection
{
    public class SelectionModelTests_Multiple
    {
        public class No_Source
        {
            [Fact]
            public void Can_Select_Multiple_Items_Before_Source_Assigned()
            {
                var target = CreateTarget(false);
                var raised = 0;

                target.SelectionChanged += (s, e) =>
                {
                    var index = raised switch
                    {
                        0 => 5,
                        1 => 10,
                        2 => 100,
                        _ => throw new NotSupportedException(),
                    };

                    Assert.Empty(e.DeselectedIndexes);
                    Assert.Empty(e.DeselectedItems);
                    Assert.Equal(new[] { index }, e.SelectedIndexes);
                    Assert.Equal(new string?[] { null }, e.SelectedItems);
                    ++raised;
                };

                target.SelectedIndex = 5;
                target.Select(10);
                target.Select(100);

                Assert.Equal(5, target.SelectedIndex);
                Assert.Equal(new[] { 5, 10, 100 }, target.SelectedIndexes);
                Assert.Null(target.SelectedItem);
                Assert.Equal(new string?[] { null, null, null }, target.SelectedItems);
                Assert.Equal(3, raised);
            }

            [Fact]
            public void Initializing_Source_Retains_Valid_Selection_And_Removes_Invalid()
            {
                var target = CreateTarget(false);
                var raised = 0;

                target.SelectedIndex = 1;
                target.Select(2);
                target.Select(10);
                target.Select(100);

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Equal(new[] { 10, 100 }, e.DeselectedIndexes);
                    Assert.Equal(new string?[] { null, null }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndexes);
                    Assert.Empty(e.SelectedItems);
                    ++raised;
                };

                target.Source = new[] { "foo", "bar", "baz" };

                Assert.Equal(1, target.SelectedIndex);
                Assert.Equal(new[] { 1, 2 }, target.SelectedIndexes);
                Assert.Equal("bar", target.SelectedItem);
                Assert.Equal(new[] { "bar", "baz" }, target.SelectedItems);
                Assert.Equal(1, raised);
            }

            [Fact]
            public void Initializing_Source_Coerces_SelectedIndex()
            {
                var target = CreateTarget(false);
                var raised = 0;

                target.SelectedIndex = 100;
                target.Select(2);

                target.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(target.SelectedIndex))
                    {
                        ++raised;
                    }
                };

                target.Source = new[] { "foo", "bar", "baz" };

                Assert.Equal(2, target.SelectedIndex);
                Assert.Equal(new[] { 2 }, target.SelectedIndexes);
                Assert.Equal("baz", target.SelectedItem);
                Assert.Equal(new[] { "baz" }, target.SelectedItems);
                Assert.Equal(1, raised);
            }

            [Fact]
            public void Initializing_Source_Doesnt_Raise_SelectionChanged_If_Selection_Valid()
            {
                var target = CreateTarget(false);
                var raised = 0;

                target.Select(1);
                target.Select(2);

                target.SelectionChanged += (s, e) =>
                {
                    ++raised;
                };

                target.Source = new[] { "foo", "bar", "baz" };

                Assert.Equal(0, raised);
            }

            [Fact]
            public void Initializing_Source_Respects_Range_SourceItem_Order()
            {
                var target = CreateTarget(false);

                target.SelectRange(2, 2);
                target.SelectedItem = "bar";

                target.Source = new[] { "foo", "bar", "baz" };

                Assert.Equal(1, target.SelectedIndex);
                Assert.Equal(new[] { 1 }, target.SelectedIndexes);
                Assert.Equal("bar", target.SelectedItem);
                Assert.Equal(new[] { "bar" }, target.SelectedItems);
            }

            [Fact]
            public void Initializing_Source_Respects_SourceItem_Range_Order()
            {
                var target = CreateTarget(false);

                target.SelectedItem = "baz";
                target.SelectRange(1, 1);

                target.Source = new[] { "foo", "bar", "baz" };

                Assert.Equal(1, target.SelectedIndex);
                Assert.Equal(new[] { 1 }, target.SelectedIndexes);
                Assert.Equal("bar", target.SelectedItem);
                Assert.Equal(new[] { "bar" }, target.SelectedItems);
            }
        }

        public class SelectedIndex
        {
            [Fact]
            public void SelectedIndex_Larger_Than_Source_Clears_Selection()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectedIndex = 1;

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Equal(new[] { 1 }, e.DeselectedIndexes);
                    Assert.Equal(new[] { "bar" }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndexes);
                    Assert.Empty(e.SelectedItems);
                    ++raised;
                };

                target.SelectedIndex = 15;

                Assert.Equal(-1, target.SelectedIndex);
                Assert.Empty(target.SelectedIndexes);
                Assert.Null(target.SelectedItem);
                Assert.Empty(target.SelectedItems);
                Assert.Equal(1, raised);
            }

            [Fact]
            public void Negative_SelectedIndex_Is_Coerced_To_Minus_1()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectionChanged += (s, e) => ++raised;

                target.SelectedIndex = -5;

                Assert.Equal(-1, target.SelectedIndex);
                Assert.Empty(target.SelectedIndexes);
                Assert.Null(target.SelectedItem);
                Assert.Empty(target.SelectedItems);
                Assert.Equal(0, raised);
            }

            [Fact]
            public void Setting_SelectedIndex_Clears_Old_Selection()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectedIndex = 0;

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Equal(new[] { 0 }, e.DeselectedIndexes);
                    Assert.Equal(new[] { "foo" }, e.DeselectedItems);
                    Assert.Equal(new[] { 1 }, e.SelectedIndexes);
                    Assert.Equal(new[] { "bar" }, e.SelectedItems);
                    ++raised;
                };

                target.SelectedIndex = 1;

                Assert.Equal(1, target.SelectedIndex);
                Assert.Equal(new[] { 1 }, target.SelectedIndexes);
                Assert.Equal("bar", target.SelectedItem);
                Assert.Equal(new[] { "bar" }, target.SelectedItems);
                Assert.Equal(1, raised);
            }

            [Fact]
            public void PropertyChanged_Is_Raised()
            {
                var target = CreateTarget();
                var raised = 0;
                
                target.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(target.SelectedIndex))
                    {
                        ++raised;
                    }
                };

                target.SelectedIndex = 1;

                Assert.Equal(1, raised);
            }
        }

        public class SelectedIndexes
        {
            [Fact]
            public void PropertyChanged_Is_Raised_When_SelectedIndex_Changes()
            {
                var target = CreateTarget();
                var raised = 0;

                target.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(target.SelectedIndexes))
                    {
                        ++raised;
                    }
                };

                target.SelectedIndex = 1;

                Assert.Equal(1, raised);
            }
        }

        public class SelectedItem
        {
            [Fact]
            public void PropertyChanged_Is_Raised_When_SelectedIndex_Changes()
            {
                var target = CreateTarget();
                var raised = 0;

                target.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(target.SelectedItem))
                    {
                        ++raised;
                    }
                };

                target.SelectedIndex = 1;

                Assert.Equal(1, raised);
            }
        }

        public class SelectedItems
        {
            [Fact]
            public void PropertyChanged_Is_Raised_When_SelectedIndex_Changes()
            {
                var target = CreateTarget();
                var raised = 0;

                target.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(target.SelectedItems))
                    {
                        ++raised;
                    }
                };

                target.SelectedIndex = 1;

                Assert.Equal(1, raised);
            }
        }

        public class Select
        {
            [Fact]
            public void Select_Sets_SelectedIndex_If_Previously_Unset()
            {
                var target = CreateTarget();
                var raised = 0;

                target.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(target.SelectedIndex))
                    {
                        ++raised;
                    }
                };

                target.Select(1);

                Assert.Equal(1, target.SelectedIndex);
                Assert.Equal(1, raised);
            }

            [Fact]
            public void Select_Adds_To_Selection()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectedIndex = 0;

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Empty(e.DeselectedIndexes);
                    Assert.Empty(e.DeselectedItems);
                    Assert.Equal(new[] { 1 }, e.SelectedIndexes);
                    Assert.Equal(new[] { "bar" }, e.SelectedItems);
                    ++raised;
                };

                target.Select(1);

                Assert.Equal(0, target.SelectedIndex);
                Assert.Equal(new[] { 0, 1 }, target.SelectedIndexes);
                Assert.Equal("foo", target.SelectedItem);
                Assert.Equal(new[] { "foo", "bar" }, target.SelectedItems);
                Assert.Equal(1, raised);
            }

            [Fact]
            public void Select_With_Invalid_Index_Does_Nothing()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectedIndex = 0;

                target.PropertyChanged += (s, e) => ++raised;
                target.SelectionChanged += (s, e) => ++raised;

                target.Select(15);

                Assert.Equal(0, target.SelectedIndex);
                Assert.Equal(new[] { 0 }, target.SelectedIndexes);
                Assert.Equal("foo", target.SelectedItem);
                Assert.Equal(new[] { "foo" }, target.SelectedItems);
                Assert.Equal(0, raised);
            }

            [Fact]
            public void Selecting_Already_Selected_Item_Doesnt_Raise_SelectionChanged()
            {
                var target = CreateTarget();
                var raised = 0;

                target.Select(2);
                target.SelectionChanged += (s, e) => ++raised;
                target.Select(2);

                Assert.Equal(0, raised);
            }
        }

        public class SelectRange
        {
            [Fact]
            public void SelectRange_Selects_Items()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Empty(e.DeselectedIndexes);
                    Assert.Empty(e.DeselectedItems);
                    Assert.Equal(new[] { 1, 2 }, e.SelectedIndexes);
                    Assert.Equal(new[] { "bar", "baz" }, e.SelectedItems);
                    ++raised;
                };

                target.SelectRange(1, 2);

                Assert.Equal(1, target.SelectedIndex);
                Assert.Equal(new[] { 1, 2 }, target.SelectedIndexes);
                Assert.Equal("bar", target.SelectedItem);
                Assert.Equal(new[] { "bar", "baz" }, target.SelectedItems);
                Assert.Equal(1, target.AnchorIndex);
                Assert.Equal(1, raised);
            }

            [Fact]
            public void SelectRange_Ignores_Out_Of_Bounds_Items()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Empty(e.DeselectedIndexes);
                    Assert.Empty(e.DeselectedItems);
                    Assert.Equal(new[] { 11, 12 }, e.SelectedIndexes);
                    Assert.Equal(new[] { "xyzzy", "thud" }, e.SelectedItems);
                    ++raised;
                };

                target.SelectRange(11, 20);

                Assert.Equal(11, target.SelectedIndex);
                Assert.Equal(new[] { 11, 12 }, target.SelectedIndexes);
                Assert.Equal("xyzzy", target.SelectedItem);
                Assert.Equal(new[] { "xyzzy", "thud" }, target.SelectedItems);
                Assert.Equal(11, target.AnchorIndex);
                Assert.Equal(1, raised);
            }

            [Fact]
            public void SelectRange_Does_Nothing_For_Non_Intersecting_Range()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectionChanged += (s, e) => ++raised;

                target.SelectRange(18, 30);

                Assert.Equal(-1, target.SelectedIndex);
                Assert.Equal(-1, target.AnchorIndex);
                Assert.Equal(0, raised);
            }
        }

        public class Deselect
        {
            [Fact]
            public void Deselect_Clears_Selected_Item()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectedIndex = 0;
                target.Select(1);

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Equal(new[] { 1 }, e.DeselectedIndexes);
                    Assert.Equal(new[] { "bar" }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndexes);
                    Assert.Empty(e.SelectedItems);
                    ++raised;
                };

                target.Deselect(1);

                Assert.Equal(0, target.SelectedIndex);
                Assert.Equal(new[] { 0 }, target.SelectedIndexes);
                Assert.Equal("foo", target.SelectedItem);
                Assert.Equal(new[] { "foo" }, target.SelectedItems);
                Assert.Equal(1, raised);
            }

            [Fact]
            public void Deselect_Updates_SelectedItem_To_First_Selected_Item()
            {
                var target = CreateTarget();

                target.SelectRange(3, 5);
                target.Deselect(3);

                Assert.Equal(4, target.SelectedIndex);
            }
        }

        public class DeselectRange
        {
            [Fact]
            public void DeselectRange_Clears_Identical_Range()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectRange(1, 2);

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Equal(new[] { 1, 2 }, e.DeselectedIndexes);
                    Assert.Equal(new[] { "bar", "baz" }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndexes);
                    Assert.Empty(e.SelectedItems);
                    ++raised;
                };

                target.DeselectRange(1, 2);

                Assert.Equal(-1, target.SelectedIndex);
                Assert.Empty(target.SelectedIndexes);
                Assert.Null(target.SelectedItem);
                Assert.Empty(target.SelectedItems);
                Assert.Equal(1, raised);
            }

            [Fact]
            public void DeselectRange_Clears_Intersecting_Range()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectRange(1, 2);

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Equal(new[] { 1 }, e.DeselectedIndexes);
                    Assert.Equal(new[] { "bar" }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndexes);
                    Assert.Empty(e.SelectedItems);
                    ++raised;
                };

                target.DeselectRange(0, 1);

                Assert.Equal(2, target.SelectedIndex);
                Assert.Equal(new[] { 2 }, target.SelectedIndexes);
                Assert.Equal("baz", target.SelectedItem);
                Assert.Equal(new[] { "baz" }, target.SelectedItems);
                Assert.Equal(1, raised);
            }

            [Fact]
            public void DeselectRange_Does_Nothing_For_Nonintersecting_Range()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectedIndex = 0;
                target.SelectionChanged += (s, e) => ++raised;
                target.DeselectRange(1, 2);

                Assert.Equal(0, target.SelectedIndex);
                Assert.Equal(new[] { 0 }, target.SelectedIndexes);
                Assert.Equal("foo", target.SelectedItem);
                Assert.Equal(new[] { "foo" }, target.SelectedItems);
                Assert.Equal(0, raised);
            }
        }

        public class Clear
        {
            [Fact]
            public void Clear_Raises_SelectionChanged()
            {
                var target = CreateTarget();
                var raised = 0;

                target.Select(1);
                target.Select(2);

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Equal(new[] { 1, 2 }, e.DeselectedIndexes);
                    Assert.Equal(new[] { "bar", "baz" }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndexes);
                    Assert.Empty(e.SelectedItems);
                    ++raised;
                };

                target.Clear();

                Assert.Equal(1, raised);
            }
        }

        public class AnchorIndex
        {
            [Fact]
            public void Setting_SelectedIndex_Sets_AnchorIndex()
            {
                var target = CreateTarget();
                var raised = 0;

                target.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(target.AnchorIndex))
                    {
                        ++raised;
                    }
                };

                target.SelectedIndex = 1;

                Assert.Equal(1, target.AnchorIndex);
                Assert.Equal(1, raised);
            }

            [Fact]
            public void Setting_SelectedIndex_To_Minus_1_Doesnt_Clear_AnchorIndex()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectedIndex = 1;

                target.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(target.AnchorIndex))
                    {
                        ++raised;
                    }
                };

                target.SelectedIndex = -1;

                Assert.Equal(1, target.AnchorIndex);
                Assert.Equal(0, raised);
            }

            [Fact]
            public void Select_Sets_AnchorIndex()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectedIndex = 0;

                target.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(target.AnchorIndex))
                    {
                        ++raised;
                    }
                };

                target.Select(1);

                Assert.Equal(1, target.AnchorIndex);
                Assert.Equal(1, raised);
            }

            [Fact]
            public void SelectRange_Doesnt_Overwrite_AnchorIndex()
            {
                var target = CreateTarget();
                var raised = 0;

                target.AnchorIndex = 0;

                target.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(target.AnchorIndex))
                    {
                        ++raised;
                    }
                };

                target.SelectRange(1, 2);

                Assert.Equal(0, target.AnchorIndex);
                Assert.Equal(0, raised);
            }

            [Fact]
            public void Deselect_Doesnt_Clear_AnchorIndex()
            {
                var target = CreateTarget();
                var raised = 0;

                target.Select(0);
                target.Select(1);

                target.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(target.AnchorIndex))
                    {
                        ++raised;
                    }
                };

                target.Deselect(1);

                Assert.Equal(1, target.AnchorIndex);
                Assert.Equal(0, raised);
            }
        }

        public class CollectionChanges
        {
            [Fact]
            public void Adding_Item_Before_Selected_Item_Updates_Indexes()
            {
                var target = CreateTarget();
                var data = (AvaloniaList<string>)target.Source!;
                var selectionChangedRaised = 0;
                var indexesChangedraised = 0;

                target.SelectedIndex = 1;

                target.SelectionChanged += (s, e) => ++selectionChangedRaised;

                target.IndexesChanged += (s, e) =>
                {
                    Assert.Equal(0, e.StartIndex);
                    Assert.Equal(1, e.Delta);
                    ++indexesChangedraised;
                };

                data.Insert(0, "new");

                Assert.Equal(2, target.SelectedIndex);
                Assert.Equal(new[] { 2 }, target.SelectedIndexes);
                Assert.Equal("bar", target.SelectedItem);
                Assert.Equal(new[] { "bar" }, target.SelectedItems);
                Assert.Equal(2, target.AnchorIndex);
                Assert.Equal(1, indexesChangedraised);
                Assert.Equal(0, selectionChangedRaised);
            }

            [Fact]
            public void Adding_Item_After_Selected_Doesnt_Raise_Events()
            {
                var target = CreateTarget();
                var data = (AvaloniaList<string>)target.Source!;
                var raised = 0;

                target.SelectedIndex = 1;

                target.PropertyChanged += (s, e) => ++raised;
                target.SelectionChanged += (s, e) => ++raised;
                target.IndexesChanged += (s, e) => ++raised;

                data.Insert(2, "new");

                Assert.Equal(1, target.SelectedIndex);
                Assert.Equal(new[] { 1 }, target.SelectedIndexes);
                Assert.Equal("bar", target.SelectedItem);
                Assert.Equal(new[] { "bar" }, target.SelectedItems);
                Assert.Equal(1, target.AnchorIndex);
                Assert.Equal(0, raised);
            }

            [Fact]
            public void Adding_Item_At_Beginning_Of_SelectedRange_Updates_Indexes()
            {
                var target = CreateTarget();
                var data = (AvaloniaList<string>)target.Source!;
                var selectionChangedRaised = 0;
                var indexesChangedraised = 0;

                target.SelectRange(4, 8);

                target.SelectionChanged += (s, e) => ++selectionChangedRaised;

                target.IndexesChanged += (s, e) =>
                {
                    Assert.Equal(4, e.StartIndex);
                    Assert.Equal(2, e.Delta);
                    ++indexesChangedraised;
                };

                data.InsertRange(4, new[] { "frank", "tank" });

                Assert.Equal(6, target.SelectedIndex);
                Assert.Equal(new[] { 6, 7, 8, 9, 10 }, target.SelectedIndexes);
                Assert.Equal("quux", target.SelectedItem);
                Assert.Equal(new[] { "quux", "corge", "grault", "garply", "waldo" }, target.SelectedItems);
                Assert.Equal(6, target.AnchorIndex);
                Assert.Equal(1, indexesChangedraised);
                Assert.Equal(0, selectionChangedRaised);
            }

            [Fact]
            public void Adding_Item_At_End_Of_SelectedRange_Updates_Indexes()
            {
                var target = CreateTarget();
                var data = (AvaloniaList<string>)target.Source!;
                var selectionChangedRaised = 0;
                var indexesChangedraised = 0;

                target.SelectRange(4, 8);

                target.SelectionChanged += (s, e) => ++selectionChangedRaised;

                target.IndexesChanged += (s, e) =>
                {
                    Assert.Equal(8, e.StartIndex);
                    Assert.Equal(2, e.Delta);
                    ++indexesChangedraised;
                };

                data.InsertRange(8, new[] { "frank", "tank" });

                Assert.Equal(4, target.SelectedIndex);
                Assert.Equal(new[] { 4, 5, 6, 7, 10 }, target.SelectedIndexes);
                Assert.Equal("quux", target.SelectedItem);
                Assert.Equal(new[] { "quux", "corge", "grault", "garply", "waldo" }, target.SelectedItems);
                Assert.Equal(4, target.AnchorIndex);
                Assert.Equal(1, indexesChangedraised);
                Assert.Equal(0, selectionChangedRaised);
            }

            [Fact]
            public void Adding_Item_In_Middle_Of_SelectedRange_Updates_Indexes()
            {
                var target = CreateTarget();
                var data = (AvaloniaList<string>)target.Source!;
                var selectionChangedRaised = 0;
                var indexesChangedraised = 0;

                target.SelectRange(4, 8);

                target.SelectionChanged += (s, e) => ++selectionChangedRaised;

                target.IndexesChanged += (s, e) =>
                {
                    Assert.Equal(6, e.StartIndex);
                    Assert.Equal(2, e.Delta);
                    ++indexesChangedraised;
                };

                data.InsertRange(6, new[] { "frank", "tank" });

                Assert.Equal(4, target.SelectedIndex);
                Assert.Equal(new[] { 4, 5, 8, 9, 10 }, target.SelectedIndexes);
                Assert.Equal("quux", target.SelectedItem);
                Assert.Equal(new[] { "quux", "corge", "grault", "garply", "waldo" }, target.SelectedItems);
                Assert.Equal(4, target.AnchorIndex);
                Assert.Equal(1, indexesChangedraised);
                Assert.Equal(0, selectionChangedRaised);
            }

            [Fact]
            public void Removing_Selected_Item_Updates_State()
            {
                var target = CreateTarget();
                var data = (AvaloniaList<string>)target.Source!;
                var selectionChangedRaised = 0;
                var selectedIndexRaised = 0;

                target.Source = data;
                target.Select(1);

                target.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(target.SelectedIndex))
                    {
                        ++selectedIndexRaised;
                    }
                };

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Empty(e.DeselectedIndexes);
                    Assert.Equal(new[] { "bar" }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndexes);
                    Assert.Empty(e.SelectedItems);
                    ++selectionChangedRaised;
                };

                data.RemoveAt(1);

                Assert.Equal(-1, target.SelectedIndex);
                Assert.Empty(target.SelectedIndexes);
                Assert.Null(target.SelectedItem);
                Assert.Empty(target.SelectedItems);
                Assert.Equal(-1, target.AnchorIndex);
                Assert.Equal(1, selectionChangedRaised);
                Assert.Equal(1, selectedIndexRaised);
            }

            [Fact]
            public void Removing_Item_Before_Selected_Item_Updates_Indexes()
            {
                var target = CreateTarget();
                var data = (AvaloniaList<string>)target.Source!;
                var selectionChangedRaised = 0;
                var indexesChangedraised = 0;

                target.SelectedIndex = 1;

                target.SelectionChanged += (s, e) => ++selectionChangedRaised;

                target.IndexesChanged += (s, e) =>
                {
                    Assert.Equal(0, e.StartIndex);
                    Assert.Equal(-1, e.Delta);
                    ++indexesChangedraised;
                };

                data.RemoveAt(0);

                Assert.Equal(0, target.SelectedIndex);
                Assert.Equal(new[] { 0 }, target.SelectedIndexes);
                Assert.Equal("bar", target.SelectedItem);
                Assert.Equal(new[] { "bar" }, target.SelectedItems);
                Assert.Equal(0, target.AnchorIndex);
                Assert.Equal(1, indexesChangedraised);
                Assert.Equal(0, selectionChangedRaised);
            }

            [Fact]
            public void Removing_Item_After_Selected_Doesnt_Raise_Events()
            {
                var target = CreateTarget();
                var data = (AvaloniaList<string>)target.Source!;
                var raised = 0;

                target.SelectedIndex = 1;

                target.PropertyChanged += (s, e) => ++raised;
                target.SelectionChanged += (s, e) => ++raised;
                target.IndexesChanged += (s, e) => ++raised;

                data.RemoveAt(2);

                Assert.Equal(1, target.SelectedIndex);
                Assert.Equal(new[] { 1 }, target.SelectedIndexes);
                Assert.Equal("bar", target.SelectedItem);
                Assert.Equal(new[] { "bar" }, target.SelectedItems);
                Assert.Equal(1, target.AnchorIndex);
                Assert.Equal(0, raised);
            }

            [Fact]
            public void Removing_Selected_Range_Raises_Events()
            {
                var target = CreateTarget();
                var data = (AvaloniaList<string>)target.Source!;
                var selectionChangedRaised = 0;
                var selectedIndexRaised = 0;

                target.Source = data;
                target.SelectRange(4, 8);

                target.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(target.SelectedIndex))
                    {
                        ++selectedIndexRaised;
                    }
                };

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Empty(e.DeselectedIndexes);
                    Assert.Equal(new[] { "quux", "corge", "grault", "garply", "waldo" }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndexes);
                    Assert.Empty(e.SelectedItems);
                    ++selectionChangedRaised;
                };

                data.RemoveRange(4, 5);

                Assert.Equal(-1, target.SelectedIndex);
                Assert.Empty(target.SelectedIndexes);
                Assert.Null(target.SelectedItem);
                Assert.Empty(target.SelectedItems);
                Assert.Equal(-1, target.AnchorIndex);
                Assert.Equal(1, selectionChangedRaised);
                Assert.Equal(1, selectedIndexRaised);
            }

            [Fact]
            public void Removing_Partial_Selected_Range_Raises_Events_1()
            {
                var target = CreateTarget();
                var data = (AvaloniaList<string>)target.Source!;
                var selectionChangedRaised = 0;
                var selectedIndexRaised = 0;

                target.Source = data;
                target.SelectRange(4, 8);

                target.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(target.SelectedIndex))
                    {
                        ++selectedIndexRaised;
                    }
                };

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Empty(e.DeselectedIndexes);
                    Assert.Equal(new[] { "quux", "corge", "grault" }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndexes);
                    Assert.Empty(e.SelectedItems);
                    ++selectionChangedRaised;
                };

                data.RemoveRange(0, 7);

                Assert.Equal(0, target.SelectedIndex);
                Assert.Equal(new[] { 0, 1 }, target.SelectedIndexes);
                Assert.Equal("garply", target.SelectedItem);
                Assert.Equal(new[] { "garply", "waldo" }, target.SelectedItems);
                Assert.Equal(0, target.AnchorIndex);
                Assert.Equal(1, selectionChangedRaised);
                Assert.Equal(1, selectedIndexRaised);
            }

            [Fact]
            public void Removing_Partial_Selected_Range_Raises_Events_2()
            {
                var target = CreateTarget();
                var data = (AvaloniaList<string>)target.Source!;
                var selectionChangedRaised = 0;
                var selectedIndexRaised = 0;

                target.Source = data;
                target.SelectRange(4, 8);

                target.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(target.SelectedIndex))
                    {
                        ++selectedIndexRaised;
                    }
                };

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Empty(e.DeselectedIndexes);
                    Assert.Equal(new[] { "garply", "waldo" }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndexes);
                    Assert.Empty(e.SelectedItems);
                    ++selectionChangedRaised;
                };

                data.RemoveRange(7, 3);

                Assert.Equal(4, target.SelectedIndex);
                Assert.Equal(new[] { 4, 5, 6 }, target.SelectedIndexes);
                Assert.Equal("quux", target.SelectedItem);
                Assert.Equal(new[] { "quux", "corge", "grault" }, target.SelectedItems);
                Assert.Equal(4, target.AnchorIndex);
                Assert.Equal(1, selectionChangedRaised);
                Assert.Equal(0, selectedIndexRaised);
            }

            [Fact]
            public void Removing_Partial_Selected_Range_Raises_Events_3()
            {
                var target = CreateTarget();
                var data = (AvaloniaList<string>)target.Source!;
                var selectionChangedRaised = 0;
                var selectedIndexRaised = 0;

                target.Source = data;
                target.SelectRange(4, 8);

                target.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(target.SelectedIndex))
                    {
                        ++selectedIndexRaised;
                    }
                };

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Empty(e.DeselectedIndexes);
                    Assert.Equal(new[] { "corge", "grault", "garply" }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndexes);
                    Assert.Empty(e.SelectedItems);
                    ++selectionChangedRaised;
                };

                data.RemoveRange(5, 3);

                Assert.Equal(4, target.SelectedIndex);
                Assert.Equal(new[] { 4, 5 }, target.SelectedIndexes);
                Assert.Equal("quux", target.SelectedItem);
                Assert.Equal(new[] { "quux", "waldo" }, target.SelectedItems);
                Assert.Equal(4, target.AnchorIndex);
                Assert.Equal(1, selectionChangedRaised);
                Assert.Equal(0, selectedIndexRaised);
            }

            [Fact]
            public void Replacing_Selected_Item_Updates_State()
            {
                var target = CreateTarget();
                var data = (AvaloniaList<string>)target.Source!;
                var selectionChangedRaised = 0;
                var selectedIndexRaised = 0;
                var indexesChangedRaised = 0;

                target.Source = data;
                target.SelectRange(1, 4);

                target.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(target.SelectedIndex))
                    {
                        ++selectedIndexRaised;
                    }
                };

                target.IndexesChanged += (s, e) => ++indexesChangedRaised;

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Empty(e.DeselectedIndexes);
                    Assert.Equal(new[] { "bar" }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndexes);
                    Assert.Empty(e.SelectedItems);
                    ++selectionChangedRaised;
                };

                data[1] = "new";

                Assert.Equal(2, target.SelectedIndex);
                Assert.Equal(new[] { 2, 3, 4 }, target.SelectedIndexes);
                Assert.Equal("baz", target.SelectedItem);
                Assert.Equal(new[] { "baz", "qux", "quux" }, target.SelectedItems);
                Assert.Equal(2, target.AnchorIndex);
                Assert.Equal(1, selectionChangedRaised);
                Assert.Equal(1, selectedIndexRaised);
                Assert.Equal(0, indexesChangedRaised);
            }

            [Fact]
            public void Resetting_Source_Updates_State()
            {
                var target = CreateTarget();
                var data = (AvaloniaList<string>)target.Source!;
                var selectionChangedRaised = 0;
                var selectedIndexRaised = 0;
                var resetRaised = 0;

                target.Source = data;
                target.Select(1);

                target.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(target.SelectedIndex))
                    {
                        ++selectedIndexRaised;
                    }
                };

                target.SelectionChanged += (s, e) => ++selectionChangedRaised;
                target.SourceReset += (s, e) => ++resetRaised;

                data.Clear();

                Assert.Equal(-1, target.SelectedIndex);
                Assert.Empty(target.SelectedIndexes);
                Assert.Null(target.SelectedItem);
                Assert.Empty(target.SelectedItems);
                Assert.Equal(-1, target.AnchorIndex);
                Assert.Equal(0, selectionChangedRaised);
                Assert.Equal(1, resetRaised);
                Assert.Equal(1, selectedIndexRaised);
            }
        }

        public class BatchUpdate
        {
            [Fact]
            public void Correctly_Batches_Selects()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Empty(e.DeselectedIndexes);
                    Assert.Empty(e.DeselectedItems);
                    Assert.Equal(new[] { 2, 3 }, e.SelectedIndexes);
                    Assert.Equal(new[] { "baz", "qux" }, e.SelectedItems);
                    ++raised;
                };

                using (target.BatchUpdate())
                {
                    target.Select(2);
                    target.Select(3);
                }

                Assert.Equal(1, raised);
            }

            [Fact]
            public void Correctly_Batches_SelectRanges()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Empty(e.DeselectedIndexes);
                    Assert.Empty(e.DeselectedItems);
                    Assert.Equal(new[] { 2, 3, 5, 6 }, e.SelectedIndexes);
                    Assert.Equal(new[] { "baz", "qux", "corge", "grault" }, e.SelectedItems);
                    ++raised;
                };

                using (target.BatchUpdate())
                {
                    target.SelectRange(2, 3);
                    target.SelectRange(5, 6);
                }

                Assert.Equal(1, raised);
            }

            [Fact]
            public void Correctly_Batches_Select_Deselect()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Empty(e.DeselectedIndexes);
                    Assert.Empty(e.DeselectedItems);
                    Assert.Equal(new[] { 2, 3 }, e.SelectedIndexes);
                    Assert.Equal(new[] { "baz", "qux" }, e.SelectedItems);
                    ++raised;
                };

                using (target.BatchUpdate())
                {
                    target.Select(2);
                    target.Select(3);
                    target.Select(4);
                    target.Deselect(4);
                }

                Assert.Equal(1, raised);
            }

            [Fact]
            public void Correctly_Batches_Deselect_Select()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectRange(2, 8);

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Equal(new[] { 2, 3 }, e.DeselectedIndexes);
                    Assert.Equal(new[] { "baz", "qux" }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndexes);
                    Assert.Empty(e.SelectedItems);
                    ++raised;
                };

                using (target.BatchUpdate())
                {
                    target.Deselect(2);
                    target.Deselect(3);
                    target.Deselect(4);
                    target.Select(4);
                }

                Assert.Equal(1, raised);
            }

            [Fact]
            public void Correctly_Batches_Select_Deselect_Range()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Empty(e.DeselectedIndexes);
                    Assert.Empty(e.DeselectedItems);
                    Assert.Equal(new[] { 2, 3 }, e.SelectedIndexes);
                    Assert.Equal(new[] { "baz", "qux" }, e.SelectedItems);
                    ++raised;
                };

                using (target.BatchUpdate())
                {
                    target.SelectRange(2, 6);
                    target.DeselectRange(4, 8);
                }

                Assert.Equal(1, raised);
            }

            [Fact]
            public void Correctly_Batches_Deselect_Select_Range()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectRange(2, 8);

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Equal(new[] { 2, 3 }, e.DeselectedIndexes);
                    Assert.Equal(new[] { "baz", "qux" }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndexes);
                    Assert.Empty(e.SelectedItems);
                    ++raised;
                };

                using (target.BatchUpdate())
                {
                    target.DeselectRange(2, 6);
                    target.SelectRange(4, 8);
                }

                Assert.Equal(1, raised);
            }

            [Fact]
            public void Correctly_Batches_Clear_Select()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectRange(2, 3);

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Equal(new[] { 3 }, e.DeselectedIndexes);
                    Assert.Equal(new[] { "qux" }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndexes);
                    Assert.Empty(e.SelectedItems);
                    ++raised;
                };

                using (target.BatchUpdate())
                {
                    target.Clear();
                    target.Select(2);
                }

                Assert.Equal(1, raised);
            }

            [Fact]
            public void Correctly_Batches_Clear_SelectedIndex()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectRange(2, 3);

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Equal(new[] { 3 }, e.DeselectedIndexes);
                    Assert.Equal(new[] { "qux" }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndexes);
                    Assert.Empty(e.SelectedItems);
                    ++raised;
                };

                using (target.BatchUpdate())
                {
                    target.Clear();
                    target.SelectedIndex = 2;
                }

                Assert.Equal(1, raised);
            }
        }

        public class LostSelection
        {
            [Fact]
            public void LostSelection_Called_On_Clear()
            {
                var target = CreateTarget();
                var raised = 0;

                target.SelectedIndex = 1;

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Equal(new[] { 1 }, e.DeselectedIndexes);
                    Assert.Equal(new[] { "bar" }, e.DeselectedItems);
                    Assert.Equal(new[] { 0 }, e.SelectedIndexes);
                    Assert.Equal(new[] { "foo" }, e.SelectedItems);
                    ++raised;
                };

                target.LostSelection += (s, e) =>
                {
                    target.Select(0);
                };

                target.Clear();

                Assert.Equal(0, target.SelectedIndex);
                Assert.Equal(1, raised);
            }

            [Fact]
            public void LostSelection_Called_When_Selection_Removed()
            {
                var target = CreateTarget();
                var data = (AvaloniaList<string>)target.Source!;
                var raised = 0;

                target.SelectRange(1, 3);

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Empty(e.DeselectedIndexes);
                    Assert.Equal(new[] { "bar", "baz", "qux" }, e.DeselectedItems);
                    Assert.Equal(new[] { 0 }, e.SelectedIndexes);
                    Assert.Equal(new[] { "quux" }, e.SelectedItems);
                    ++raised;
                };

                target.LostSelection += (s, e) =>
                {
                    target.Select(0);
                };

                data.RemoveRange(0, 4);

                Assert.Equal(0, target.SelectedIndex);
                Assert.Equal(1, raised);
            }
        }

        public class SourceReset
        {
            [Fact]
            public void Can_Restore_Selection_In_SourceReset_Event()
            {
                var data = new ResettingList<string> { "foo", "bar", "baz" };
                var target = CreateTarget(createData: false);
                var sourceResetRaised = 0;
                var selectionChangedRaised = 0;

                target.Source = data;
                target.SelectedIndex = 1;

                target.SourceReset += (s, e) =>
                {
                    target.SelectedIndex = data.IndexOf("bar");
                    ++sourceResetRaised;
                };

                target.SelectionChanged += (s, e) =>
                {
                    Assert.Empty(e.DeselectedIndexes);
                    Assert.Empty(e.DeselectedItems);
                    Assert.Equal(new[] { 3 }, e.SelectedIndexes);
                    Assert.Equal(new[] { "bar" }, e.SelectedItems);
                    ++selectionChangedRaised;
                };

                data.Reset(new[] { "qux", "foo", "quux", "bar", "baz" });

                Assert.Equal(3, target.SelectedIndex);
                Assert.Equal(1, selectionChangedRaised);
                Assert.Equal(1, sourceResetRaised);
            }
        }

        private static SelectionModel<string?> CreateTarget(bool createData = true)
        {
            var result = new SelectionModel<string?> { SingleSelect = false };

            if (createData)
            {
                result.Source = new AvaloniaList<string>
                {
                    "foo",
                    "bar",
                    "baz",
                    "qux",
                    "quux",
                    "corge",
                    "grault",
                    "garply",
                    "waldo",
                    "fred",
                    "plugh",
                    "xyzzy",
                    "thud"
                };
            }

            return result;
        }

        private class ResettingList<T> : List<T>, INotifyCollectionChanged
        {
            public event NotifyCollectionChangedEventHandler? CollectionChanged;

            public void Reset(IEnumerable<T>? items = null)
            {
                if (items != null)
                {
                    Clear();
                    AddRange(items);
                }

                CollectionChanged?.Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

    }
}
