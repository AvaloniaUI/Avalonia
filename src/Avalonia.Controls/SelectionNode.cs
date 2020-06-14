﻿// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls.Utils;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// Tracks nested selection.
    /// </summary>
    /// <remarks>
    /// SelectionNode is the internal tree data structure that we keep track of for selection in 
    /// a nested scenario. This would map to one ItemsSourceView/Collection. This node reacts to
    /// collection changes and keeps the selected indices up to date. This can either be a leaf
    /// node or a non leaf node.
    /// </remarks>
    internal class SelectionNode : IDisposable
    {
        private readonly SelectionModel _manager;
        private readonly List<SelectionNode?> _childrenNodes = new List<SelectionNode?>();
        private readonly SelectionNode? _parent;
        private readonly List<IndexRange> _selected = new List<IndexRange>();
        private readonly List<int> _selectedIndicesCached = new List<int>();
        private IDisposable? _childrenSubscription;
        private SelectionNodeOperation? _operation;
        private object? _source;
        private bool _selectedIndicesCacheIsValid;
        private bool _retainSelectionOnReset;
        private List<object?>? _selectedItems;

        public SelectionNode(SelectionModel manager, SelectionNode? parent)
        {
            _manager = manager;
            _parent = parent;
        }

        public int AnchorIndex { get; set; } = -1;

        public bool RetainSelectionOnReset 
        {
            get => _retainSelectionOnReset;
            set
            {
                if (_retainSelectionOnReset != value)
                {
                    _retainSelectionOnReset = value;

                    if (_retainSelectionOnReset)
                    {
                        _selectedItems = new List<object?>();
                        PopulateSelectedItemsFromSelectedIndices();
                    }
                    else
                    {
                        _selectedItems = null;
                    }

                    foreach (var child in _childrenNodes)
                    {
                        if (child != null)
                        {
                            child.RetainSelectionOnReset = value;
                        }
                    }
                }
            }
        }

        public object? Source
        {
            get => _source;
            set
            {
                if (_source != value)
                {
                    if (_source != null)
                    {
                        ClearSelection();
                        ClearChildNodes();
                        UnhookCollectionChangedHandler();
                    }

                    _source = value;

                    // Setup ItemsSourceView
                    var newDataSource = value as ItemsSourceView;

                    if (value != null && newDataSource == null)
                    {
                        newDataSource = new ItemsSourceView((IEnumerable)value);
                    }

                    ItemsSourceView = newDataSource;

                    TrimInvalidSelections();
                    PopulateSelectedItemsFromSelectedIndices();
                    HookupCollectionChangedHandler();
                    OnSelectionChanged();
                }
            }
        }

        private void TrimInvalidSelections()
        {
            if (_selected == null || ItemsSourceView == null)
            {
                return;
            }

            var validRange = ItemsSourceView.Count > 0 ? new IndexRange(0, ItemsSourceView.Count - 1) : new IndexRange(-1, -1);
            var removed = new List<IndexRange>();
            var removedCount = IndexRange.Intersect(_selected, validRange, removed);

            if (removedCount > 0)
            {
                using var operation = _manager.Update();
                SelectedCount -= removedCount;
                OnSelectionChanged();
                _operation!.Deselected(removed);
            }
        }

        public ItemsSourceView? ItemsSourceView { get; private set; }
        public int DataCount => ItemsSourceView?.Count ?? 0;
        public int ChildrenNodeCount => _childrenNodes.Count;
        public int RealizedChildrenNodeCount { get; private set; }

        public IndexPath IndexPath
        {
            get
            {
                var path = new List<int>(); ;
                var parent = _parent;
                var child = this;
                
                while (parent != null)
                {
                    var childNodes = parent._childrenNodes;
                    var index = childNodes.IndexOf(child);

                    // We are walking up to the parent, so the path will be backwards
                    path.Insert(0, index);
                    child = parent;
                    parent = parent._parent;
                }

                return new IndexPath(path);
            }
        }

        // For a genuine tree view, we dont know which node is leaf until we 
        // actually walk to it, so currently the tree builds up to the leaf. I don't 
        // create a bunch of leaf node instances - instead i use the same instance m_leafNode to avoid 
        // an explosion of node objects. However, I'm still creating the m_childrenNodes 
        // collection unfortunately.
        public SelectionNode? GetAt(int index, bool realizeChild, IndexPath finalIndexPath)
        {
            SelectionNode? child = null;
            
            if (realizeChild)
            {
                if (ItemsSourceView == null || index < 0 || index >= ItemsSourceView.Count)
                {
                    throw new IndexOutOfRangeException();
                }

                if (_childrenNodes.Count == 0)
                {
                    if (ItemsSourceView != null)
                    {
                        for (int i = 0; i < ItemsSourceView.Count; i++)
                        {
                            _childrenNodes.Add(null);
                        }
                    }
                }

                if (_childrenNodes[index] == null)
                {
                    var childData = ItemsSourceView!.GetAt(index);
                    IObservable<object?>? resolver = null;
                    
                    if (childData != null)
                    {
                        var childDataIndexPath = IndexPath.CloneWithChildIndex(index);
                        resolver = _manager.ResolvePath(childData, childDataIndexPath, finalIndexPath);
                    }

                    if (resolver != null)
                    {
                        child = new SelectionNode(_manager, parent: this);
                        child.SetChildrenObservable(resolver);
                    }
                    else if (childData is IEnumerable<object> || childData is IList)
                    {
                        child = new SelectionNode(_manager, parent: this);
                        child.Source = childData;
                    }
                    else
                    { 
                        child = _manager.SharedLeafNode;
                    }

                    if (_operation != null && child != _manager.SharedLeafNode)
                    {
                        child.BeginOperation();
                    }

                    _childrenNodes[index] = child;
                    RealizedChildrenNodeCount++;
                }
                else
                {
                    child = _childrenNodes[index];
                }
            }
            else
            {
                if (_childrenNodes.Count > 0)
                {
                    child = _childrenNodes[index];
                }
            }

            return child;
        }

        public void SetChildrenObservable(IObservable<object?> resolver)
        {
            _childrenSubscription = resolver.Subscribe(x =>
            {
                if (Source != null)
                {
                    using (_manager.Update())
                    {
                        SelectionTreeHelper.Traverse(
                            this,
                            realizeChildren: false,
                            info => info.Node.Clear());
                    }
                }

                Source = x;
            });
        }

        public int SelectedCount { get; private set; }

        public bool IsSelected(int index)
        {
            var isSelected = false;

            foreach (var range in _selected)
            {
                if (range.Contains(index))
                {
                    isSelected = true;
                    break;
                }
            }

            return isSelected;
        }

        // True  -> Selected
        // False -> Not Selected
        // Null  -> Some descendents are selected and some are not
        public bool? IsSelectedWithPartial()
        {
            var isSelected = (bool?)false;

            if (_parent != null)
            {
                var parentsChildren = _parent._childrenNodes;

                var myIndexInParent = parentsChildren.IndexOf(this);
                
                if (myIndexInParent != -1)
                {
                    isSelected = _parent.IsSelectedWithPartial(myIndexInParent);
                }
            }

            return isSelected;
        }

        // True  -> Selected
        // False -> Not Selected
        // Null  -> Some descendents are selected and some are not
        public bool? IsSelectedWithPartial(int index)
        {
            SelectionState selectionState;

            if (_childrenNodes.Count == 0 || // no nodes realized
                _childrenNodes.Count <= index || // target node is not realized 
                _childrenNodes[index] == null || // target node is not realized
                _childrenNodes[index] == _manager.SharedLeafNode)  // target node is a leaf node.
            {
                // Ask parent if the target node is selected.
                selectionState = IsSelected(index) ? SelectionState.Selected : SelectionState.NotSelected;
            }
            else
            {
                // targetNode is the node representing the index. This node is the parent. 
                // targetNode is a non-leaf node, containing one or many children nodes. Evaluate 
                // based on children of targetNode.
                var targetNode = _childrenNodes[index];
                selectionState = targetNode!.EvaluateIsSelectedBasedOnChildrenNodes();
            }

            return ConvertToNullableBool(selectionState);
        }

        public int SelectedIndex
        {
            get => SelectedCount > 0 ? SelectedIndices[0] : -1;
            set
            {
                if (IsValidIndex(value) && (SelectedCount != 1 || !IsSelected(value)))
                {
                    ClearSelection();

                    if (value != -1)
                    {
                        Select(value, true);
                    }
                }
            }
        }

        public List<int> SelectedIndices
        {
            get
            {
                if (!_selectedIndicesCacheIsValid)
                {
                    _selectedIndicesCacheIsValid = true;
                    
                    foreach (var range in _selected)
                    {
                        for (int index = range.Begin; index <= range.End; index++)
                        {
                            // Avoid duplicates
                            if (!_selectedIndicesCached.Contains(index))
                            {
                                _selectedIndicesCached.Add(index);
                            }
                        }
                    }

                    // Sort the list for easy consumption
                    _selectedIndicesCached.Sort();
                }

                return _selectedIndicesCached;
            }
        }

        public IEnumerable<object> SelectedItems
        {
            get => SelectedIndices.Select(x => ItemsSourceView!.GetAt(x));
        }

        public void Dispose()
        {
            _childrenSubscription?.Dispose();
            ItemsSourceView?.Dispose();
            ClearChildNodes();
            UnhookCollectionChangedHandler();
        }

        public void BeginOperation()
        {
            if (_operation != null)
            {
                throw new AvaloniaInternalException("Selection operation already in progress.");
            }

            _operation = new SelectionNodeOperation(this);

            for (var i = 0; i < _childrenNodes.Count; ++i)
            {
                var child = _childrenNodes[i];

                if (child != null && child != _manager.SharedLeafNode)
                {
                    child.BeginOperation();
                }
            }
        }

        public void EndOperation(List<SelectionNodeOperation> changes)
        {
            if (_operation == null)
            {
                throw new AvaloniaInternalException("No selection operation in progress.");
            }

            if (_operation.HasChanges)
            {
                changes.Add(_operation);
            }

            _operation = null;

            for (var i = 0; i < _childrenNodes.Count; ++i)
            {
                var child = _childrenNodes[i];

                if (child != null && child != _manager.SharedLeafNode)
                {
                    child.EndOperation(changes);
                }
            }
        }

        public bool Cleanup()
        {
            var result = SelectedCount == 0;

            for (var i = 0; i < _childrenNodes.Count; ++i)
            {
                var child = _childrenNodes[i];

                if (child != null)
                {
                    if (child.Cleanup())
                    {
                        child.Dispose();
                        _childrenNodes[i] = null;
                    }
                    else
                    {
                        result = false;
                    }
                }
            }

            return result;
        }

        public bool Select(int index, bool select)
        {
            return Select(index, select, raiseOnSelectionChanged: true);
        }

        public bool ToggleSelect(int index)
        {
            return Select(index, !IsSelected(index));
        }

        public void SelectAll()
        {
            if (ItemsSourceView != null)
            {
                var size = ItemsSourceView.Count;
                
                if (size > 0)
                {
                    SelectRange(new IndexRange(0, size - 1), select: true);
                }
            }
        }

        public void Clear() => ClearSelection();

        public bool SelectRange(IndexRange range, bool select)
        {
            if (IsValidIndex(range.Begin) && IsValidIndex(range.End))
            {
                if (select)
                {
                    AddRange(range, raiseOnSelectionChanged: true);
                }
                else
                {
                    RemoveRange(range, raiseOnSelectionChanged: true);
                }

                return true;
            }

            return false;
        }

        private void HookupCollectionChangedHandler()
        {
            if (ItemsSourceView != null)
            {
                ItemsSourceView.CollectionChanged += OnSourceListChanged;
            }
        }

        private void UnhookCollectionChangedHandler()
        {
            if (ItemsSourceView != null)
            {
                ItemsSourceView.CollectionChanged -= OnSourceListChanged;
            }
        }

        private bool IsValidIndex(int index)
        {
            return ItemsSourceView == null || (index >= 0 && index < ItemsSourceView.Count);
        }

        private void AddRange(IndexRange addRange, bool raiseOnSelectionChanged)
        {
            var selected = new List<IndexRange>();

            SelectedCount += IndexRange.Add(_selected, addRange, selected);

            if (selected.Count > 0)
            {
                _operation?.Selected(selected);

                if (_selectedItems != null && ItemsSourceView != null)
                {
                    for (var i = addRange.Begin; i <= addRange.End; ++i)
                    {
                        _selectedItems.Add(ItemsSourceView!.GetAt(i));
                    }
                }

                if (raiseOnSelectionChanged)
                {
                    OnSelectionChanged();
                }
            }
        }

        private void RemoveRange(IndexRange removeRange, bool raiseOnSelectionChanged)
        {
            var removed = new List<IndexRange>();

            SelectedCount -= IndexRange.Remove(_selected, removeRange, removed);

            if (removed.Count > 0)
            {
                _operation?.Deselected(removed);

                if (_selectedItems != null)
                {
                    for (var i = removeRange.Begin; i <= removeRange.End; ++i)
                    {
                        _selectedItems.Remove(ItemsSourceView!.GetAt(i));
                    }
                }

                if (raiseOnSelectionChanged)
                {
                    OnSelectionChanged();
                }
            }
        }

        private void ClearSelection()
        {
            // Deselect all items
            if (_selected.Count > 0)
            {
                _operation?.Deselected(_selected);
                _selected.Clear();
                OnSelectionChanged();
            }

            _selectedItems?.Clear();
            SelectedCount = 0;
            AnchorIndex = -1;
        }

        private void ClearChildNodes()
        {
            for (int i = 0; i < _childrenNodes.Count; i++)
            {
                var child = _childrenNodes[i];

                if (child != null && child != _manager.SharedLeafNode)
                {
                    child.Dispose();
                    _childrenNodes[i] = null;
                }
            }

            RealizedChildrenNodeCount = 0;
        }

        private bool Select(int index, bool select, bool raiseOnSelectionChanged)
        {
            if (IsValidIndex(index))
            {
                // Ignore duplicate selection calls
                if (IsSelected(index) == select)
                {
                    return true;
                }

                var range = new IndexRange(index, index);

                if (select)
                {
                    AddRange(range, raiseOnSelectionChanged);
                }
                else
                {
                    RemoveRange(range, raiseOnSelectionChanged);
                }

                return true;
            }

            return false;
        }

        private void OnSourceListChanged(object dataSource, NotifyCollectionChangedEventArgs args)
        {
            bool selectionInvalidated = false;
            List<object?>? removed = null;

            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                {
                    selectionInvalidated = OnItemsAdded(args.NewStartingIndex, args.NewItems.Count);
                    break;
                }

                case NotifyCollectionChangedAction.Remove:
                {
                    (selectionInvalidated, removed) = OnItemsRemoved(args.OldStartingIndex, args.OldItems);
                    break;
                }

                case NotifyCollectionChangedAction.Reset:
                    {
                        if (_selectedItems == null)
                        {
                            ClearSelection();
                        }
                        else
                        {
                            removed = RecreateSelectionFromSelectedItems();
                        }

                        selectionInvalidated = true;
                        break;
                    }

                case NotifyCollectionChangedAction.Replace:
                {
                    (selectionInvalidated, removed) = OnItemsRemoved(args.OldStartingIndex, args.OldItems);
                    selectionInvalidated |= OnItemsAdded(args.NewStartingIndex, args.NewItems.Count);
                    break;
                }
            }

            if (selectionInvalidated)
            {
                OnSelectionChanged();
            }

            _manager.OnSelectionInvalidatedDueToCollectionChange(selectionInvalidated, removed);
        }

        private bool OnItemsAdded(int index, int count)
        {
            var selectionInvalidated = false;
            
            // Update ranges for leaf items
            var toAdd = new List<IndexRange>();

            for (int i = 0; i < _selected.Count; i++)
            {
                var range = _selected[i];

                // The range is after the inserted items, need to shift the range right
                if (range.End >= index)
                {
                    int begin = range.Begin;
                    
                    // If the index left of newIndex is inside the range,
                    // Split the range and remember the left piece to add later
                    if (range.Contains(index - 1))
                    {
                        range.Split(index - 1, out var before, out _);
                        toAdd.Add(before);
                        begin = index;
                    }

                    // Shift the range to the right
                    _selected[i] = new IndexRange(begin + count, range.End + count);
                    selectionInvalidated = true;
                }
            }

            // Add the left sides of the split ranges
            _selected.AddRange(toAdd);

            // Update for non-leaf if we are tracking non-leaf nodes
            if (_childrenNodes.Count > 0)
            {
                selectionInvalidated = true;
                for (int i = 0; i < count; i++)
                {
                    _childrenNodes.Insert(index, null);
                }
            }

            // Adjust the anchor
            if (AnchorIndex >= index)
            {
                AnchorIndex += count;
            }

            // Check if adding a node invalidated an ancestors
            // selection state. For example if parent was selected before
            // adding a new item makes the parent partially selected now.
            if (!selectionInvalidated)
            {
                var parent = _parent;
                
                while (parent != null)
                {
                    var isSelected = parent.IsSelectedWithPartial();
                    
                    // If a parent is selected, then it will become partially selected.
                    // If it is not selected or partially selected - there is no change.
                    if (isSelected == true)
                    {
                        selectionInvalidated = true;
                        break;
                    }

                    parent = parent._parent;
                }
            }

            return selectionInvalidated;
        }

        private (bool, List<object?>) OnItemsRemoved(int index, IList items)
        {
            var selectionInvalidated = false;
            var removed = new List<object?>();
            var count = items.Count;
            var isSelected = false;

            for (int i = 0; i <= count - 1; i++)
            {
                if (IsSelected(index + i))
                {
                    isSelected = true;
                    removed.Add(items[i]);
                }
            }

            if (isSelected)
            {
                var removeRange = new IndexRange(index, index + count - 1);
                SelectedCount -= IndexRange.Remove(_selected, removeRange);
                selectionInvalidated = true;

                if (_selectedItems != null)
                {
                    foreach (var i in items)
                    {
                        _selectedItems.Remove(i);
                    }
                }
            }

            for (int i = 0; i < _selected.Count; i++)
            {
                var range = _selected[i];

                // The range is after the removed items, need to shift the range left
                if (range.End > index)
                {
                    // Shift the range to the left
                    _selected[i] = new IndexRange(range.Begin - count, range.End - count);
                    selectionInvalidated = true;
                }
            }

            // Update for non-leaf if we are tracking non-leaf nodes
            if (_childrenNodes.Count > 0)
            {
                selectionInvalidated = true;
                for (int i = 0; i < count; i++)
                {
                    if (_childrenNodes[index] != null)
                    {
                        removed.AddRange(_childrenNodes[index]!.SelectedItems);
                        RealizedChildrenNodeCount--;
                        _childrenNodes[index]!.Dispose();
                    }
                    _childrenNodes.RemoveAt(index);
                }
            }

            //Adjust the anchor
            if (AnchorIndex >= index)
            {
                AnchorIndex -= count;
            }

            return (selectionInvalidated, removed);
        }

        private void OnSelectionChanged()
        {
            _selectedIndicesCacheIsValid = false;
            _selectedIndicesCached.Clear();
        }

        public static bool? ConvertToNullableBool(SelectionState isSelected)
        {
            bool? result = null; // PartialySelected

            if (isSelected == SelectionState.Selected)
            {
                result = true;
            }
            else if (isSelected == SelectionState.NotSelected)
            {
                result = false;
            }

            return result;
        }

        public SelectionState EvaluateIsSelectedBasedOnChildrenNodes()
        {
            var selectionState = SelectionState.NotSelected;
            int realizedChildrenNodeCount = RealizedChildrenNodeCount;
            int selectedCount = SelectedCount;

            if (realizedChildrenNodeCount != 0 || selectedCount != 0)
            {
                // There are realized children or some selected leaves.
                int dataCount = DataCount;
                if (realizedChildrenNodeCount == 0 && selectedCount > 0)
                {
                    // All nodes are leaves under it - we didn't create children nodes as an optimization.
                    // See if all/some or none of the leaves are selected.
                    selectionState = dataCount != selectedCount ?
                        SelectionState.PartiallySelected :
                        dataCount == selectedCount ? SelectionState.Selected : SelectionState.NotSelected;
                }
                else
                {
                    // There are child nodes, walk them individually and evaluate based on each child
                    // being selected/not selected or partially selected.
                    selectedCount = 0;
                    int notSelectedCount = 0;
                    for (int i = 0; i < ChildrenNodeCount; i++)
                    {
                        var child = GetAt(i, false, default);

                        if (child != null)
                        {
                            // child is realized, ask it.
                            var isChildSelected = IsSelectedWithPartial(i);
                            if (isChildSelected == null)
                            {
                                selectionState = SelectionState.PartiallySelected;
                                break;
                            }
                            else if (isChildSelected == true)
                            {
                                selectedCount++;
                            }
                            else
                            {
                                notSelectedCount++;
                            }
                        }
                        else
                        {
                            // not realized.
                            if (IsSelected(i))
                            {
                                selectedCount++;
                            }
                            else
                            {
                                notSelectedCount++;
                            }
                        }

                        if (selectedCount > 0 && notSelectedCount > 0)
                        {
                            selectionState = SelectionState.PartiallySelected;
                            break;
                        }
                    }

                    if (selectionState != SelectionState.PartiallySelected)
                    {
                        if (selectedCount != 0 && selectedCount != dataCount)
                        {
                            selectionState = SelectionState.PartiallySelected;
                        }
                        else
                        {
                            selectionState = selectedCount == dataCount ? SelectionState.Selected : SelectionState.NotSelected;
                        }
                    }
                }
            }

            return selectionState;
        }

        private void PopulateSelectedItemsFromSelectedIndices()
        {
            if (_selectedItems != null)
            {
                _selectedItems.Clear();

                foreach (var i in SelectedIndices)
                {
                    _selectedItems.Add(ItemsSourceView!.GetAt(i));
                }
            }
        }

        private List<object?> RecreateSelectionFromSelectedItems()
        {
            var removed = new List<object?>();

            _selected.Clear();
            SelectedCount = 0;

            for (var i = 0; i < _selectedItems!.Count; ++i)
            {
                var item = _selectedItems[i];
                var index = ItemsSourceView!.IndexOf(item);

                if (index != -1)
                {
                    IndexRange.Add(_selected, new IndexRange(index, index));
                    ++SelectedCount;
                }
                else
                {
                    removed.Add(item);
                    _selectedItems.RemoveAt(i--);
                }
            }

            return removed;
        }

        public enum SelectionState
        {
            Selected,
            NotSelected,
            PartiallySelected
        }
    }
}
