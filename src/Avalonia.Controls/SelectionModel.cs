// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls.Utils;

#nullable enable

namespace Avalonia.Controls
{
    public class SelectionModel : ISelectionModel, IDisposable
    {
        private readonly SelectionNode _rootNode;
        private bool _singleSelect;
        private bool _autoSelect;
        private int _operationCount;
        private IndexPath _oldAnchorIndex;
        private IReadOnlyList<IndexPath>? _selectedIndicesCached;
        private IReadOnlyList<object?>? _selectedItemsCached;
        private SelectionModelChildrenRequestedEventArgs? _childrenRequestedEventArgs;

        public event EventHandler<SelectionModelChildrenRequestedEventArgs>? ChildrenRequested;
        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<SelectionModelSelectionChangedEventArgs>? SelectionChanged;

        public SelectionModel()
        {
            _rootNode = new SelectionNode(this, null);
            SharedLeafNode = new SelectionNode(this, null);
        }

        public object? Source
        {
            get => _rootNode.Source;
            set
            {
                if (_rootNode.Source != value)
                {
                    var raiseChanged = _rootNode.Source == null && SelectedIndices.Count > 0;

                    if (_rootNode.Source != null)
                    {
                        // Temporarily prevent auto-select when switching source.
                        var restoreAutoSelect = _autoSelect;
                        _autoSelect = false;

                        try
                        {
                            using (var operation = new Operation(this))
                            {
                                ClearSelection(resetAnchor: true);
                            }
                        }
                        finally
                        {
                            _autoSelect = restoreAutoSelect;
                        }
                    }

                    _rootNode.Source = value;
                    ApplyAutoSelect(true);

                    RaisePropertyChanged("Source");

                    if (raiseChanged)
                    {
                        var e = new SelectionModelSelectionChangedEventArgs(
                            null,
                            SelectedIndices,
                            null,
                            SelectedItems);
                        OnSelectionChanged(e);
                    }
                }
            }
        }

        public bool SingleSelect
        {
            get => _singleSelect;
            set
            {
                if (_singleSelect != value)
                {
                    _singleSelect = value;
                    var selectedIndices = SelectedIndices;

                    if (value && selectedIndices != null && selectedIndices.Count > 0)
                    {
                        using var operation = new Operation(this);

                        // We want to be single select, so make sure there is only 
                        // one selected item.
                        var firstSelectionIndexPath = selectedIndices[0];
                        ClearSelection(resetAnchor: true);
                        SelectWithPathImpl(firstSelectionIndexPath, select: true);
                        SelectedIndex = firstSelectionIndexPath;
                    }

                    RaisePropertyChanged("SingleSelect");
                }
            }
        }

        public bool RetainSelectionOnReset
        {
            get => _rootNode.RetainSelectionOnReset;
            set => _rootNode.RetainSelectionOnReset = value;
        }

        public bool AutoSelect 
        {
            get => _autoSelect;
            set
            {
                if (_autoSelect != value)
                {
                    _autoSelect = value;
                    ApplyAutoSelect(true);
                }
            }
        }

        public IndexPath AnchorIndex
        {
            get
            {
                IndexPath anchor = default;

                if (_rootNode.AnchorIndex >= 0)
                {
                    var path = new List<int>();
                    SelectionNode? current = _rootNode;

                    while (current?.AnchorIndex >= 0)
                    {
                        path.Add(current.AnchorIndex);
                        current = current.GetAt(current.AnchorIndex, false, default);
                    }

                    anchor = new IndexPath(path);
                }

                return anchor;
            }
            set
            {
                var oldValue = AnchorIndex;

                if (value != null)
                {
                    SelectionTreeHelper.TraverseIndexPath(
                        _rootNode,
                        value,
                        realizeChildren: true,
                        (currentNode, path, depth, childIndex) => currentNode.AnchorIndex = path.GetAt(depth));
                }
                else
                {
                    _rootNode.AnchorIndex = -1;
                }

                if (_operationCount == 0 && oldValue != AnchorIndex)
                {
                    RaisePropertyChanged("AnchorIndex");
                }
            }
        }

        public IndexPath SelectedIndex
        {
            get
            {
                IndexPath selectedIndex = default;
                var selectedIndices = SelectedIndices;

                if (selectedIndices?.Count > 0)
                {
                    selectedIndex = selectedIndices[0];
                }

                return selectedIndex;
            }
            set
            {
                if (!IsSelectedAt(value) || SelectedItems.Count > 1)
                {
                    using var operation = new Operation(this);
                    ClearSelection(resetAnchor: true);
                    SelectWithPathImpl(value, select: true);
                }
            }
        }

        public object? SelectedItem
        {
            get
            {
                object? item = null;
                var selectedItems = SelectedItems;

                if (selectedItems?.Count > 0)
                {
                    item = selectedItems[0];
                }

                return item;
            }
        }

        public IReadOnlyList<object?> SelectedItems
        {
            get
            {
                if (_selectedItemsCached == null)
                {
                    var selectedInfos = new List<SelectedItemInfo>();
                    var count = 0;

                    if (_rootNode.Source != null)
                    {
                        SelectionTreeHelper.Traverse(
                            _rootNode,
                            realizeChildren: false,
                            currentInfo =>
                            {
                                if (currentInfo.Node.SelectedCount > 0)
                                {
                                    selectedInfos.Add(new SelectedItemInfo(currentInfo.Node, currentInfo.Path));
                                    count += currentInfo.Node.SelectedCount;
                                }
                            });
                    }

                    // Instead of creating a dumb vector that takes up the space for all the selected items,
                    // we create a custom IReadOnlyList implementation that calls back using a delegate to find 
                    // the selected item at a particular index. This avoid having to create the storage and copying
                    // needed in a dumb vector. This also allows us to expose a tree of selected nodes into an 
                    // easier to consume flat vector view of objects.
                    var selectedItems = new SelectedItems<object?, SelectedItemInfo> (
                        selectedInfos,
                        count,
                        (infos, index) =>
                        {
                            var currentIndex = 0;
                            object? item = null;

                            foreach (var info in infos)
                            {
                                var node = info.Node;

                                if (node != null)
                                {
                                    var currentCount = node.SelectedCount;

                                    if (index >= currentIndex && index < currentIndex + currentCount)
                                    {
                                        var targetIndex = node.SelectedIndices[index - currentIndex];
                                        item = node.ItemsSourceView!.GetAt(targetIndex);
                                        break;
                                    }

                                    currentIndex += currentCount;
                                }
                                else
                                {
                                    throw new InvalidOperationException(
                                        "Selection has changed since SelectedItems property was read.");
                                }
                            }

                            return item;
                        });

                    _selectedItemsCached = selectedItems;
                }

                return _selectedItemsCached;
            }
        }

        public IReadOnlyList<IndexPath> SelectedIndices
        {
            get
            {
                if (_selectedIndicesCached == null)
                {
                    var selectedInfos = new List<SelectedItemInfo>();
                    var count = 0;

                    SelectionTreeHelper.Traverse(
                        _rootNode,
                        false,
                        currentInfo =>
                        {
                            if (currentInfo.Node.SelectedCount > 0)
                            {
                                selectedInfos.Add(new SelectedItemInfo(currentInfo.Node, currentInfo.Path));
                                count += currentInfo.Node.SelectedCount;
                            }
                        });

                    // Instead of creating a dumb vector that takes up the space for all the selected indices,
                    // we create a custom VectorView implimentation that calls back using a delegate to find 
                    // the IndexPath at a particular index. This avoid having to create the storage and copying
                    // needed in a dumb vector. This also allows us to expose a tree of selected nodes into an 
                    // easier to consume flat vector view of IndexPaths.
                    var indices = new SelectedItems<IndexPath, SelectedItemInfo>(
                        selectedInfos,
                        count,
                        (infos, index) => // callback for GetAt(index)
                        {
                            var currentIndex = 0;
                            IndexPath path = default;

                            foreach (var info in infos)
                            {
                                var node = info.Node;

                                if (node != null)
                                {
                                    var currentCount = node.SelectedCount;
                                    if (index >= currentIndex && index < currentIndex + currentCount)
                                    {
                                        int targetIndex = node.SelectedIndices[index - currentIndex];
                                        path = info.Path.CloneWithChildIndex(targetIndex);
                                        break;
                                    }

                                    currentIndex += currentCount;
                                }
                                else
                                {
                                    throw new InvalidOperationException(
                                        "Selection has changed since SelectedIndices property was read.");
                                }
                            }

                            return path;
                        });

                    _selectedIndicesCached = indices;
                }

                return _selectedIndicesCached;
            }
        }

        internal SelectionNode SharedLeafNode { get; private set; }

        public void Dispose()
        {
            ClearSelection(resetAnchor: false);
            _rootNode.Cleanup();
            _rootNode.Dispose();
            _selectedIndicesCached = null;
            _selectedItemsCached = null;
        }

        public void SetAnchorIndex(int index) => AnchorIndex = new IndexPath(index);

        public void SetAnchorIndex(int groupIndex, int index) => AnchorIndex = new IndexPath(groupIndex, index);

        public void Select(int index)
        {
            using var operation = new Operation(this);
            SelectImpl(index, select: true);
        }

        public void Select(int groupIndex, int itemIndex)
        {
            using var operation = new Operation(this);
            SelectWithGroupImpl(groupIndex, itemIndex, select: true);
        }

        public void SelectAt(IndexPath index)
        {
            using var operation = new Operation(this);
            SelectWithPathImpl(index, select: true);
        }

        public void Deselect(int index)
        {
            using var operation = new Operation(this);
            SelectImpl(index, select: false);
        }

        public void Deselect(int groupIndex, int itemIndex)
        {
            using var operation = new Operation(this);
            SelectWithGroupImpl(groupIndex, itemIndex, select: false);
        }

        public void DeselectAt(IndexPath index)
        {
            using var operation = new Operation(this);
            SelectWithPathImpl(index, select: false);
        }

        public bool IsSelected(int index) => _rootNode.IsSelected(index);

        public bool IsSelected(int grouIndex, int itemIndex)
        {
            return IsSelectedAt(new IndexPath(grouIndex, itemIndex));
        }

        public bool IsSelectedAt(IndexPath index)
        {
            var path = index;
            SelectionNode? node = _rootNode;

            for (int i = 0; i < path.GetSize() - 1; i++)
            {
                var childIndex = path.GetAt(i);
                node = node.GetAt(childIndex, false, default);

                if (node == null)
                {
                    return false;
                }
            }

            return node.IsSelected(index.GetAt(index.GetSize() - 1));
        }

        public bool? IsSelectedWithPartial(int index)
        {
            if (index < 0)
            {
                throw new ArgumentException("Index must be >= 0", nameof(index));
            }

            var isSelected = _rootNode.IsSelectedWithPartial(index);
            return isSelected;
        }

        public bool? IsSelectedWithPartial(int groupIndex, int itemIndex)
        {
            if (groupIndex < 0)
            {
                throw new ArgumentException("Group index must be >= 0", nameof(groupIndex));
            }

            if (itemIndex < 0)
            {
                throw new ArgumentException("Item index must be >= 0", nameof(itemIndex));
            }

            var isSelected = (bool?)false;
            var childNode = _rootNode.GetAt(groupIndex, false, default);

            if (childNode != null)
            {
                isSelected = childNode.IsSelectedWithPartial(itemIndex);
            }

            return isSelected;
        }

        public bool? IsSelectedWithPartialAt(IndexPath index)
        {
            var path = index;
            var isRealized = true;
            SelectionNode? node = _rootNode;

            for (int i = 0; i < path.GetSize() - 1; i++)
            {
                var childIndex = path.GetAt(i);
                node = node.GetAt(childIndex, false, default);

                if (node == null)
                {
                    isRealized = false;
                    break;
                }
            }

            var isSelected = (bool?)false;

            if (isRealized)
            {
                var size = path.GetSize();
                if (size == 0)
                {
                    isSelected = SelectionNode.ConvertToNullableBool(node!.EvaluateIsSelectedBasedOnChildrenNodes());
                }
                else
                {
                    isSelected = node!.IsSelectedWithPartial(path.GetAt(size - 1));
                }
            }

            return isSelected;
        }

        public void SelectRangeFromAnchor(int index)
        {
            using var operation = new Operation(this);
            SelectRangeFromAnchorImpl(index, select: true);
        }

        public void SelectRangeFromAnchor(int endGroupIndex, int endItemIndex)
        {
            using var operation = new Operation(this);
            SelectRangeFromAnchorWithGroupImpl(endGroupIndex, endItemIndex, select: true);
        }

        public void SelectRangeFromAnchorTo(IndexPath index)
        {
            using var operation = new Operation(this);
            SelectRangeImpl(AnchorIndex, index, select: true);
        }

        public void DeselectRangeFromAnchor(int index)
        {
            using var operation = new Operation(this);
            SelectRangeFromAnchorImpl(index, select: false);
        }

        public void DeselectRangeFromAnchor(int endGroupIndex, int endItemIndex)
        {
            using var operation = new Operation(this);
            SelectRangeFromAnchorWithGroupImpl(endGroupIndex, endItemIndex, false /* select */);
        }

        public void DeselectRangeFromAnchorTo(IndexPath index)
        {
            using var operation = new Operation(this);
            SelectRangeImpl(AnchorIndex, index, select: false);
        }

        public void SelectRange(IndexPath start, IndexPath end)
        {
            using var operation = new Operation(this);
            SelectRangeImpl(start, end, select: true);
        }

        public void DeselectRange(IndexPath start, IndexPath end)
        {
            using var operation = new Operation(this);
            SelectRangeImpl(start, end, select: false);
        }

        public void SelectAll()
        {
            using var operation = new Operation(this);

            SelectionTreeHelper.Traverse(
                _rootNode,
                realizeChildren: true,
                info =>
                {
                    if (info.Node.DataCount > 0)
                    {
                        info.Node.SelectAll();
                    }
                });
        }

        public void ClearSelection()
        {
            using var operation = new Operation(this);
            ClearSelection(resetAnchor: true);
        }

        public IDisposable Update() => new Operation(this);

        protected void OnPropertyChanged(string propertyName)
        {
            RaisePropertyChanged(propertyName);
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void OnSelectionInvalidatedDueToCollectionChange(
            bool selectionInvalidated,
            IReadOnlyList<object?>? removedItems)
        {
            SelectionModelSelectionChangedEventArgs? e = null;

            if (selectionInvalidated)
            {
                e = new SelectionModelSelectionChangedEventArgs(null, null, removedItems, null);
            }

            OnSelectionChanged(e);
            ApplyAutoSelect(true);
        }

        internal IObservable<object?>? ResolvePath(
            object data,
            IndexPath dataIndexPath,
            IndexPath finalIndexPath)
        {
            IObservable<object?>? resolved = null;

            // Raise ChildrenRequested event if there is a handler
            if (ChildrenRequested != null)
            {
                if (_childrenRequestedEventArgs == null)
                {
                    _childrenRequestedEventArgs = new SelectionModelChildrenRequestedEventArgs(
                        data,
                        dataIndexPath,
                        finalIndexPath,
                        false);
                }
                else
                {
                    _childrenRequestedEventArgs.Initialize(data, dataIndexPath, finalIndexPath, false);
                }

                ChildrenRequested(this, _childrenRequestedEventArgs);
                resolved = _childrenRequestedEventArgs.Children;

                // Clear out the values in the args so that it cannot be used after the event handler call.
                _childrenRequestedEventArgs.Initialize(null, default, default, true);
            }

            return resolved;
        }

        private void ClearSelection(bool resetAnchor)
        {
            SelectionTreeHelper.Traverse(
                _rootNode,
                realizeChildren: false,
                info => info.Node.Clear());

            if (resetAnchor)
            {
                AnchorIndex = default;
            }

            OnSelectionChanged();
        }

        private void OnSelectionChanged(SelectionModelSelectionChangedEventArgs? e = null)
        {
            _selectedIndicesCached = null;
            _selectedItemsCached = null;

            if (e != null)
            {
                SelectionChanged?.Invoke(this, e);

                RaisePropertyChanged(nameof(SelectedIndex));
                RaisePropertyChanged(nameof(SelectedIndices));

                if (_rootNode.Source != null)
                {
                    RaisePropertyChanged(nameof(SelectedItem));
                    RaisePropertyChanged(nameof(SelectedItems));
                }
            }
        }

        private void SelectImpl(int index, bool select)
        {
            if (_singleSelect)
            {
                ClearSelection(resetAnchor: true);
            }

            var selected = _rootNode.Select(index, select);

            if (selected)
            {
                AnchorIndex = new IndexPath(index);
            }

            OnSelectionChanged();
        }

        private void SelectWithGroupImpl(int groupIndex, int itemIndex, bool select)
        {
            if (_singleSelect)
            {
                ClearSelection(resetAnchor: true);
            }

            var childNode = _rootNode.GetAt(groupIndex, true, new IndexPath(groupIndex, itemIndex));
            var selected = childNode!.Select(itemIndex, select);

            if (selected)
            {
                AnchorIndex = new IndexPath(groupIndex, itemIndex);
            }

            OnSelectionChanged();
        }

        private void SelectWithPathImpl(IndexPath index, bool select)
        {
            bool selected = false;

            if (_singleSelect)
            {
                ClearSelection(resetAnchor: true);
            }

            SelectionTreeHelper.TraverseIndexPath(
                _rootNode,
                index,
                true,
                (currentNode, path, depth, childIndex) =>
                {
                    if (depth == path.GetSize() - 1)
                    {
                        selected = currentNode.Select(childIndex, select);
                    }
                }
            );

            if (selected)
            {
                AnchorIndex = index;
            }

            OnSelectionChanged();
        }

        private void SelectRangeFromAnchorImpl(int index, bool select)
        {
            int anchorIndex = 0;
            var anchor = AnchorIndex;

            if (anchor != null)
            {
                anchorIndex = anchor.GetAt(0);
            }

            _rootNode.SelectRange(new IndexRange(anchorIndex, index), select);
            OnSelectionChanged();
        }

        private void SelectRangeFromAnchorWithGroupImpl(int endGroupIndex, int endItemIndex, bool select)
        {
            var startGroupIndex = 0;
            var startItemIndex = 0;
            var anchorIndex = AnchorIndex;

            if (anchorIndex != null)
            {
                startGroupIndex = anchorIndex.GetAt(0);
                startItemIndex = anchorIndex.GetAt(1);
            }

            // Make sure start > end
            if (startGroupIndex > endGroupIndex ||
                (startGroupIndex == endGroupIndex && startItemIndex > endItemIndex))
            {
                int temp = startGroupIndex;
                startGroupIndex = endGroupIndex;
                endGroupIndex = temp;
                temp = startItemIndex;
                startItemIndex = endItemIndex;
                endItemIndex = temp;
            }

            for (int groupIdx = startGroupIndex; groupIdx <= endGroupIndex; groupIdx++)
            {
                var groupNode = _rootNode.GetAt(groupIdx, true, new IndexPath(endGroupIndex, endItemIndex))!;
                int startIndex = groupIdx == startGroupIndex ? startItemIndex : 0;
                int endIndex = groupIdx == endGroupIndex ? endItemIndex : groupNode.DataCount - 1;
                groupNode.SelectRange(new IndexRange(startIndex, endIndex), select);
            }

            OnSelectionChanged();
        }

        private void SelectRangeImpl(IndexPath start, IndexPath end, bool select)
        {
            var winrtStart = start;
            var winrtEnd = end;

            // Make sure start <= end 
            if (winrtEnd.CompareTo(winrtStart) == -1)
            {
                var temp = winrtStart;
                winrtStart = winrtEnd;
                winrtEnd = temp;
            }

            // Note: Since we do not know the depth of the tree, we have to walk to each leaf
            SelectionTreeHelper.TraverseRangeRealizeChildren(
                _rootNode,
                winrtStart,
                winrtEnd,
                info =>
                {
                    if (info.Path >= winrtStart && info.Path <= winrtEnd)
                    {
                        info.ParentNode!.Select(info.Path.GetAt(info.Path.GetSize() - 1), select);
                    }
                });

            OnSelectionChanged();
        }

        private void BeginOperation()
        {
            if (_operationCount++ == 0)
            {
                _oldAnchorIndex = AnchorIndex;
                _rootNode.BeginOperation();
            }
        }

        private void EndOperation()
        {
            if (_operationCount == 0)
            {
                throw new AvaloniaInternalException("No selection operation in progress.");
            }

            SelectionModelSelectionChangedEventArgs? e = null;

            if (--_operationCount == 0)
            {
                ApplyAutoSelect(false);

                var changes = new List<SelectionNodeOperation>();
                _rootNode.EndOperation(changes);

                if (changes.Count > 0)
                {
                    var changeSet = new SelectionModelChangeSet(changes);
                    e = changeSet.CreateEventArgs();
                }

                OnSelectionChanged(e);
                
                if (_oldAnchorIndex != AnchorIndex)
                {
                    RaisePropertyChanged(nameof(AnchorIndex));
                }

                _rootNode.Cleanup();
                _oldAnchorIndex = default;
            }
        }

        private void ApplyAutoSelect(bool createOperation)
        {
            if (AutoSelect)
            {
                _selectedIndicesCached = null;

                if (SelectedIndex == default && _rootNode.ItemsSourceView?.Count > 0)
                {
                    if (createOperation)
                    {
                        using var operation = new Operation(this);
                        SelectImpl(0, true);
                    }
                    else
                    {
                        SelectImpl(0, true);
                    }
                }
            }
        }

        internal class SelectedItemInfo : ISelectedItemInfo
        {
            public SelectedItemInfo(SelectionNode node, IndexPath path)
            {
                Node = node;
                Path = path;
            }

            public SelectionNode Node { get; }
            public IndexPath Path { get; }
            public int Count => Node.SelectedCount;
        }

        private struct Operation : IDisposable
        {
            private readonly SelectionModel _manager;
            public Operation(SelectionModel manager) => (_manager = manager).BeginOperation();
            public void Dispose() => _manager.EndOperation();
        }
    }
}
