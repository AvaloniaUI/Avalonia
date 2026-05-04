using System;
using System.Collections.Generic;
using Avalonia.Collections.Pooled;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition;

internal sealed class CompositionHitTestAabbTree
{
    internal interface IQueryHitTester
    {
        CompositionVisual? HitTest(CompositionVisual visual);
    }

    private const int Null = -1;
    private const double FatBoundsPadding = 1;
    private static readonly CandidateComparer s_candidateComparer = new();

    internal static bool IsEnabled { get; } = InitializeIsEnabled();

    private readonly Dictionary<CompositionVisual, int> _leaves = [];
    private readonly Dictionary<CompositionVisual, int> _unbounded = [];
    private readonly List<Node> _nodes = [];
    private readonly List<Candidate> _queryCandidates = [];
    private int[] _queryStack = [];
    private int _root = Null;
    private int _freeList = Null;

    private static bool InitializeIsEnabled() => !AppContext.TryGetSwitch("Avalonia.Rendering.Composition.DisableHitTestAabbTree", out var disabled) || !disabled;

    public void Clear()
    {
        _leaves.Clear();
        _unbounded.Clear();
        _nodes.Clear();
        _queryCandidates.Clear();
        _root = Null;
        _freeList = Null;
    }

    public void Rebuild(CompositionVisualCollection children)
    {
        Clear();

        for (var i = 0; i < children.Count; i++)
            Add(children[i], i);
    }

    public bool Update(CompositionVisual visual)
    {
        if (_leaves.TryGetValue(visual, out var leaf))
        {
            var order = _nodes[leaf].Order;
            var state = GetBoundsState(visual, out var bounds);

            if (state == BoundsState.Bounded)
            {
                MoveLeaf(leaf, bounds);
                SetOrder(leaf, order);
            }
            else
            {
                DestroyLeaf(visual, leaf);
                if (state == BoundsState.Unbounded)
                    _unbounded[visual] = order;
            }

            return true;
        }

        if (_unbounded.TryGetValue(visual, out var unboundedOrder))
        {
            var state = GetBoundsState(visual, out var bounds);

            if (state == BoundsState.Unbounded)
            {
                return true;
            }

            _unbounded.Remove(visual);

            if (state == BoundsState.Bounded)
                CreateLeaf(visual, bounds, unboundedOrder);

            return true;
        }

        return false;
    }

    public void Query(Point point, PooledList<CompositionVisual> results)
    {
        _queryCandidates.Clear();

        if (_root != Null)
        {
            var stackCount = 0;
            PushQueryNode(ref stackCount, _root);

            while (stackCount > 0)
            {
                var nodeIndex = _queryStack[--stackCount];
                var node = _nodes[nodeIndex];

                if (!node.Bounds.Contains(point))
                    continue;

                if (node.IsLeaf)
                {
                    if (node.Visual != null)
                        _queryCandidates.Add(new Candidate(node.Visual, node.Order));
                }
                else
                {
                    PushQueryNode(ref stackCount, node.Child1);
                    PushQueryNode(ref stackCount, node.Child2);
                }
            }
        }

        foreach (var candidate in _unbounded)
            _queryCandidates.Add(new Candidate(candidate.Key, candidate.Value));

        _queryCandidates.Sort(s_candidateComparer);

        foreach (var candidate in _queryCandidates)
            results.Add(candidate.Visual);

        _queryCandidates.Clear();
    }

    public CompositionVisual? QueryFirst<T>(Point point, ref T hitTest)
        where T : struct, IQueryHitTester
    {
        _queryCandidates.Clear();

        if (_root != Null)
        {
            var stackCount = 0;
            PushQueryNode(ref stackCount, _root);

            while (stackCount > 0)
            {
                var nodeIndex = _queryStack[--stackCount];
                var node = _nodes[nodeIndex];

                if (!node.Bounds.Contains(point))
                    continue;

                if (node.IsLeaf)
                {
                    if (node.Visual != null)
                        _queryCandidates.Add(new Candidate(node.Visual, node.Order));
                }
                else
                {
                    PushQueryNode(ref stackCount, node.Child1);
                    PushQueryNode(ref stackCount, node.Child2);
                }
            }
        }

        foreach (var candidate in _unbounded)
            _queryCandidates.Add(new Candidate(candidate.Key, candidate.Value));

        _queryCandidates.Sort(s_candidateComparer);

        foreach (var candidate in _queryCandidates)
        {
            var hit = hitTest.HitTest(candidate.Visual);
            if (hit != null)
            {
                _queryCandidates.Clear();
                return hit;
            }
        }

        _queryCandidates.Clear();
        return null;
    }

    private void Add(CompositionVisual visual, int order)
    {
        var state = GetBoundsState(visual, out var bounds);

        if (state == BoundsState.Bounded)
            CreateLeaf(visual, bounds, order);
        else if (state == BoundsState.Unbounded)
            _unbounded[visual] = order;
    }

    private void PushQueryNode(ref int count, int nodeIndex)
    {
        if (nodeIndex == Null)
            return;

        if (count == _queryStack.Length)
            Array.Resize(ref _queryStack, Math.Max(16, _queryStack.Length * 2));

        _queryStack[count++] = nodeIndex;
    }

    private void CreateLeaf(CompositionVisual visual, LtrbRect bounds, int order)
    {
        var leaf = AllocateNode();
        var node = _nodes[leaf];
        node.Bounds = Fatten(bounds);
        node.Visual = visual;
        node.Order = order;
        node.Height = 0;
        _nodes[leaf] = node;
        _leaves[visual] = leaf;
        InsertLeaf(leaf);
    }

    private void DestroyLeaf(CompositionVisual visual, int leaf)
    {
        RemoveLeaf(leaf);
        FreeNode(leaf);
        _leaves.Remove(visual);
    }

    private void MoveLeaf(int leaf, LtrbRect bounds)
    {
        if (_nodes[leaf].Bounds.Contains(bounds))
            return;

        RemoveLeaf(leaf);

        var node = _nodes[leaf];
        node.Bounds = Fatten(bounds);
        _nodes[leaf] = node;

        InsertLeaf(leaf);
    }

    private void SetOrder(int nodeIndex, int order)
    {
        var node = _nodes[nodeIndex];
        node.Order = order;
        _nodes[nodeIndex] = node;
    }

    private int AllocateNode()
    {
        if (_freeList == Null)
        {
            _nodes.Add(new Node
            {
                Parent = Null,
                Child1 = Null,
                Child2 = Null,
                Next = Null
            });
            return _nodes.Count - 1;
        }

        var index = _freeList;
        var node = _nodes[index];
        _freeList = node.Next;
        node.Parent = Null;
        node.Child1 = Null;
        node.Child2 = Null;
        node.Next = Null;
        node.Height = 0;
        node.Visual = null;
        node.Order = 0;
        _nodes[index] = node;
        return index;
    }

    private void FreeNode(int index)
    {
        var node = _nodes[index];
        node.Next = _freeList;
        node.Parent = Null;
        node.Child1 = Null;
        node.Child2 = Null;
        node.Height = -1;
        node.Visual = null;
        node.Order = 0;
        _nodes[index] = node;
        _freeList = index;
    }

    private void InsertLeaf(int leaf)
    {
        if (_root == Null)
        {
            _root = leaf;
            var root = _nodes[_root];
            root.Parent = Null;
            _nodes[_root] = root;
            return;
        }

        var leafBounds = _nodes[leaf].Bounds;
        var sibling = FindBestSibling(leafBounds);
        var oldParent = _nodes[sibling].Parent;
        var newParent = AllocateNode();
        var parentNode = _nodes[newParent];
        parentNode.Parent = oldParent;
        parentNode.Bounds = leafBounds.Union(_nodes[sibling].Bounds);
        parentNode.Height = _nodes[sibling].Height + 1;
        parentNode.Child1 = sibling;
        parentNode.Child2 = leaf;
        parentNode.Visual = null;
        _nodes[newParent] = parentNode;

        var siblingNode = _nodes[sibling];
        siblingNode.Parent = newParent;
        _nodes[sibling] = siblingNode;

        var leafNode = _nodes[leaf];
        leafNode.Parent = newParent;
        _nodes[leaf] = leafNode;

        if (oldParent == Null)
        {
            _root = newParent;
        }
        else
        {
            var oldParentNode = _nodes[oldParent];
            if (oldParentNode.Child1 == sibling)
                oldParentNode.Child1 = newParent;
            else
                oldParentNode.Child2 = newParent;
            _nodes[oldParent] = oldParentNode;
        }

        FixAncestors(newParent);
    }

    private int FindBestSibling(LtrbRect leafBounds)
    {
        var index = _root;
        while (!_nodes[index].IsLeaf)
        {
            var node = _nodes[index];
            var child1 = node.Child1;
            var child2 = node.Child2;
            var area = Perimeter(node.Bounds);
            var combinedArea = Perimeter(node.Bounds.Union(leafBounds));
            var cost = 2 * combinedArea;
            var inheritanceCost = 2 * (combinedArea - area);

            var cost1 = GetInsertionCost(child1, leafBounds, inheritanceCost);
            var cost2 = GetInsertionCost(child2, leafBounds, inheritanceCost);

            if (cost < cost1 && cost < cost2)
                break;

            index = cost1 < cost2 ? child1 : child2;
        }

        return index;
    }

    private double GetInsertionCost(int nodeIndex, LtrbRect leafBounds, double inheritanceCost)
    {
        var node = _nodes[nodeIndex];
        var union = node.Bounds.Union(leafBounds);

        if (node.IsLeaf)
            return Perimeter(union) + inheritanceCost;

        return Perimeter(union) - Perimeter(node.Bounds) + inheritanceCost;
    }

    private void RemoveLeaf(int leaf)
    {
        if (leaf == _root)
        {
            _root = Null;
            return;
        }

        var leafNode = _nodes[leaf];
        var parent = leafNode.Parent;
        var parentNode = _nodes[parent];
        var grandParent = parentNode.Parent;
        var sibling = parentNode.Child1 == leaf ? parentNode.Child2 : parentNode.Child1;

        if (grandParent != Null)
        {
            var grandParentNode = _nodes[grandParent];
            if (grandParentNode.Child1 == parent)
                grandParentNode.Child1 = sibling;
            else
                grandParentNode.Child2 = sibling;
            _nodes[grandParent] = grandParentNode;

            var siblingNode = _nodes[sibling];
            siblingNode.Parent = grandParent;
            _nodes[sibling] = siblingNode;

            FreeNode(parent);
            FixAncestors(grandParent);
        }
        else
        {
            _root = sibling;
            var siblingNode = _nodes[sibling];
            siblingNode.Parent = Null;
            _nodes[sibling] = siblingNode;
            FreeNode(parent);
        }

        leafNode.Parent = Null;
        _nodes[leaf] = leafNode;
    }

    private void FixAncestors(int index)
    {
        while (index != Null)
        {
            index = Balance(index);

            var node = _nodes[index];
            var child1 = _nodes[node.Child1];
            var child2 = _nodes[node.Child2];
            node.Bounds = child1.Bounds.Union(child2.Bounds);
            node.Height = 1 + Math.Max(child1.Height, child2.Height);
            _nodes[index] = node;

            index = node.Parent;
        }
    }

    private int Balance(int indexA)
    {
        var a = _nodes[indexA];
        if (a.IsLeaf || a.Height < 2)
            return indexA;

        var indexB = a.Child1;
        var indexC = a.Child2;
        var b = _nodes[indexB];
        var c = _nodes[indexC];
        var balance = c.Height - b.Height;

        if (balance > 1)
            return RotateCUp(indexA, indexB, indexC);

        if (balance < -1)
            return RotateBUp(indexA, indexB, indexC);

        return indexA;
    }

    private int RotateCUp(int indexA, int indexB, int indexC)
    {
        var a = _nodes[indexA];
        var c = _nodes[indexC];
        var indexF = c.Child1;
        var indexG = c.Child2;
        var f = _nodes[indexF];
        var g = _nodes[indexG];

        c.Child1 = indexA;
        c.Parent = a.Parent;
        a.Parent = indexC;

        ReplaceParentChild(indexA, indexC, c.Parent);

        if (f.Height > g.Height)
        {
            c.Child2 = indexF;
            a.Child2 = indexG;
            g.Parent = indexA;
            _nodes[indexG] = g;
            a.Bounds = _nodes[indexB].Bounds.Union(g.Bounds);
            c.Bounds = a.Bounds.Union(f.Bounds);
            a.Height = 1 + Math.Max(_nodes[indexB].Height, g.Height);
            c.Height = 1 + Math.Max(a.Height, f.Height);
        }
        else
        {
            c.Child2 = indexG;
            a.Child2 = indexF;
            f.Parent = indexA;
            _nodes[indexF] = f;
            a.Bounds = _nodes[indexB].Bounds.Union(f.Bounds);
            c.Bounds = a.Bounds.Union(g.Bounds);
            a.Height = 1 + Math.Max(_nodes[indexB].Height, f.Height);
            c.Height = 1 + Math.Max(a.Height, g.Height);
        }

        _nodes[indexA] = a;
        _nodes[indexC] = c;
        return indexC;
    }

    private int RotateBUp(int indexA, int indexB, int indexC)
    {
        var a = _nodes[indexA];
        var b = _nodes[indexB];
        var indexD = b.Child1;
        var indexE = b.Child2;
        var d = _nodes[indexD];
        var e = _nodes[indexE];

        b.Child1 = indexA;
        b.Parent = a.Parent;
        a.Parent = indexB;

        ReplaceParentChild(indexA, indexB, b.Parent);

        if (d.Height > e.Height)
        {
            b.Child2 = indexD;
            a.Child1 = indexE;
            e.Parent = indexA;
            _nodes[indexE] = e;
            a.Bounds = _nodes[indexC].Bounds.Union(e.Bounds);
            b.Bounds = a.Bounds.Union(d.Bounds);
            a.Height = 1 + Math.Max(_nodes[indexC].Height, e.Height);
            b.Height = 1 + Math.Max(a.Height, d.Height);
        }
        else
        {
            b.Child2 = indexE;
            a.Child1 = indexD;
            d.Parent = indexA;
            _nodes[indexD] = d;
            a.Bounds = _nodes[indexC].Bounds.Union(d.Bounds);
            b.Bounds = a.Bounds.Union(e.Bounds);
            a.Height = 1 + Math.Max(_nodes[indexC].Height, d.Height);
            b.Height = 1 + Math.Max(a.Height, e.Height);
        }

        _nodes[indexA] = a;
        _nodes[indexB] = b;
        return indexB;
    }

    private void ReplaceParentChild(int oldChild, int newChild, int parent)
    {
        if (parent == Null)
        {
            _root = newChild;
            return;
        }

        var parentNode = _nodes[parent];
        if (parentNode.Child1 == oldChild)
            parentNode.Child1 = newChild;
        else
            parentNode.Child2 = newChild;
        _nodes[parent] = parentNode;
    }

    private static BoundsState GetBoundsState(CompositionVisual visual, out LtrbRect bounds)
    {
        bounds = default;

        var readback = visual.TryGetValidReadback();
        if (readback == null)
            return BoundsState.Empty;

        if (visual.DisableSubTreeBoundsHitTestOptimization)
            return BoundsState.Unbounded;

        if (readback.TransformedSubtreeBounds is not { } subtreeBounds || subtreeBounds.IsZeroSize)
            return BoundsState.Empty;

        bounds = subtreeBounds;
        return BoundsState.Bounded;
    }

    private static LtrbRect Fatten(LtrbRect bounds) =>
        new(bounds.Left - FatBoundsPadding,
            bounds.Top - FatBoundsPadding,
            bounds.Right + FatBoundsPadding,
            bounds.Bottom + FatBoundsPadding);

    private static double Perimeter(LtrbRect bounds) => 2 * (bounds.Width + bounds.Height);

    private enum BoundsState
    {
        Empty,
        Bounded,
        Unbounded
    }

    private struct Node
    {
        public LtrbRect Bounds;
        public CompositionVisual? Visual;
        public int Parent;
        public int Child1;
        public int Child2;
        public int Next;
        public int Height;
        public int Order;

        public readonly bool IsLeaf => Child1 == Null;
    }

    private readonly struct Candidate(CompositionVisual visual, int order)
    {
        public CompositionVisual Visual { get; } = visual;
        public int Order { get; } = order;
    }

    private sealed class CandidateComparer : IComparer<Candidate>
    {
        public int Compare(Candidate left, Candidate right) => right.Order.CompareTo(left.Order);
    }
}
