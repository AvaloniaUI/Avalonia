﻿// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Collections;
using Avalonia.Diagnostics;
using ReactiveUI;
using Xunit;
using Xunit.Abstractions;

namespace Avalonia.Controls.UnitTests
{
    public class SelectionModelTests
    {
        private readonly ITestOutputHelper _output;

        public SelectionModelTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ValidateOneLevelSingleSelectionNoSource()
        {
            SelectionModel selectionModel = new SelectionModel() { SingleSelect = true };
            _output.WriteLine("No source set.");
            Select(selectionModel, 4, true);
            ValidateSelection(selectionModel, Path(4));
            Select(selectionModel, 4, false);
            ValidateSelection(selectionModel);
        }

        [Fact]
        public void ValidateOneLevelSingleSelection()
        {
            SelectionModel selectionModel = new SelectionModel() { SingleSelect = true };
            _output.WriteLine("Set the source to 10 items");
            selectionModel.Source = Enumerable.Range(0, 10).ToList();
            Select(selectionModel, 3, true);
            ValidateSelection(selectionModel, Path(3));
            Select(selectionModel, 3, false);
            ValidateSelection(selectionModel);
        }

        [Fact]
        public void ValidateSelectionChangedEvent()
        {
            SelectionModel selectionModel = new SelectionModel();
            selectionModel.Source = Enumerable.Range(0, 10).ToList();

            int selectionChangedFiredCount = 0;
            selectionModel.SelectionChanged += delegate (object sender, SelectionModelSelectionChangedEventArgs args)
            {
                selectionChangedFiredCount++;
                ValidateSelection(selectionModel, Path(4));
            };

            Select(selectionModel, 4, true);
            ValidateSelection(selectionModel, Path(4));
            Assert.Equal(1, selectionChangedFiredCount);
        }

        [Fact]
        public void ValidateCanSetSelectedIndex()
        {
            var model = new SelectionModel();
            var ip = IndexPath.CreateFrom(34);
            model.SelectedIndex = ip;
            Assert.Equal(0, ip.CompareTo(model.SelectedIndex));
        }

        [Fact]
        public void ValidateOneLevelMultipleSelection()
        {
            SelectionModel selectionModel = new SelectionModel();
            selectionModel.Source = Enumerable.Range(0, 10).ToList();

            Select(selectionModel, 4, true);
            ValidateSelection(selectionModel, Path(4));
            SelectRangeFromAnchor(selectionModel, 8, true /* select */);
            ValidateSelection(selectionModel,
                Path(4),
                Path(5),
                Path(6),
                Path(7),
                Path(8));

            ClearSelection(selectionModel);
            SetAnchorIndex(selectionModel, 6);
            SelectRangeFromAnchor(selectionModel, 3, true /* select */);
            ValidateSelection(selectionModel,
                Path(3),
                Path(4),
                Path(5),
                Path(6));

            SetAnchorIndex(selectionModel, 4);
            SelectRangeFromAnchor(selectionModel, 5, false /* select */);
            ValidateSelection(selectionModel,
                Path(3),
                Path(6));
        }

        [Fact]
        public void ValidateTwoLevelSingleSelection()
        {
            SelectionModel selectionModel = new SelectionModel();
            _output.WriteLine("Setting the source");
            selectionModel.Source = CreateNestedData(1 /* levels */ , 2 /* groupsAtLevel */, 2 /* countAtLeaf */);
            Select(selectionModel, 1, 1, true);
            ValidateSelection(selectionModel, Path(1, 1));
            Select(selectionModel, 1, 1, false);
            ValidateSelection(selectionModel);
        }

        [Fact]
        public void ValidateTwoLevelMultipleSelection()
        {
            SelectionModel selectionModel = new SelectionModel();
            _output.WriteLine("Setting the source");
            selectionModel.Source = CreateNestedData(1 /* levels */ , 3 /* groupsAtLevel */, 3 /* countAtLeaf */);

            Select(selectionModel, 1, 2, true);
            ValidateSelection(selectionModel, Path(1, 2));
            SelectRangeFromAnchor(selectionModel, 2, 2, true /* select */);
            ValidateSelection(selectionModel,
                Path(1, 2),
                Path(2, 0),
                Path(2, 1),
                Path(2, 2));

            ClearSelection(selectionModel);
            SetAnchorIndex(selectionModel, 2, 1);
            SelectRangeFromAnchor(selectionModel, 0, 1, true /* select */);
            ValidateSelection(selectionModel,
                Path(0, 1),
                Path(0, 2),
                Path(1, 0),
                Path(1, 1),
                Path(1, 2),
                Path(2, 0),
                Path(2, 1));

            SetAnchorIndex(selectionModel, 1, 1);
            SelectRangeFromAnchor(selectionModel, 2, 0, false /* select */);
            ValidateSelection(selectionModel,
                Path(0, 1),
                Path(0, 2),
                Path(1, 0),
                Path(2, 1));

            ClearSelection(selectionModel);
            ValidateSelection(selectionModel);
        }

        [Fact]
        public void ValidateNestedSingleSelection()
        {
            SelectionModel selectionModel = new SelectionModel() { SingleSelect = true };
            _output.WriteLine("Setting the source");
            selectionModel.Source = CreateNestedData(3 /* levels */ , 2 /* groupsAtLevel */, 2 /* countAtLeaf */);
            var path = Path(1, 0, 1, 1);
            Select(selectionModel, path, true);
            ValidateSelection(selectionModel, path);
            Select(selectionModel, Path(0, 0, 1, 0), true);
            ValidateSelection(selectionModel, Path(0, 0, 1, 0));
            Select(selectionModel, Path(0, 0, 1, 0), false);
            ValidateSelection(selectionModel);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ValidateNestedMultipleSelection(bool handleChildrenRequested)
        {
            SelectionModel selectionModel = new SelectionModel();
            List<IndexPath> sourcePaths = new List<IndexPath>();

            _output.WriteLine("Setting the source");
            selectionModel.Source = CreateNestedData(3 /* levels */ , 2 /* groupsAtLevel */, 4 /* countAtLeaf */);
            if (handleChildrenRequested)
            {
                selectionModel.ChildrenRequested += (object sender, SelectionModelChildrenRequestedEventArgs args) =>
                {
                    _output.WriteLine("ChildrenRequestedIndexPath:" + args.SourceIndex);
                    sourcePaths.Add(args.SourceIndex);
                    args.Children = Observable.Return(args.Source as IEnumerable);
                };
            }

            var startPath = Path(1, 0, 1, 0);
            Select(selectionModel, startPath, true);
            ValidateSelection(selectionModel, startPath);

            var endPath = Path(1, 1, 1, 0);
            SelectRangeFromAnchor(selectionModel, endPath, true /* select */);

            if (handleChildrenRequested)
            {
                // Validate SourceIndices.
                var expectedSourceIndices = new List<IndexPath>()
                {
                    Path(1),
                    Path(1, 0),
                    Path(1, 0, 1),
                    Path(1, 1),
                    Path(1, 0, 1, 3),
                    Path(1, 0, 1, 2),
                    Path(1, 0, 1, 1),
                    Path(1, 0, 1, 0),
                    Path(1, 1, 1),
                    Path(1, 1, 0),
                    Path(1, 1, 0, 3),
                    Path(1, 1, 0, 2),
                    Path(1, 1, 0, 1),
                    Path(1, 1, 0, 0),
                    Path(1, 1, 1, 0)
                };

                Assert.Equal(expectedSourceIndices.Count, sourcePaths.Count);
                for (int i = 0; i < expectedSourceIndices.Count; i++)
                {
                    Assert.True(AreEqual(expectedSourceIndices[i], sourcePaths[i]));
                }
            }

            ValidateSelection(selectionModel,
                Path(1, 1),
                Path(1, 0, 1, 0),
                Path(1, 0, 1, 1),
                Path(1, 0, 1, 2),
                Path(1, 0, 1, 3),
                Path(1, 1, 0),
                Path(1, 1, 1),
                Path(1, 1, 0, 0),
                Path(1, 1, 0, 1),
                Path(1, 1, 0, 2),
                Path(1, 1, 0, 3),
                Path(1, 1, 1, 0));

            ClearSelection(selectionModel);
            ValidateSelection(selectionModel);

            startPath = Path(0, 1, 0, 2);
            SetAnchorIndex(selectionModel, startPath);
            endPath = Path(0, 0, 0, 2);
            SelectRangeFromAnchor(selectionModel, endPath, true /* select */);
            ValidateSelection(selectionModel,
                Path(0, 1),
                Path(0, 0, 1),
                Path(0, 0, 0, 2),
                Path(0, 0, 0, 3),
                Path(0, 0, 1, 0),
                Path(0, 0, 1, 1),
                Path(0, 0, 1, 2),
                Path(0, 0, 1, 3),
                Path(0, 1, 0),
                Path(0, 1, 0, 0),
                Path(0, 1, 0, 1),
                Path(0, 1, 0, 2));

            startPath = Path(0, 1, 0, 2);
            SetAnchorIndex(selectionModel, startPath);
            endPath = Path(0, 0, 0, 2);
            SelectRangeFromAnchor(selectionModel, endPath, false /* select */);
            ValidateSelection(selectionModel);
        }

        [Fact]
        public void ValidateInserts()
        {
            var data = new ObservableCollection<int>(Enumerable.Range(0, 10));
            var selectionModel = new SelectionModel();
            selectionModel.Source = data;

            selectionModel.Select(3);
            selectionModel.Select(4);
            selectionModel.Select(5);
            ValidateSelection(selectionModel,
                Path(3),
                Path(4),
                Path(5));

            _output.WriteLine("Insert in selected range: Inserting 3 items at index 4");
            data.Insert(4, 41);
            data.Insert(4, 42);
            data.Insert(4, 43);
            ValidateSelection(selectionModel,
                Path(3),
                Path(7),
                Path(8));

            _output.WriteLine("Insert before selected range: Inserting 3 items at index 0");
            data.Insert(0, 100);
            data.Insert(0, 101);
            data.Insert(0, 102);
            ValidateSelection(selectionModel,
                Path(6),
                Path(10),
                Path(11));

            _output.WriteLine("Insert after selected range: Inserting 3 items at index 12");
            data.Insert(12, 1000);
            data.Insert(12, 1001);
            data.Insert(12, 1002);
            ValidateSelection(selectionModel,
                Path(6),
                Path(10),
                Path(11));
        }

        [Fact]
        public void ValidateGroupInserts()
        {
            var data = CreateNestedData(1 /* levels */ , 3 /* groupsAtLevel */, 3 /* countAtLeaf */);
            var selectionModel = new SelectionModel();
            selectionModel.Source = data;

            selectionModel.Select(1, 1);
            ValidateSelection(selectionModel, Path(1, 1));

            _output.WriteLine("Insert before selected range: Inserting item at group index 0");
            data.Insert(0, 100);
            ValidateSelection(selectionModel, Path(2, 1));

            _output.WriteLine("Insert after selected range: Inserting item at group index 3");
            data.Insert(3, 1000);
            ValidateSelection(selectionModel, Path(2, 1));
        }

        [Fact]
        public void ValidateRemoves()
        {
            var data = new ObservableCollection<int>(Enumerable.Range(0, 10));
            var selectionModel = new SelectionModel();
            selectionModel.Source = data;

            selectionModel.Select(6);
            selectionModel.Select(7);
            selectionModel.Select(8);
            ValidateSelection(selectionModel,
                Path(6),
                Path(7),
                Path(8));

            _output.WriteLine("Remove before selected range: Removing item at index 0");
            data.RemoveAt(0);
            ValidateSelection(selectionModel,
                Path(5),
                Path(6),
                Path(7));

            _output.WriteLine("Remove from before to middle of selected range: Removing items at index 3, 4, 5");
            data.RemoveAt(3);
            data.RemoveAt(3);
            data.RemoveAt(3);
            ValidateSelection(selectionModel, Path(3), Path(4));

            _output.WriteLine("Remove after selected range: Removing item at index 5");
            data.RemoveAt(5);
            ValidateSelection(selectionModel, Path(3), Path(4));
        }

        [Fact]
        public void ValidateGroupRemoves()
        {
            var data = CreateNestedData(1 /* levels */ , 3 /* groupsAtLevel */, 3 /* countAtLeaf */);
            var selectionModel = new SelectionModel();
            selectionModel.Source = data;

            selectionModel.Select(1, 1);
            selectionModel.Select(1, 2);
            ValidateSelection(selectionModel, Path(1, 1), Path(1, 2));

            _output.WriteLine("Remove before selected range: Removing item at group index 0");
            data.RemoveAt(0);
            ValidateSelection(selectionModel, Path(0, 1), Path(0, 2));

            _output.WriteLine("Remove after selected range: Removing item at group index 1");
            data.RemoveAt(1);
            ValidateSelection(selectionModel, Path(0, 1), Path(0, 2));

            _output.WriteLine("Remove group containing selected items");

            var raised = 0;

            selectionModel.SelectionChanged += (s, e) => 
            {
                Assert.Empty(e.DeselectedIndices);
                Assert.Equal(new object[] { 4, 5, }, e.DeselectedItems);
                Assert.Empty(e.SelectedIndices);
                Assert.Empty(e.SelectedItems);
                ++raised;
            };

            data.RemoveAt(0);
            ValidateSelection(selectionModel);
            Assert.Equal(1, raised);
        }

        [Fact]
        public void CanReplaceItem()
        {
            var data = new ObservableCollection<int>(Enumerable.Range(0, 10));
            var selectionModel = new SelectionModel();
            selectionModel.Source = data;

            selectionModel.Select(3);
            selectionModel.Select(4);
            selectionModel.Select(5);
            ValidateSelection(selectionModel, Path(3), Path(4), Path(5));

            data[3] = 300;
            data[4] = 400;
            ValidateSelection(selectionModel, Path(5));
        }

        [Fact]
        public void ValidateGroupReplaceLosesSelection()
        {
            var data = CreateNestedData(1 /* levels */ , 3 /* groupsAtLevel */, 3 /* countAtLeaf */);
            var selectionModel = new SelectionModel();
            selectionModel.Source = data;

            selectionModel.Select(1, 1);
            ValidateSelection(selectionModel, Path(1, 1));

            data[1] = new ObservableCollection<int>(Enumerable.Range(0, 5));
            ValidateSelection(selectionModel);
        }

        [Fact]
        public void ValidateClear()
        {
            var data = new ObservableCollection<int>(Enumerable.Range(0, 10));
            var selectionModel = new SelectionModel();
            selectionModel.Source = data;

            selectionModel.Select(3);
            selectionModel.Select(4);
            selectionModel.Select(5);
            ValidateSelection(selectionModel, Path(3), Path(4), Path(5));

            data.Clear();
            ValidateSelection(selectionModel);
        }

        [Fact]
        public void ValidateGroupClear()
        {
            var data = CreateNestedData(1 /* levels */ , 3 /* groupsAtLevel */, 3 /* countAtLeaf */);
            var selectionModel = new SelectionModel();
            selectionModel.Source = data;

            selectionModel.Select(1, 1);
            ValidateSelection(selectionModel, Path(1, 1));

            (data[1] as IList).Clear();
            ValidateSelection(selectionModel);
        }

        // In some cases the leaf node might get a collection change that affects an ancestors selection
        // state. In this case we were not raising selection changed event. For example, if all elements 
        // in a group are selected and a new item gets inserted - the parent goes from selected to partially 
        // selected. In that case we need to raise the selection changed event so that the header containers 
        // can show the correct visual.
        [Fact]
        public void ValidateEventWhenInnerNodeChangesSelectionState()
        {
            bool selectionChangedRaised = false;
            var data = CreateNestedData(1 /* levels */ , 3 /* groupsAtLevel */, 3 /* countAtLeaf */);
            var selectionModel = new SelectionModel();
            selectionModel.Source = data;
            selectionModel.SelectionChanged += (sender, args) => { selectionChangedRaised = true; };

            selectionModel.Select(1, 0);
            selectionModel.Select(1, 1);
            selectionModel.Select(1, 2);
            ValidateSelection(selectionModel, Path(1, 0), Path(1, 1), Path(1, 2));

            _output.WriteLine("Inserting 1.0");
            selectionChangedRaised = false;
            (data[1] as AvaloniaList<object>).Insert(0, 100);
            Assert.True(selectionChangedRaised, "SelectionChanged event was not raised");
            ValidateSelection(selectionModel, Path(1, 1), Path(1, 2), Path(1, 3));

            _output.WriteLine("Removing 1.0");
            selectionChangedRaised = false;
            (data[1] as AvaloniaList<object>).RemoveAt(0);
            Assert.True(selectionChangedRaised, "SelectionChanged event was not raised");
            ValidateSelection(selectionModel,
                Path(1, 0),
                Path(1, 1),
                Path(1, 2));
        }

        [Fact]
        public void ValidatePropertyChangedEventIsRaised()
        {
            var selectionModel = new SelectionModel();
            _output.WriteLine("Set the source to 10 items");
            selectionModel.Source = Enumerable.Range(0, 10).ToList();

            bool selectedIndexChanged = false;
            bool selectedIndicesChanged = false;
            bool SelectedItemChanged = false;
            bool SelectedItemsChanged = false;
            bool AnchorIndexChanged = false;
            selectionModel.PropertyChanged += (sender, args) =>
            {
                switch (args.PropertyName)
                {
                    case "SelectedIndex":
                        selectedIndexChanged = true;
                        break;
                    case "SelectedIndices":
                        selectedIndicesChanged = true;
                        break;
                    case "SelectedItem":
                        SelectedItemChanged = true;
                        break;
                    case "SelectedItems":
                        SelectedItemsChanged = true;
                        break;
                    case "AnchorIndex":
                        AnchorIndexChanged = true;
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            };

            Select(selectionModel, 3, true);

            Assert.True(selectedIndexChanged);
            Assert.True(selectedIndicesChanged);
            Assert.True(SelectedItemChanged);
            Assert.True(SelectedItemsChanged);
            Assert.True(AnchorIndexChanged);
        }

        [Fact]
        public void CanExtendSelectionModelINPC()
        {
            var selectionModel = new CustomSelectionModel();
            bool intPropertyChanged = false;
            selectionModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == "IntProperty")
                {
                    intPropertyChanged = true;
                }
            };

            selectionModel.IntProperty = 5;
            Assert.True(intPropertyChanged);
        }

        [Fact]
        public void SelectRangeRegressionTest()
        {
            var selectionModel = new SelectionModel()
            {
                Source = CreateNestedData(1, 2, 3)
            };

            // length of start smaller than end used to cause an out of range error.
            selectionModel.SelectRange(IndexPath.CreateFrom(0), IndexPath.CreateFrom(1, 1));

            ValidateSelection(selectionModel,
                Path(0),
                Path(1),
                Path(0, 0),
                Path(0, 1),
                Path(0, 2),
                Path(1, 0),
                Path(1, 1));
        }

        [Fact]
        public void SelectRange_Should_Select_Nested_Items_On_Different_Levels()
        {
            var target = new SelectionModel();
            var data = CreateNestedData(1, 2, 3);

            target.Source = data;
            target.AnchorIndex = new IndexPath(0, 1);
            target.SelectRange(Path(0, 1), Path(1));

            Assert.Equal(
                new[]
                {
                    Path(1),
                    Path(0, 1),
                    Path(0, 2),
                },
                target.SelectedIndices);
        }

        [Fact]
        public void Should_Listen_For_Changes_After_Deselect()
        {
            var target = new SelectionModel();
            var data = CreateNestedData(1, 2, 3);

            target.Source = data;
            target.Select(1, 0);
            target.Deselect(1, 0);
            target.Select(1, 0);
            ((AvaloniaList<object>)data[1]).Insert(0, "foo");

            Assert.Equal(new IndexPath(1, 1), target.SelectedIndex);
        }

        [Fact]
        public void Selecting_Item_Raises_SelectionChanged()
        {
            var target = new SelectionModel();
            var raised = 0;

            target.Source = Enumerable.Range(0, 10).ToList();
            target.SelectionChanged += (s, e) =>
            {
                Assert.Empty(e.DeselectedIndices);
                Assert.Empty(e.DeselectedItems);
                Assert.Equal(new[] { new IndexPath(4) }, e.SelectedIndices);
                Assert.Equal(new object[] { 4 }, e.SelectedItems);
                ++raised;
            };

            target.Select(4);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Selecting_Already_Selected_Item_Doesnt_Raise_SelectionChanged()
        {
            var target = new SelectionModel();
            var raised = 0;

            target.Source = Enumerable.Range(0, 10).ToList();
            target.Select(4);
            target.SelectionChanged += (s, e) => ++raised;
            target.Select(4);

            Assert.Equal(0, raised);
        }

        [Fact]
        public void SingleSelecting_Item_Raises_SelectionChanged()
        {
            var target = new SelectionModel { SingleSelect = true };
            var raised = 0;

            target.Source = Enumerable.Range(0, 10).ToList();
            target.Select(3);

            target.SelectionChanged += (s, e) =>
            {
                Assert.Equal(new[] { new IndexPath(3) }, e.DeselectedIndices);
                Assert.Equal(new object[] { 3 }, e.DeselectedItems);
                Assert.Equal(new[] { new IndexPath(4) }, e.SelectedIndices);
                Assert.Equal(new object[] { 4 }, e.SelectedItems);
                ++raised;
            };

            target.Select(4);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void SingleSelecting_Already_Selected_Item_Doesnt_Raise_SelectionChanged()
        {
            var target = new SelectionModel { SingleSelect = true };
            var raised = 0;

            target.Source = Enumerable.Range(0, 10).ToList();
            target.Select(4);
            target.SelectionChanged += (s, e) => ++raised;
            target.Select(4);

            Assert.Equal(0, raised);
        }

        [Fact]
        public void Selecting_Item_With_Group_Raises_SelectionChanged()
        {
            var target = new SelectionModel();
            var raised = 0;

            target.Source = CreateNestedData(1, 2, 3);
            target.SelectionChanged += (s, e) =>
            {
                Assert.Empty(e.DeselectedIndices);
                Assert.Empty(e.DeselectedItems);
                Assert.Equal(new[] { new IndexPath(1, 1) }, e.SelectedIndices);
                Assert.Equal(new object[] { 4 }, e.SelectedItems);
                ++raised;
            };

            target.Select(1, 1);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void SelectAt_Raises_SelectionChanged()
        {
            var target = new SelectionModel();
            var raised = 0;

            target.Source = CreateNestedData(1, 2, 3);
            target.SelectionChanged += (s, e) =>
            {
                Assert.Empty(e.DeselectedIndices);
                Assert.Empty(e.DeselectedItems);
                Assert.Equal(new[] { new IndexPath(1, 1) }, e.SelectedIndices);
                Assert.Equal(new object[] { 4 }, e.SelectedItems);
                ++raised;
            };

            target.SelectAt(new IndexPath(1, 1));

            Assert.Equal(1, raised);
        }

        [Fact]
        public void SelectAll_Raises_SelectionChanged()
        {
            var target = new SelectionModel { SingleSelect = true };
            var raised = 0;

            target.Source = Enumerable.Range(0, 10).ToList();
            target.SelectionChanged += (s, e) =>
            {
                var expected = Enumerable.Range(0, 10);
                Assert.Empty(e.DeselectedIndices);
                Assert.Empty(e.DeselectedItems);
                Assert.Equal(expected.Select(x => new IndexPath(x)), e.SelectedIndices);
                Assert.Equal(expected, e.SelectedItems.Cast<int>());
                ++raised;
            };

            target.SelectAll();

            Assert.Equal(1, raised);
        }

        [Fact]
        public void SelectAll_With_Already_Selected_Items_Raises_SelectionChanged()
        {
            var target = new SelectionModel { SingleSelect = true };
            var raised = 0;

            target.Source = Enumerable.Range(0, 10).ToList();
            target.Select(4);

            target.SelectionChanged += (s, e) =>
            {
                var expected = Enumerable.Range(0, 10).Except(new[] { 4 });
                Assert.Empty(e.DeselectedIndices);
                Assert.Empty(e.DeselectedItems);
                Assert.Equal(expected.Select(x => new IndexPath(x)), e.SelectedIndices);
                Assert.Equal(expected, e.SelectedItems.Cast<int>());
                ++raised;
            };

            target.SelectAll();

            Assert.Equal(1, raised);
        }

        [Fact]
        public void SelectRangeFromAnchor_Raises_SelectionChanged()
        {
            var target = new SelectionModel();
            var raised = 0;

            target.Source = Enumerable.Range(0, 10).ToList();
            target.SelectionChanged += (s, e) =>
            {
                var expected = Enumerable.Range(4, 3);
                Assert.Empty(e.DeselectedIndices);
                Assert.Empty(e.DeselectedItems);
                Assert.Equal(expected.Select(x => new IndexPath(x)), e.SelectedIndices);
                Assert.Equal(expected, e.SelectedItems.Cast<int>());
                ++raised;
            };

            target.AnchorIndex = new IndexPath(4);
            target.SelectRangeFromAnchor(6);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void SelectRangeFromAnchor_With_Group_Raises_SelectionChanged()
        {
            var target = new SelectionModel();
            var raised = 0;

            target.Source = CreateNestedData(1, 2, 10);
            target.SelectionChanged += (s, e) =>
            {
                var expected = Enumerable.Range(11, 6);
                Assert.Empty(e.DeselectedIndices);
                Assert.Empty(e.DeselectedItems);
                Assert.Equal(expected.Select(x => new IndexPath(x / 10, x % 10)), e.SelectedIndices);
                Assert.Equal(expected, e.SelectedItems.Cast<int>());
                ++raised;
            };

            target.AnchorIndex = new IndexPath(1, 1);
            target.SelectRangeFromAnchor(1, 6);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void SelectRangeFromAnchorTo_Raises_SelectionChanged()
        {
            var target = new SelectionModel();
            var raised = 0;

            target.Source = CreateNestedData(1, 2, 10);
            target.SelectionChanged += (s, e) =>
            {
                var expected = Enumerable.Range(11, 6);
                Assert.Empty(e.DeselectedIndices);
                Assert.Empty(e.DeselectedItems);
                Assert.Equal(expected.Select(x => new IndexPath(x / 10, x % 10)), e.SelectedIndices);
                Assert.Equal(expected, e.SelectedItems.Cast<int>());
                ++raised;
            };

            target.AnchorIndex = new IndexPath(1, 1);
            target.SelectRangeFromAnchorTo(new IndexPath(1, 6));

            Assert.Equal(1, raised);
        }

        [Fact]
        public void ClearSelection_Raises_SelectionChanged()
        {
            var target = new SelectionModel();
            var raised = 0;

            target.Source = Enumerable.Range(0, 10).ToList();
            target.Select(4);
            target.Select(5);

            target.SelectionChanged += (s, e) =>
            {
                var expected = Enumerable.Range(4, 2);
                Assert.Equal(expected.Select(x => new IndexPath(x)), e.DeselectedIndices);
                Assert.Equal(expected, e.DeselectedItems.Cast<int>());
                Assert.Empty(e.SelectedIndices);
                Assert.Empty(e.SelectedItems);
                ++raised;
            };

            target.ClearSelection();

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Clearing_Nested_Selection_Raises_SelectionChanged()
        {
            var target = new SelectionModel();
            var raised = 0;

            target.Source = CreateNestedData(1, 2, 3);
            target.Select(1, 1);

            target.SelectionChanged += (s, e) =>
            {
                Assert.Equal(new[] { new IndexPath(1, 1) }, e.DeselectedIndices);
                Assert.Equal(new object[] { 4 }, e.DeselectedItems);
                Assert.Empty(e.SelectedIndices);
                Assert.Empty(e.SelectedItems);
                ++raised;
            };

            target.ClearSelection();

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Changing_Source_Raises_SelectionChanged()
        {
            var target = new SelectionModel();
            var raised = 0;

            target.Source = Enumerable.Range(0, 10).ToList();
            target.Select(4);
            target.Select(5);

            target.SelectionChanged += (s, e) =>
            {
                var expected = Enumerable.Range(4, 2);
                Assert.Equal(expected.Select(x => new IndexPath(x)), e.DeselectedIndices);
                Assert.Equal(expected, e.DeselectedItems.Cast<int>());
                Assert.Empty(e.SelectedIndices);
                Assert.Empty(e.SelectedItems);
                ++raised;
            };

            target.Source = Enumerable.Range(20, 10).ToList();

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Setting_SelectedIndex_Raises_SelectionChanged()
        {
            var target = new SelectionModel();
            var raised = 0;

            target.Source = Enumerable.Range(0, 10).ToList();
            target.Select(4);
            target.Select(5);

            target.SelectionChanged += (s, e) =>
            {
                Assert.Equal(new[] { new IndexPath(4), new IndexPath(5) }, e.DeselectedIndices);
                Assert.Equal(new object[] { 4, 5 }, e.DeselectedItems);
                Assert.Equal(new[] { new IndexPath(6) }, e.SelectedIndices);
                Assert.Equal(new object[] { 6 }, e.SelectedItems);
                ++raised;
            };

            target.SelectedIndex = new IndexPath(6);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Removing_Selected_Item_Raises_SelectionChanged()
        {
            var target = new SelectionModel();
            var data = new ObservableCollection<int>(Enumerable.Range(0, 10));
            var raised = 0;

            target.Source = data;
            target.Select(4);
            target.Select(5);

            target.SelectionChanged += (s, e) =>
            {
                Assert.Empty(e.DeselectedIndices);
                Assert.Equal(new object[] { 4 }, e.DeselectedItems);
                Assert.Empty(e.SelectedIndices);
                Assert.Empty(e.SelectedItems);
                ++raised;
            };

            data.Remove(4);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Removing_Selected_Child_Item_Raises_SelectionChanged()
        {
            var target = new SelectionModel();
            var data = CreateNestedData(1, 2, 3);
            var raised = 0;

            target.Source = data;
            target.SelectRange(new IndexPath(0), new IndexPath(1, 1));

            target.SelectionChanged += (s, e) =>
            {
                Assert.Empty(e.DeselectedIndices);
                Assert.Equal(new object[] { 1}, e.DeselectedItems);
                Assert.Empty(e.SelectedIndices);
                Assert.Empty(e.SelectedItems);
                ++raised;
            };

            ((AvaloniaList<object>)data[0]).RemoveAt(1);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Removing_Selected_Item_With_Children_Raises_SelectionChanged()
        {
            var target = new SelectionModel();
            var data = CreateNestedData(1, 2, 3);
            var raised = 0;

            target.Source = data;
            target.SelectRange(new IndexPath(0), new IndexPath(1, 1));

            target.SelectionChanged += (s, e) =>
            {
                Assert.Empty(e.DeselectedIndices);
                Assert.Equal(new object[] { new AvaloniaList<int> { 0, 1, 2 }, 0, 1, 2 }, e.DeselectedItems);
                Assert.Empty(e.SelectedIndices);
                Assert.Empty(e.SelectedItems);
                ++raised;
            };

            data.RemoveAt(0);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Removing_Unselected_Item_Before_Selected_Item_Raises_SelectionChanged()
        {
            var target = new SelectionModel();
            var data = new ObservableCollection<int>(Enumerable.Range(0, 10));
            var raised = 0;

            target.Source = data;
            target.Select(8);

            target.SelectionChanged += (s, e) =>
            {
                Assert.Empty(e.DeselectedIndices);
                Assert.Empty(e.DeselectedItems);
                Assert.Empty(e.SelectedIndices);
                Assert.Empty(e.SelectedItems);
                ++raised;
            };

            data.Remove(6);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Removing_Unselected_Item_After_Selected_Item_Doesnt_Raise_SelectionChanged()
        {
            var target = new SelectionModel();
            var data = new ObservableCollection<int>(Enumerable.Range(0, 10));
            var raised = 0;

            target.Source = data;
            target.Select(4);

            target.SelectionChanged += (s, e) => ++raised;

            data.Remove(6);

            Assert.Equal(0, raised); 
        }

        [Fact]
        public void Disposing_Unhooks_CollectionChanged_Handlers()
        {
            var data = CreateNestedData(2, 2, 2);
            var target = new SelectionModel { Source = data };

            target.SelectAll();
            VerifyCollectionChangedHandlers(1, data);

            target.Dispose();

            VerifyCollectionChangedHandlers(0, data);
        }

        [Fact]
        public void Clearing_Selection_Unhooks_CollectionChanged_Handlers()
        {
            var data = CreateNestedData(2, 2, 2);
            var target = new SelectionModel { Source = data };

            target.SelectAll();
            VerifyCollectionChangedHandlers(1, data);

            target.ClearSelection();

            // Root subscription not unhooked until SelectionModel is disposed.
            Assert.Equal(1, GetSubscriberCount(data));

            foreach (AvaloniaList<object> i in data)
            {
                VerifyCollectionChangedHandlers(0, i);
            }
        }

        [Fact]
        public void Removing_Item_Unhooks_CollectionChanged_Handlers()
        {
            var data = CreateNestedData(2, 2, 2);
            var target = new SelectionModel { Source = data };

            target.SelectAll();

            var toRemove = (AvaloniaList<object>)data[1];
            data.Remove(toRemove);

            Assert.Equal(0, GetSubscriberCount(toRemove));
        }

        [Fact]
        public void SelectRange_Behaves_The_Same_As_Multiple_Selects()
        {
            var data = new[] { 1, 2, 3 };
            var target = new SelectionModel { Source = data };

            target.Select(1);

            Assert.Equal(new[] { IndexPath.CreateFrom(1) }, target.SelectedIndices);

            target.ClearSelection();
            target.SelectRange(new IndexPath(1), new IndexPath(1));

            Assert.Equal(new[] { IndexPath.CreateFrom(1) }, target.SelectedIndices);
        }

        [Fact]
        public void SelectRange_Behaves_The_Same_As_Multiple_Selects_Nested()
        {
            var data = CreateNestedData(3, 2, 2);
            var target = new SelectionModel { Source = data };

            target.Select(1);

            Assert.Equal(new[] { IndexPath.CreateFrom(1) }, target.SelectedIndices);

            target.ClearSelection();
            target.SelectRange(new IndexPath(1), new IndexPath(1));

            Assert.Equal(new[] { IndexPath.CreateFrom(1) }, target.SelectedIndices);
        }

        [Fact]
        public void Should_Not_Treat_Strings_As_Nested_Selections()
        {
            var data = new[] { "foo", "bar", "baz" };
            var target = new SelectionModel { Source = data };

            target.SelectAll();

            Assert.Equal(3, target.SelectedItems.Count);
        }

        [Fact]
        public void Not_Enumerating_Changes_Does_Not_Prevent_Further_Operations()
        {
            var data = new[] { "foo", "bar", "baz" };
            var target = new SelectionModel { Source = data };

            target.SelectionChanged += (s, e) => { };

            target.SelectAll();
            target.ClearSelection();
        }

        [Fact]
        public void Can_Change_Selection_From_SelectionChanged()
        {
            var data = new[] { "foo", "bar", "baz" };
            var target = new SelectionModel { Source = data };
            var raised = 0;

            target.SelectionChanged += (s, e) => 
            {
                if (raised++ == 0)
                {
                    target.ClearSelection();
                }
            };

            target.SelectAll();

            Assert.Equal(2, raised);
        }

        [Fact]
        public void Raises_SelectionChanged_With_No_Source()
        {
            var target = new SelectionModel();
            var raised = 0;

            target.SelectionChanged += (s, e) =>
            {
                Assert.Empty(e.DeselectedIndices);
                Assert.Empty(e.DeselectedItems);
                Assert.Equal(new[] { new IndexPath(1) }, e.SelectedIndices);
                Assert.Empty(e.SelectedItems);
                ++raised;
            };

            target.Select(1);

            Assert.Equal(new[] { new IndexPath(1) }, target.SelectedIndices);
            Assert.Empty(target.SelectedItems);
        }

        [Fact]
        public void Raises_SelectionChanged_With_Items_After_Source_Is_Set()
        {
            var target = new SelectionModel();
            var raised = 0;

            target.Select(1);
            target.SelectionChanged += (s, e) =>
            {
                Assert.Empty(e.DeselectedIndices);
                Assert.Empty(e.DeselectedItems);
                Assert.Equal(new[] { new IndexPath(1) }, e.SelectedIndices);
                Assert.Equal(new[] { "bar" }, e.SelectedItems);
                ++raised;
            };

            target.Source = new[] { "foo", "bar", "baz" };

            Assert.Equal(1, raised);
        }

        [Fact]
        public void RetainSelectionOnReset_Retains_Selection_On_Reset()
        {
            var data = new ResettingList<string> { "foo", "bar", "baz" };
            var target = new SelectionModel { Source = data, RetainSelectionOnReset = true };

            target.SelectRange(new IndexPath(1), new IndexPath(2));
            data.Reset();

            Assert.Equal(new[] { new IndexPath(1), new IndexPath(2) }, target.SelectedIndices);
            Assert.Equal(new[] { "bar", "baz" }, target.SelectedItems);
        }

        [Fact]
        public void RetainSelectionOnReset_Retains_Correct_Selection_After_Deselect()
        {
            var data = new ResettingList<string> { "foo", "bar", "baz" };
            var target = new SelectionModel { Source = data, RetainSelectionOnReset = true };

            target.SelectRange(new IndexPath(1), new IndexPath(2));
            target.Deselect(2);
            data.Reset();

            Assert.Equal(new[] { new IndexPath(1) }, target.SelectedIndices);
            Assert.Equal(new[] { "bar" }, target.SelectedItems);
        }

        [Fact]
        public void RetainSelectionOnReset_Retains_Correct_Selection_After_Remove_1()
        {
            var data = new ResettingList<string> { "foo", "bar", "baz" };
            var target = new SelectionModel { Source = data, RetainSelectionOnReset = true };

            target.SelectRange(new IndexPath(1), new IndexPath(2));
            data.RemoveAt(2);
            data.Reset(new[] { "foo", "bar", "baz" });

            Assert.Equal(new[] { new IndexPath(1) }, target.SelectedIndices);
            Assert.Equal(new[] { "bar" }, target.SelectedItems);
        }

        [Fact]
        public void RetainSelectionOnReset_Retains_Correct_Selection_After_Remove_2()
        {
            var data = new ResettingList<string> { "foo", "bar", "baz" };
            var target = new SelectionModel { Source = data, RetainSelectionOnReset = true };

            target.SelectRange(new IndexPath(1), new IndexPath(2));
            data.RemoveAt(0);
            data.Reset(new[] { "foo", "bar", "baz" });

            Assert.Equal(new[] { new IndexPath(1), new IndexPath(2) }, target.SelectedIndices);
            Assert.Equal(new[] { "bar", "baz" }, target.SelectedItems);
        }

        [Fact]
        public void RetainSelectionOnReset_Retains_No_Selection_After_Clear()
        {
            var data = new ResettingList<string> { "foo", "bar", "baz" };
            var target = new SelectionModel { Source = data, RetainSelectionOnReset = true };

            target.SelectRange(new IndexPath(1), new IndexPath(2));
            target.ClearSelection();
            data.Reset();

            Assert.Empty(target.SelectedIndices);
            Assert.Empty(target.SelectedItems);
        }

        [Fact]
        public void RetainSelectionOnReset_Retains_Correct_Selection_After_Two_Resets()
        {
            var data = new ResettingList<string> { "foo", "bar", "baz" };
            var target = new SelectionModel { Source = data, RetainSelectionOnReset = true };

            target.SelectRange(new IndexPath(1), new IndexPath(2));
            data.Reset(new[] { "foo", "bar" });
            data.Reset(new[] { "foo", "bar", "baz" });

            Assert.Equal(new[] { new IndexPath(1) }, target.SelectedIndices);
            Assert.Equal(new[] { "bar", }, target.SelectedItems);
        }

        [Fact]
        public void RetainSelectionOnReset_Raises_Empty_SelectionChanged_On_Reset_With_No_Changes()
        {
            var data = new ResettingList<string> { "foo", "bar", "baz" };
            var target = new SelectionModel { Source = data, RetainSelectionOnReset = true };
            var raised = 0;

            target.SelectRange(new IndexPath(1), new IndexPath(2));

            target.SelectionChanged += (s, e) =>
            {
                Assert.Empty(e.DeselectedIndices);
                Assert.Empty(e.DeselectedItems);
                Assert.Empty(e.SelectedIndices);
                Assert.Empty(e.SelectedItems);
                ++raised;
            };

            data.Reset();
        }

        [Fact]
        public void RetainSelectionOnReset_Raises_SelectionChanged_On_Reset_With_Removed_Items()
        {
            var data = new ResettingList<string> { "foo", "bar", "baz" };
            var target = new SelectionModel { Source = data, RetainSelectionOnReset = true };
            var raised = 0;

            target.SelectRange(new IndexPath(1), new IndexPath(2));

            target.SelectionChanged += (s, e) =>
            {
                Assert.Empty(e.DeselectedIndices);
                Assert.Equal(new[] { "bar" }, e.DeselectedItems);
                Assert.Empty(e.SelectedIndices);
                Assert.Empty(e.SelectedItems);
                ++raised;
            };

            data.Reset(new[] { "foo", "baz" });

            Assert.Equal(1, raised);
        }

        [Fact]
        public void RetainSelectionOnReset_Handles_Null_Source()
        {
            var data = new ResettingList<string> { "foo", "bar", "baz" };
            var target = new SelectionModel { RetainSelectionOnReset = true };
            var raised = 0;

            target.SelectionChanged += (s, e) =>
            {
                if (raised == 0)
                {
                    Assert.Empty(e.DeselectedIndices);
                    Assert.Empty(e.DeselectedItems);
                    Assert.Equal(new[] { new IndexPath(1) }, e.SelectedIndices);
                    Assert.Empty(e.SelectedItems);
                }
                else if (raised == 1)
                {
                    Assert.Empty(e.DeselectedIndices);
                    Assert.Empty(e.DeselectedItems);
                    Assert.Equal(new[] { new IndexPath(1) }, e.SelectedIndices);
                    Assert.Equal(new[] { "bar" }, e.SelectedItems);
                }
                else if (raised == 3)
                {
                    Assert.Empty(e.DeselectedIndices);
                    Assert.Empty(e.DeselectedItems);
                    Assert.Empty(e.SelectedIndices);
                    Assert.Empty(e.SelectedItems);
                }

                ++raised;
            };

            target.Select(1);
            Assert.Equal(1, raised);

            target.Source = data;
            Assert.Equal(2, raised);
            Assert.Equal(new[] { new IndexPath(1) }, target.SelectedIndices);

            data.Reset(new[] { "qux", "foo", "bar", "baz" });
            Assert.Equal(3, raised);
            Assert.Equal(new[] { new IndexPath(2) }, target.SelectedIndices);
        }
        
        [Fact]
        public void Can_Batch_Update()
        {
            var target = new SelectionModel();
            var raised = 0;

            target.Source = Enumerable.Range(0, 10).ToList();
            target.Select(1);

            target.SelectionChanged += (s, e) =>
            {
                Assert.Equal(new[] { new IndexPath(1) }, e.DeselectedIndices);
                Assert.Equal(new object[] { 1 }, e.DeselectedItems);
                Assert.Equal(new[] { new IndexPath(4) }, e.SelectedIndices);
                Assert.Equal(new object[] { 4 }, e.SelectedItems);
                ++raised;
            };

            using (target.Update())
            {
                target.Deselect(1);
                target.Select(4);
            }

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Batch_Update_Clear_Nested_Data_Raises_SelectionChanged()
        {
            var target = new SelectionModel();
            var raised = 0;

            target.Source = CreateNestedData(3, 2, 2);
            target.SelectRange(new IndexPath(0), new IndexPath(1, 1));

            Assert.Equal(24, target.SelectedIndices.Count);

            var indices = target.SelectedIndices.ToList();
            var items = target.SelectedItems.ToList();

            target.SelectionChanged += (s, e) =>
            {
                Assert.Equal(indices, e.DeselectedIndices);
                Assert.Equal(items, e.DeselectedItems);
                Assert.Empty(e.SelectedIndices);
                Assert.Empty(e.SelectedItems);
                ++raised;
            };

            using (target.Update())
            {
                target.ClearSelection();
            }

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Batch_Update_Does_Not_Raise_PropertyChanged_Until_Operation_Finished()
        {
            var data = new[] { "foo", "bar", "baz", "qux" };
            var target = new SelectionModel { Source = data };
            var raised = 0;

            target.SelectedIndex = new IndexPath(1);

            Assert.Equal(new IndexPath(1), target.AnchorIndex);

            target.PropertyChanged += (s, e) => ++raised;

            using (target.Update())
            {
                target.ClearSelection();

                Assert.Equal(0, raised);

                target.AnchorIndex = new IndexPath(2);

                Assert.Equal(0, raised);

                target.SelectedIndex = new IndexPath(3);

                Assert.Equal(0, raised);
            }

            Assert.Equal(new IndexPath(3), target.AnchorIndex);
            Assert.Equal(5, raised);
        }

        [Fact]
        public void Batch_Update_Does_Not_Raise_PropertyChanged_If_Nothing_Changed()
        {
            var data = new[] { "foo", "bar", "baz", "qux" };
            var target = new SelectionModel { Source = data };
            var raised = 0;

            target.SelectedIndex = new IndexPath(1);

            Assert.Equal(new IndexPath(1), target.AnchorIndex);

            target.PropertyChanged += (s, e) => ++raised;

            using (target.Update())
            {
                target.ClearSelection();
                target.SelectedIndex = new IndexPath(1);
            }

            Assert.Equal(0, raised);
        }

        [Fact]
        public void Batch_Update_Selection_Is_Correct_Throughout()
        {
            var data = new[] { "foo", "bar", "baz", "qux" };
            var target = new SelectionModel { Source = data };
            var raised = 0;

            using (target.Update())
            {
                target.Select(1);

                Assert.Equal(new IndexPath(1), target.SelectedIndex);
                Assert.Equal(new[] { new IndexPath(1) }, target.SelectedIndices);
                Assert.Equal("bar", target.SelectedItem);
                Assert.Equal(new[] { "bar" }, target.SelectedItems);

                target.Deselect(1);

                Assert.Equal(new IndexPath(), target.SelectedIndex);
                Assert.Empty(target.SelectedIndices);
                Assert.Null(target.SelectedItem);
                Assert.Empty(target.SelectedItems);

                target.SelectRange(new IndexPath(1), new IndexPath(1));

                Assert.Equal(new IndexPath(1), target.SelectedIndex);
                Assert.Equal(new[] { new IndexPath(1) }, target.SelectedIndices);
                Assert.Equal("bar", target.SelectedItem);
                Assert.Equal(new[] { "bar" }, target.SelectedItems);

                target.ClearSelection();

                Assert.Equal(new IndexPath(), target.SelectedIndex);
                Assert.Empty(target.SelectedIndices);
                Assert.Null(target.SelectedItem);
                Assert.Empty(target.SelectedItems);
            }

            Assert.Equal(0, raised);
        }

        [Fact]
        public void AutoSelect_Selects_When_Enabled()
        {
            var data = new[] { "foo", "bar", "baz" };
            var target = new SelectionModel { Source = data };
            var raised = 0;

            target.SelectionChanged += (s, e) =>
            {
                Assert.Empty(e.DeselectedIndices);
                Assert.Empty(e.DeselectedItems);
                Assert.Equal(new[] { new IndexPath(0) }, e.SelectedIndices);
                Assert.Equal(new[] { "foo" }, e.SelectedItems);
                ++raised;
            };

            target.AutoSelect = true;

            Assert.Equal(new IndexPath(0), target.SelectedIndex);
            Assert.Equal(1, raised);
        }

        [Fact]
        public void AutoSelect_Selects_When_Source_Assigned()
        {
            var data = new[] { "foo", "bar", "baz" };
            var target = new SelectionModel { AutoSelect = true };
            var raised = 0;

            target.SelectionChanged += (s, e) =>
            {
                Assert.Empty(e.DeselectedIndices);
                Assert.Empty(e.DeselectedItems);
                Assert.Equal(new[] { new IndexPath(0) }, e.SelectedIndices);
                Assert.Equal(new[] { "foo" }, e.SelectedItems);
                ++raised;
            };

            target.Source = data;
            
            Assert.Equal(new IndexPath(0), target.SelectedIndex);
            Assert.Equal(1, raised);
        }

        [Fact]
        public void AutoSelect_Selects_When_New_Source_Assigned_And_Old_Source_Has_Selection()
        {
            var data = new[] { "foo", "bar", "baz" };
            var target = new SelectionModel { AutoSelect = true, Source = data };
            var raised = 0;

            target.SelectionChanged += (s, e) =>
            {
                if (raised == 0)
                {
                    Assert.Equal(new[] { new IndexPath(0) }, e.DeselectedIndices);
                    Assert.Equal(new[] { "foo" }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndices);
                    Assert.Empty(e.SelectedItems);
                }
                else
                {
                    Assert.Empty(e.DeselectedIndices);
                    Assert.Empty(e.DeselectedItems);
                    Assert.Equal(new[] { new IndexPath(0) }, e.SelectedIndices);
                    Assert.Equal(new[] { "newfoo" }, e.SelectedItems);
                }
                ++raised;
            };

            target.Source = new[] { "newfoo" };

            Assert.Equal(new IndexPath(0), target.SelectedIndex);
            Assert.Equal(2, raised);
        }

        [Fact]
        public void AutoSelect_Selects_When_First_Item_Added()
        {
            var data = new ObservableCollection<string>();
            var target = new SelectionModel { AutoSelect = true , Source = data };
            var raised = 0;

            target.SelectionChanged += (s, e) =>
            {
                Assert.Empty(e.DeselectedIndices);
                Assert.Empty(e.DeselectedItems);
                Assert.Equal(new[] { new IndexPath(0) }, e.SelectedIndices);
                Assert.Equal(new[] { "foo" }, e.SelectedItems);
                ++raised;
            };

            data.Add("foo");

            Assert.Equal(new IndexPath(0), target.SelectedIndex);
            Assert.Equal(1, raised);
        }

        [Fact]
        public void AutoSelect_Selects_When_Selected_Item_Removed()
        {
            var data = new ObservableCollection<string> { "foo", "bar", "baz" };
            var target = new SelectionModel { AutoSelect = true, Source = data };
            var raised = 0;

            target.SelectedIndex = new IndexPath(2);

            target.SelectionChanged += (s, e) =>
            {
                if (raised == 0)
                {
                    Assert.Empty(e.DeselectedIndices);
                    Assert.Equal(new[] { "baz" }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndices);
                    Assert.Empty(e.SelectedItems);
                }
                else
                {
                    Assert.Empty(e.DeselectedIndices);
                    Assert.Empty(e.DeselectedItems);
                    Assert.Equal(new[] { new IndexPath(0) }, e.SelectedIndices);
                    Assert.Equal(new[] { "foo" }, e.SelectedItems);
                }

                ++raised;
            };

            data.RemoveAt(2);

            Assert.Equal(new IndexPath(0), target.SelectedIndex);
            Assert.Equal(2, raised);
        }

        [Fact]
        public void AutoSelect_Selects_On_Deselection()
        {
            var data = new[] { "foo", "bar", "baz" };
            var target = new SelectionModel { AutoSelect = true, Source = data };
            var raised = 0;

            target.SelectedIndex = new IndexPath(2);

            target.SelectionChanged += (s, e) =>
            {
                Assert.Equal(new[] { new IndexPath(2) }, e.DeselectedIndices);
                Assert.Equal(new[] { "baz" }, e.DeselectedItems);
                Assert.Equal(new[] { new IndexPath(0) }, e.SelectedIndices);
                Assert.Equal(new[] { "foo" }, e.SelectedItems);
                ++raised;
            };

            target.Deselect(2);

            Assert.Equal(new IndexPath(0), target.SelectedIndex);
            Assert.Equal(1, raised);
        }

        [Fact]
        public void AutoSelect_Selects_On_ClearSelection()
        {
            var data = new[] { "foo", "bar", "baz" };
            var target = new SelectionModel { AutoSelect = true, Source = data };
            var raised = 0;

            target.SelectedIndex = new IndexPath(2);

            target.SelectionChanged += (s, e) =>
            {
                Assert.Equal(new[] { new IndexPath(2) }, e.DeselectedIndices);
                Assert.Equal(new[] { "baz" }, e.DeselectedItems);
                Assert.Equal(new[] { new IndexPath(0) }, e.SelectedIndices);
                Assert.Equal(new[] { "foo" }, e.SelectedItems);
                ++raised;
            };

            target.ClearSelection();

            Assert.Equal(new IndexPath(0), target.SelectedIndex);
            Assert.Equal(1, raised);
        }

        [Fact]
        public void AutoSelect_Overrides_Deselecting_First_Item()
        {
            var data = new[] { "foo", "bar", "baz" };
            var target = new SelectionModel { AutoSelect = true, Source = data };
            var raised = 0;

            target.Select(0);

            target.SelectionChanged += (s, e) =>
            {
                ++raised;
            };

            target.Deselect(0);

            Assert.Equal(new IndexPath(0), target.SelectedIndex);
            Assert.Equal(0, raised);
        }

        [Fact]
        public void AutoSelect_Is_Applied_At_End_Of_Batch_Update()
        {
            var data = new[] { "foo", "bar", "baz" };
            var target = new SelectionModel { AutoSelect = true, Source = data };

            using (target.Update())
            {
                target.ClearSelection();

                Assert.Equal(new IndexPath(), target.SelectedIndex);
                Assert.Empty(target.SelectedIndices);
                Assert.Null(target.SelectedItem);
                Assert.Empty(target.SelectedItems);
            }

            Assert.Equal(new IndexPath(0), target.SelectedIndex);
            Assert.Equal(new[] { new IndexPath(0) }, target.SelectedIndices);
            Assert.Equal("foo", target.SelectedItem);
            Assert.Equal(new[] { "foo" }, target.SelectedItems);

            Assert.Equal(new IndexPath(0), target.SelectedIndex);
        }

        [Fact]
        public void Can_Replace_Parent_Children_Collection()
        {
            var root = new Node("Root");
            var target = new SelectionModel { Source = new[] { root } };
            var raised = 0;

            target.ChildrenRequested += (s, e) => e.Children = ((Node)e.Source).WhenAnyValue(x => x.Children);

            target.Select(0, 9);

            var selected = (Node)target.SelectedItem;
            Assert.Equal("Child 9", selected.Header);

            target.SelectionChanged += (s, e) =>
            {
                Assert.Equal(new[] { Path(0, 9) }, e.DeselectedIndices);
                Assert.Equal(new[] { selected }, e.DeselectedItems);
                Assert.Empty(e.SelectedIndices);
                Assert.Empty(e.SelectedItems);
                ++raised;
            };

            root.ReplaceChildren();

            Assert.Null(target.SelectedItem);
            Assert.Equal(1, raised);
        }

        [Fact]
        public void Can_Replace_Grandparent_Children_Collection()
        {
            var root = new Node("Root");
            var target = new SelectionModel { Source = new[] { root } };
            var raised = 0;

            target.ChildrenRequested += (s, e) => e.Children = ((Node)e.Source).WhenAnyValue(x => x.Children);

            target.SelectAt(Path(0, 9, 1));

            var selected = (Node)target.SelectedItem;
            Assert.Equal("Child 1", selected.Header);

            target.SelectionChanged += (s, e) =>
            {
                Assert.Equal(new[] { Path(0, 9, 1) }, e.DeselectedIndices);
                Assert.Equal(new[] { selected }, e.DeselectedItems);
                Assert.Empty(e.SelectedIndices);
                Assert.Empty(e.SelectedItems);
                ++raised;
            };

            root.ReplaceChildren();

            Assert.Null(target.SelectedItem);
            Assert.Equal(1, raised);
        }

        [Fact]
        public void Child_Resolver_Is_Unsubscribed_When_Source_Changed()
        {
            var root = new Node("Root");
            var target = new SelectionModel { Source = new[] { root } };
            target.ChildrenRequested += (s, e) => e.Children = ((Node)e.Source).WhenAnyValue(x => x.Children);

            target.Select(0, 9);

            Assert.Equal(1, root.PropertyChangedSubscriptions);

            target.Source = null;

            Assert.Equal(0, root.PropertyChangedSubscriptions);
        }

        [Fact]
        public void Child_Resolver_Is_Unsubscribed_When_Parent_Removed()
        {
            var root = new Node("Root");
            var target = new SelectionModel { Source = new[] { root } };
            var node = root.Children[1];
            var path = new IndexPath(new[] { 0, 1, 1 });

            target.ChildrenRequested += (s, e) => e.Children = ((Node)e.Source).WhenAnyValue(x => x.Children);

            target.SelectAt(path);

            Assert.Equal(1, node.PropertyChangedSubscriptions);

            root.ReplaceChildren();

            Assert.Equal(0, node.PropertyChangedSubscriptions);
        }

        [Fact]
        public void Setting_SelectedIndex_To_Minus_1_Clears_Selection()
        {
            var data = new[] { "foo", "bar", "baz" };
            var target = new SelectionModel { Source = data };
            target.SelectedIndex = new IndexPath(1);
            target.SelectedIndex = new IndexPath(-1);
            Assert.Empty(target.SelectedIndices);
        }

        [Fact]
        public void Assigning_Source_With_Less_Items_Than_Previous_Clears_Selection()
        {
            var data = new[] { "foo", "bar", "baz", "boo", "hoo" };
            var smallerData = new[] { "foo", "bar", "baz" };
            var target = new SelectionModel { RetainSelectionOnReset = true };
            target.Source = data;
            target.SelectedIndex = new IndexPath(4);
            target.Source = smallerData;
            Assert.Empty(target.SelectedIndices);
        }

        [Fact]
        public void Initializing_Source_With_Less_Items_Than_Selection_Trims_Selection()
        {
            var data = new[] { "foo", "bar", "baz" };
            var target = new SelectionModel();
            target.SelectedIndex = new IndexPath(4);
            target.Source = data;
            Assert.Empty(target.SelectedIndices);
        }

        [Fact]
        public void Initializing_Source_With_Less_Items_Than_Selection_Trims_Selection_RetainSelection()
        {
            var data = new[] { "foo", "bar", "baz" };
            var target = new SelectionModel { RetainSelectionOnReset = true };
            target.SelectedIndex = new IndexPath(4);
            target.Source = data;
            Assert.Empty(target.SelectedIndices);
        }

        [Fact]
        public void Initializing_Source_With_Less_Items_Than_Multiple_Selection_Trims_Selection()
        {
            var data = new[] { "foo", "bar", "baz" };
            var target = new SelectionModel { RetainSelectionOnReset = true };
            target.Select(4);
            target.Select(2);
            target.Source = data;
            Assert.Equal(1, target.SelectedIndices.Count);
            Assert.Equal(new IndexPath(2), target.SelectedIndices.First());
        }

        [Fact]
        public void Initializing_Source_With_Less_Items_Than_Selection_Raises_SelectionChanged()
        {
            var data = new[] { "foo", "bar", "baz" };
            var target = new SelectionModel();
            var raised = 0;

            target.SelectedIndex = new IndexPath(4);

            target.SelectionChanged += (s, e) =>
            {
                if (raised == 0)
                {
                    Assert.Equal(new[] { Path(4) }, e.DeselectedIndices);
                    Assert.Equal(new object[] { null }, e.DeselectedItems);
                    Assert.Empty(e.SelectedIndices);
                    Assert.Empty(e.SelectedItems);
                }

                ++raised;
            };

            target.Source = data;
            
            Assert.Equal(2, raised);
        }

        private int GetSubscriberCount(AvaloniaList<object> list)
        {
            return ((INotifyCollectionChangedDebug)list).GetCollectionChangedSubscribers()?.Length ?? 0;
        }

        private void VerifyCollectionChangedHandlers(int expected, AvaloniaList<object> list)
        {
            var count = GetSubscriberCount(list);
            
            Assert.Equal(expected, count);

            foreach (var i in list)
            {
                if (i is AvaloniaList<object> l)
                {
                    VerifyCollectionChangedHandlers(expected, l);
                }
            }
        }

        private void Select(SelectionModel manager, int index, bool select)
        {
            _output.WriteLine((select ? "Selecting " : "DeSelecting ") + index);
            if (select)
            {
                manager.Select(index);
            }
            else
            {
                manager.Deselect(index);
            }
        }

        private void Select(SelectionModel manager, int groupIndex, int itemIndex, bool select)
        {
            _output.WriteLine((select ? "Selecting " : "DeSelecting ") + groupIndex + "." + itemIndex);
            if (select)
            {
                manager.Select(groupIndex, itemIndex);
            }
            else
            {
                manager.Deselect(groupIndex, itemIndex);
            }
        }

        private void Select(SelectionModel manager, IndexPath index, bool select)
        {
            _output.WriteLine((select ? "Selecting " : "DeSelecting ") + index);
            if (select)
            {
                manager.SelectAt(index);
            }
            else
            {
                manager.DeselectAt(index);
            }
        }

        private void SelectRangeFromAnchor(SelectionModel manager, int index, bool select)
        {
            _output.WriteLine("SelectRangeFromAnchor " + index + " select: " + select.ToString());
            if (select)
            {
                manager.SelectRangeFromAnchor(index);
            }
            else
            {
                manager.DeselectRangeFromAnchor(index);
            }
        }

        private void SelectRangeFromAnchor(SelectionModel manager, int groupIndex, int itemIndex, bool select)
        {
            _output.WriteLine("SelectRangeFromAnchor " + groupIndex + "." + itemIndex + " select:" + select.ToString());
            if (select)
            {
                manager.SelectRangeFromAnchor(groupIndex, itemIndex);
            }
            else
            {
                manager.DeselectRangeFromAnchor(groupIndex, itemIndex);
            }
        }

        private void SelectRangeFromAnchor(SelectionModel manager, IndexPath index, bool select)
        {
            _output.WriteLine("SelectRangeFromAnchor " + index + " select: " + select.ToString());
            if (select)
            {
                manager.SelectRangeFromAnchorTo(index);
            }
            else
            {
                manager.DeselectRangeFromAnchorTo(index);
            }
        }

        private void ClearSelection(SelectionModel manager)
        {
            _output.WriteLine("ClearSelection");
            manager.ClearSelection();
        }

        private void SetAnchorIndex(SelectionModel manager, int index)
        {
            _output.WriteLine("SetAnchorIndex " + index);
            manager.SetAnchorIndex(index);
        }

        private void SetAnchorIndex(SelectionModel manager, int groupIndex, int itemIndex)
        {
            _output.WriteLine("SetAnchor " + groupIndex + "." + itemIndex);
            manager.SetAnchorIndex(groupIndex, itemIndex);
        }

        private void SetAnchorIndex(SelectionModel manager, IndexPath index)
        {
            _output.WriteLine("SetAnchor " + index);
            manager.AnchorIndex = index;
        }

        private void ValidateSelection(
            SelectionModel selectionModel,
            params IndexPath[] expectedSelected)
        {
            Assert.Equal(expectedSelected, selectionModel.SelectedIndices);
        }

        private object GetData(SelectionModel selectionModel, IndexPath indexPath)
        {
            var data = selectionModel.Source;
            for (int i = 0; i < indexPath.GetSize(); i++)
            {
                var listData = data as IList;
                data = listData[indexPath.GetAt(i)];
            }

            return data;
        }

        private bool AreEqual(IndexPath a, IndexPath b)
        {
            if (a.GetSize() != b.GetSize())
            {
                return false;
            }

            for (int i = 0; i < a.GetSize(); i++)
            {
                if (a.GetAt(i) != b.GetAt(i))
                {
                    return false;
                }
            }

            return true;
        }

        private List<IndexPath> GetIndexPathsInSource(object source)
        {
            List<IndexPath> paths = new List<IndexPath>();
            Traverse(source, (TreeWalkNodeInfo node) =>
            {
                if (!paths.Contains(node.Path))
                {
                    paths.Add(node.Path);
                }
            });

            _output.WriteLine("All Paths in source..");
            foreach (var path in paths)
            {
                _output.WriteLine(path.ToString());
            }
            _output.WriteLine("done.");

            return paths;
        }

        private static void Traverse(object root, Action<TreeWalkNodeInfo> nodeAction)
        {
            var pendingNodes = new Stack<TreeWalkNodeInfo>();
            IndexPath current = Path(null);
            pendingNodes.Push(new TreeWalkNodeInfo() { Current = root, Path = current });

            while (pendingNodes.Count > 0)
            {
                var currentNode = pendingNodes.Pop();
                var currentObject = currentNode.Current as IList;

                if (currentObject != null)
                {
                    for (int i = currentObject.Count - 1; i >= 0; i--)
                    {
                        var child = currentObject[i];
                        List<int> path = new List<int>();
                        for (int idx = 0; idx < currentNode.Path.GetSize(); idx++)
                        {
                            path.Add(currentNode.Path.GetAt(idx));
                        }

                        path.Add(i);
                        var childPath = IndexPath.CreateFromIndices(path);
                        if (child != null)
                        {
                            pendingNodes.Push(new TreeWalkNodeInfo() { Current = child, Path = childPath });
                        }
                    }
                }

                nodeAction(currentNode);
            }
        }

        private bool Contains(List<IndexPath> list, IndexPath index)
        {
            bool contains = false;
            foreach (var item in list)
            {
                if (item.CompareTo(index) == 0)
                {
                    contains = true;
                    break;
                }
            }

            return contains;
        }

        public static AvaloniaList<object> CreateNestedData(int levels = 3, int groupsAtLevel = 5, int countAtLeaf = 10)
        {
            var nextData = 0;
            return CreateNestedData(levels, groupsAtLevel, countAtLeaf, ref nextData);
        }

        public static AvaloniaList<object> CreateNestedData(
            int levels,
            int groupsAtLevel,
            int countAtLeaf,
            ref int nextData)
        {
            var data = new AvaloniaList<object>();
            if (levels != 0)
            {
                for (int i = 0; i < groupsAtLevel; i++)
                {
                    data.Add(CreateNestedData(levels - 1, groupsAtLevel, countAtLeaf, ref nextData));
                }
            }
            else
            {
                for (int i = 0; i < countAtLeaf; i++)
                {
                    data.Add(nextData++);
                }
            }

            return data;
        }

        static IndexPath Path(params int[] path)
        {
            return IndexPath.CreateFromIndices(path);
        }

        private static int _nextData = 0;
        private struct TreeWalkNodeInfo
        {
            public object Current { get; set; }

            public IndexPath Path { get; set; }
        }

        private class ResettingList<T> : List<object>, INotifyCollectionChanged
        {
            public event NotifyCollectionChangedEventHandler CollectionChanged;

            public new void RemoveAt(int index)
            {
                var item = this[index];
                base.RemoveAt(index);
                CollectionChanged?.Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new[] { item }, index));
            }

            public void Reset(IEnumerable<object> items = null)
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

        private class Node : INotifyPropertyChanged
        {
            private ObservableCollection<Node> _children;
            private PropertyChangedEventHandler _propertyChanged;

            public Node(string header)
            {
                Header = header;
            }

            public string Header { get; }

            public ObservableCollection<Node> Children
            {
                get => _children ??= CreateChildren(10);
                private set
                {
                    _children = value;
                    _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Children)));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged
            {
                add
                {
                    _propertyChanged += value;
                    ++PropertyChangedSubscriptions;
                }

                remove
                {
                    _propertyChanged -= value;
                    --PropertyChangedSubscriptions;
                }
            }

            public int PropertyChangedSubscriptions { get; private set; }

            public void ReplaceChildren()
            {
                Children = CreateChildren(5);
            }

            private ObservableCollection<Node> CreateChildren(int count)
            {
                return new ObservableCollection<Node>(
                    Enumerable.Range(0, count).Select(x => new Node("Child " + x)));
            }
        }
    }

    class CustomSelectionModel : SelectionModel
    {
        public int IntProperty
        {
            get { return _intProperty; }
            set
            {
                _intProperty = value;
                OnPropertyChanged("IntProperty");
            }
        }

        private int _intProperty;
    }
}
