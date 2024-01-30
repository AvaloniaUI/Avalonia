using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Utilities;

namespace Avalonia.Diagnostics.Controls.VirtualizedTreeView;

internal class FlatTree : IReadOnlyList<FlatTreeNode>,
    IList<FlatTreeNode>,
    IList,
    INotifyCollectionChanged,
    IWeakEventSubscriber<NotifyCollectionChangedEventArgs>,
    IWeakEventSubscriber<PropertyChangedEventArgs>
{
    private List<FlatTreeNode> _flatTree = new();

    // In order to keep expanded state in sync, we need to keep track of expanded nodes here, not by checking IsExpanded
    // Why: property changed can be fired even if IsExpanded is not changed
    // Why: if there is an event bound to IsExpanded changes that changes the children, then this change can be fired before IsExpanded property change
    // is fired in FlatTree
    private HashSet<ITreeNode> _expanded = new();

    public FlatTree(IEnumerable<ITreeNode> roots)
    {
        foreach (var root in roots)
        {
            InsertNode(root, 0, _flatTree.Count);
        }
    }

    /// <summary>
    /// Returns true if the given node is expanded in the flat tree
    /// (its children have been already added to the tree)
    /// </summary>
    /// <param name="node">The node to check whether is expanded</param>
    /// <returns>True if node is expanded</returns>
    private bool IsExpanded(ITreeNode node)
    {
        return _expanded.Contains(node);
    }

    private void SubscribeToNode(ITreeNode node)
    {
        WeakEvents.CollectionChanged.Subscribe(node, this);
        WeakEvents.ThreadSafePropertyChanged.Subscribe(node, this);
    }

    private void UnsubscribeFromNode(ITreeNode node)
    {
        _expanded.Remove(node);
        WeakEvents.CollectionChanged.Unsubscribe(node, this);
        WeakEvents.ThreadSafePropertyChanged.Unsubscribe(node, this);
    }

    /// <summary>
    /// Inserts an ITreeNode at the given level (indent) and index along with all expanded children.
    /// Then binds to the PropertyChanged and CollectionChanged events of the node to observer changes.
    /// </summary>
    /// <param name="node">Node to insert</param>
    /// <param name="level">Indent level for the given node</param>
    /// <param name="startIndex">Index to insert the node at</param>
    /// <returns>Number of inserted elements to the list</returns>
    private int InsertNode(ITreeNode node, int level, int startIndex)
    {
        int index = startIndex;
        var flatChild = new FlatTreeNode(node, level);
        _flatTree.Insert(index++, flatChild);

        SubscribeToNode(node);

        if (node.IsExpanded)
        {
            _expanded.Add(node);
            index += InsertChildren(flatChild, index);
        }

        return index - startIndex;
    }

    /// <summary>
    /// Inserts children of the node and all expanded children recursively starting at the given index
    /// </summary>
    /// <param name="parent">Parent node</param>
    /// <param name="startIndex">Index to insert the children at</param>
    /// <returns>Number of added nodes</returns>
    private int InsertChildren(FlatTreeNode parent, int startIndex)
    {
        int index = startIndex;
        foreach (var child in parent.Node.Children)
        {
            index += InsertNode(child, parent.Level + 1, index);
        }

        return index - startIndex;
    }

    /// <summary>
    /// Counts all expanded children of the given node recursively
    /// </summary>
    /// <param name="parent">Parent node</param>
    /// <param name="limit">If non null, counts only first `limit` children</param>
    /// <returns>Number of expanded children</returns>
    private int CountExpandedChildren(ITreeNode parent, int? limit = null)
    {
        int count = 0;
        var end = limit ?? parent.Children.Count;
        for (var index = 0; index < end; index++)
        {
            var child = parent.Children[index];
            count++;
            if (IsExpanded(child))
                count += CountExpandedChildren(child);
        }

        return count;
    }

    private int IndexOfNode(ITreeNode node)
    {
        for (int i = 0; i < _flatTree.Count; i++)
            if (ReferenceEquals(_flatTree[i].Node, node))
                return i;
        return -1;
    }

    public void OnEvent(object? sender, WeakEvent ev, PropertyChangedEventArgs e)
    {
        NodeOnPropertyChanged(sender, e);
    }

    private void NodeOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender == null)
            return;

        var node = (ITreeNode)sender;

        if (e.PropertyName != nameof(node.IsExpanded))
        {
            return;
        }

        var nodeIndex = IndexOfNode(node);
        var flatNode = _flatTree[nodeIndex];
        if (node.IsExpanded)
        {
            if (!_expanded.Add(node))
                return;

            var insertedItemsCount = InsertChildren(flatNode, nodeIndex + 1);
            var newItems = _flatTree.GetRange(nodeIndex + 1, insertedItemsCount);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems, nodeIndex + 1));
        }
        else
        {
            if (!_expanded.Remove(node))
                return;

            var removedItemsCount = CountExpandedChildren(node);
            var removedItems = _flatTree.GetRange(nodeIndex + 1, removedItemsCount);
            foreach (var item in removedItems)
            {
                UnsubscribeFromNode(item.Node);
            }

            _flatTree.RemoveRange(nodeIndex + 1, removedItemsCount);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems, nodeIndex + 1));
        }
    }

    public void OnEvent(object? sender, WeakEvent ev, NotifyCollectionChangedEventArgs e)
    {
        NodeChildrenChanged(sender, e);
    }

    private void NodeChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender == null)
            return;

        var parent = (ITreeNode)sender;
        var indexOfParent = IndexOfNode(parent);
        var flatParent = _flatTree[indexOfParent];

        if (!IsExpanded(parent))
            return;

        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            var startIndex = indexOfParent + 1 + CountExpandedChildren(parent, e.NewStartingIndex);
            var index = startIndex;
            for (int i = 0; i < e.NewItems!.Count; i++)
            {
                index += InsertNode(flatParent.Node.Children[e.NewStartingIndex + i], flatParent.Level + 1, index);
            }
            var count = index - startIndex;
            var newItems = _flatTree.GetRange(startIndex, count);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems, startIndex));
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            var startIndex = indexOfParent + 1 + CountExpandedChildren(parent, e.OldStartingIndex);
            var count = 0;
            for (int i = 0; i < e.OldItems!.Count; i++)
            {
                if (IsExpanded(_flatTree[startIndex + count].Node))
                    count += CountExpandedChildren(_flatTree[startIndex + count].Node);
                count++;
            }
            var removedItems = _flatTree.GetRange(startIndex, count);
            foreach (var item in removedItems)
            {
                UnsubscribeFromNode(item.Node);
            }
            _flatTree.RemoveRange(startIndex, count);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems, startIndex));
        }
        else
            throw new NotImplementedException();
    }

    public IEnumerator<FlatTreeNode> GetEnumerator() => _flatTree.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(FlatTreeNode item) => throw new InvalidOperationException();

    public int Add(object? value) => throw new InvalidOperationException();

    public void Clear() => throw new InvalidOperationException();

    public bool Contains(object? value) => _flatTree.Contains(value);

    public int IndexOf(object? value) => value is FlatTreeNode node ? _flatTree.IndexOf(node) : -1;

    public void Insert(int index, object? value) => throw new InvalidOperationException();

    public void Remove(object? value) => throw new InvalidOperationException();

    public bool Contains(FlatTreeNode item) => _flatTree.Contains(item);

    public void CopyTo(FlatTreeNode[] array, int arrayIndex) => _flatTree.CopyTo(array, arrayIndex);

    public bool Remove(FlatTreeNode item) => throw new InvalidOperationException();

    public void CopyTo(Array array, int index) => _flatTree.CopyTo((FlatTreeNode[])array, index);

    public int Count => _flatTree.Count;

    public bool IsSynchronized => false;

    public object SyncRoot => this;

    public bool IsReadOnly => true;

    object? IList.this[int index]
    {
        get => this[index];
        set => throw new InvalidOperationException();
    }

    public int IndexOf(FlatTreeNode item) => _flatTree.IndexOf(item);

    public void Insert(int index, FlatTreeNode item) => throw new InvalidOperationException();

    public void RemoveAt(int index) => throw new InvalidOperationException();

    public bool IsFixedSize => false;

    public FlatTreeNode this[int index]
    {
        get => _flatTree[index];
        set => throw new InvalidOperationException();
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
}
