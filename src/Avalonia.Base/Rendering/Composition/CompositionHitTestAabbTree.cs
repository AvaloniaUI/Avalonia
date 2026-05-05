using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Collections.Pooled;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition;

internal sealed class CompositionHitTestAabbTree
{
    private const int Null = -1;
    private const int OrderBucketSize = 32;
    private const double FatBoundsPadding = 1;
    private static readonly CandidateComparer s_candidateComparer = new();

    private readonly Dictionary<CompositionVisual, Entry> _entries = [];
    private readonly List<Bucket> _buckets = [];
    private readonly List<Node> _nodes = [];
    private int _freeList = Null;

    public CompositionHitTestAabbTree(CompositionVisualCollection children)
    {
        for (var i = 0; i < children.Count; i++)
            Update(children[i], i);
    }

    public void Clear()
    {
        _entries.Clear();
        _buckets.Clear();
        _nodes.Clear();
        _freeList = Null;
    }

    public void Update(CompositionVisual visual, int order)
    {
        ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_entries, visual, out var exists);
        if (!exists)
            entry = new Entry(order);

        var oldOrder = entry.Order;
        entry.Order = order;

        if (entry.Leaf != Null)
        {
            var state = GetBoundsState(visual, out var bounds);

            if (state == BoundsState.Bounded)
            {
                UpdateLeaf(entry.Leaf, bounds, order);
            }
            else
            {
                DestroyLeaf(entry.Leaf);
                entry.Leaf = Null;
                if (state == BoundsState.Unbounded)
                    AddUnbounded(visual, order, ref entry);
            }

            return;
        }

        if (entry.IsUnbounded)
        {
            var state = GetBoundsState(visual, out var bounds);

            if (state == BoundsState.Unbounded)
            {
                MoveUnbounded(visual, oldOrder, order);
                return;
            }

            RemoveUnbounded(visual, oldOrder);
            entry.IsUnbounded = false;

            if (state == BoundsState.Bounded)
                entry.Leaf = CreateLeaf(visual, bounds, order);

            return;
        }

        Add(visual, order, ref entry);
    }

    public void UpdateBounds(CompositionVisual visual)
    {
        if (_entries.TryGetValue(visual, out var entry))
            Update(visual, entry.Order);
    }

    public void Remove(CompositionVisual visual)
    {
        if (!_entries.Remove(visual, out var entry))
            return;

        if (entry.Leaf != Null)
        {
            DestroyLeaf(entry.Leaf);
            return;
        }

        if (entry.IsUnbounded)
            RemoveUnbounded(visual, entry.Order);
    }

    public void UpdateOrder(CompositionVisual visual, int order)
    {
        ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_entries, visual, out var exists);
        if (!exists)
        {
            entry = new Entry(order);
            return;
        }

        var oldOrder = entry.Order;
        entry.Order = order;

        if (entry.Leaf != Null)
        {
            MoveLeafToOrder(entry.Leaf, order);
            return;
        }

        if (entry.IsUnbounded)
            MoveUnbounded(visual, oldOrder, order);
    }

    public void Query(Point point, PooledList<CompositionVisual> results)
    {
        var candidates = ArrayPool<Candidate>.Shared.Rent(OrderBucketSize);
        var stack = ArrayPool<int>.Shared.Rent(16);
        var candidateCount = 0;

        try
        {
            for (var i = _buckets.Count - 1; i >= 0; i--)
            {
                var bucket = _buckets[i];
                if (bucket.IsEmpty)
                    continue;

                candidateCount = 0;
                var stackCount = 0;
                QueryBucket(bucket, point, ref candidates, ref candidateCount, ref stack, ref stackCount);
                Array.Sort(candidates, 0, candidateCount, s_candidateComparer);

                for (var j = 0; j < candidateCount; j++)
                    results.Add(candidates[j].Visual);

                Array.Clear(candidates, 0, candidateCount);
            }
        }
        finally
        {
            Array.Clear(candidates, 0, candidateCount);
            ArrayPool<Candidate>.Shared.Return(candidates);
            ArrayPool<int>.Shared.Return(stack);
        }
    }

    public CompositionVisual? QueryFirst(CompositionTarget target, Point point, Func<CompositionVisual, bool>? filter, Func<CompositionVisual, bool>? resultFilter)
    {
        var candidates = ArrayPool<Candidate>.Shared.Rent(OrderBucketSize);
        var stack = ArrayPool<int>.Shared.Rent(16);
        var candidateCount = 0;

        try
        {
            for (var i = _buckets.Count - 1; i >= 0; i--)
            {
                var bucket = _buckets[i];
                if (bucket.IsEmpty)
                    continue;

                candidateCount = 0;
                var stackCount = 0;
                QueryBucket(bucket, point, ref candidates, ref candidateCount, ref stack, ref stackCount);
                Array.Sort(candidates, 0, candidateCount, s_candidateComparer);

                for (var j = 0; j < candidateCount; j++)
                {
                    var hit = target.HitTestFirstCore(candidates[j].Visual, point, filter, resultFilter);
                    if (hit != null)
                        return hit;
                }

                Array.Clear(candidates, 0, candidateCount);
            }
        }
        finally
        {
            Array.Clear(candidates, 0, candidateCount);
            ArrayPool<Candidate>.Shared.Return(candidates);
            ArrayPool<int>.Shared.Return(stack);
        }

        return null;
    }

    private void Add(CompositionVisual visual, int order, ref Entry entry)
    {
        var state = GetBoundsState(visual, out var bounds);

        if (state == BoundsState.Bounded)
            entry.Leaf = CreateLeaf(visual, bounds, order);
        else if (state == BoundsState.Unbounded)
            AddUnbounded(visual, order, ref entry);
    }

    private void QueryBucket(Bucket bucket, Point point, ref Candidate[] candidates, ref int candidateCount, ref int[] stack, ref int stackCount)
    {
        PushQueryNode(ref stack, ref stackCount, bucket.Root);

        while (stackCount > 0)
        {
            var nodeIndex = stack[--stackCount];
            var node = _nodes[nodeIndex];

            if (!node.Bounds.Contains(point))
                continue;

            if (node.IsLeaf)
            {
                if (node.Visual != null)
                    AddCandidate(ref candidates, ref candidateCount, new Candidate(node.Visual, node.Order));
            }
            else
            {
                PushQueryNode(ref stack, ref stackCount, node.Child1);
                PushQueryNode(ref stack, ref stackCount, node.Child2);
            }
        }

        if (bucket.Unbounded != null)
            foreach (var visual in bucket.Unbounded)
                if (_entries.TryGetValue(visual, out var entry))
                    AddCandidate(ref candidates, ref candidateCount, new Candidate(visual, entry.Order));
    }

    private static void AddCandidate(ref Candidate[] candidates, ref int count, Candidate candidate)
    {
        if (count == candidates.Length)
            Resize(ref candidates, count);

        candidates[count++] = candidate;
    }

    private static void PushQueryNode(ref int[] stack, ref int count, int nodeIndex)
    {
        if (nodeIndex == Null)
            return;

        if (count == stack.Length)
            Resize(ref stack, count);

        stack[count++] = nodeIndex;
    }

    private static void Resize<T>(ref T[] buffer, int count)
    {
        var resized = ArrayPool<T>.Shared.Rent(buffer.Length * 2);
        Array.Copy(buffer, resized, count);
        ArrayPool<T>.Shared.Return(buffer, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        buffer = resized;
    }

    private static int GetBucketIndex(int order)
    {
        if (order <= 0)
            return 0;
        return order / OrderBucketSize;
    }

    private ref Bucket GetOrCreateBucket(int bucketIndex)
    {
        while (_buckets.Count <= bucketIndex)
            _buckets.Add(new Bucket(Null));

        return ref GetRef(_buckets, bucketIndex);
    }

    private void RemoveBucketIfEmpty(int bucketIndex)
    {
        if (bucketIndex >= _buckets.Count)
            return;

        bool removeTrailingBucket;
        {
            ref var bucket = ref GetRef(_buckets, bucketIndex);
            if (bucket.Unbounded?.Count == 0)
                bucket.Unbounded = null;

            removeTrailingBucket = bucket.IsEmpty && bucketIndex == _buckets.Count - 1;
        }

        if (!removeTrailingBucket)
            return;

        do
        {
            _buckets.RemoveAt(_buckets.Count - 1);
        } while (_buckets.Count > 0 && _buckets[^1].IsEmpty);
    }

    private static ref T GetRef<T>(List<T> items, int index) => ref CollectionsMarshal.AsSpan(items)[index];

    private int CreateLeaf(CompositionVisual visual, LtrbRect bounds, int order)
    {
        var leaf = AllocateNode();
        {
            ref var node = ref GetRef(_nodes, leaf);
            node.Bounds = Fatten(bounds);
            node.Visual = visual;
            node.Order = order;
            node.Bucket = GetBucketIndex(order);
            node.Height = 0;
        }

        InsertLeaf(leaf);
        return leaf;
    }

    private void DestroyLeaf(int leaf)
    {
        RemoveLeaf(leaf);
        FreeNode(leaf);
    }

    private void UpdateLeaf(int leaf, LtrbRect bounds, int order)
    {
        var bucket = GetBucketIndex(order);

        {
            ref var node = ref GetRef(_nodes, leaf);

            // If the exact bounds still fit inside the fat bounds, the tree shape can stay unchanged.
            if (node.Bucket == bucket && node.Bounds.Contains(bounds))
            {
                node.Order = order;
                return;
            }
        }

        RemoveLeaf(leaf);

        {
            ref var removedNode = ref GetRef(_nodes, leaf);
            removedNode.Bounds = Fatten(bounds);
            removedNode.Bucket = bucket;
            removedNode.Order = order;
        }

        InsertLeaf(leaf);
    }

    private void MoveLeafToOrder(int leaf, int order)
    {
        var bucket = GetBucketIndex(order);

        {
            ref var node = ref GetRef(_nodes, leaf);
            if (node.Bucket == bucket)
            {
                node.Order = order;
                return;
            }
        }

        RemoveLeaf(leaf);

        {
            ref var removedNode = ref GetRef(_nodes, leaf);
            removedNode.Order = order;
            removedNode.Bucket = bucket;
        }

        InsertLeaf(leaf);
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
        ref var node = ref GetRef(_nodes, index);
        _freeList = node.Next;
        node.Parent = Null;
        node.Child1 = Null;
        node.Child2 = Null;
        node.Next = Null;
        node.Height = 0;
        node.Visual = null;
        node.Order = 0;
        node.Bucket = 0;
        return index;
    }

    private void FreeNode(int index)
    {
        ref var node = ref GetRef(_nodes, index);
        node.Next = _freeList;
        node.Parent = Null;
        node.Child1 = Null;
        node.Child2 = Null;
        node.Height = -1;
        node.Visual = null;
        node.Order = 0;
        node.Bucket = 0;
        _freeList = index;
    }

    private void InsertLeaf(int leaf)
    {
        var bucketIndex = GetRef(_nodes, leaf).Bucket;
        ref var bucket = ref GetOrCreateBucket(bucketIndex);

        if (bucket.Root == Null)
        {
            bucket.Root = leaf;
            ref var root = ref GetRef(_nodes, leaf);
            root.Parent = Null;
            return;
        }

        var leafBounds = GetRef(_nodes, leaf).Bounds;
        var sibling = FindBestSibling(bucket.Root, leafBounds);
        var oldParent = GetRef(_nodes, sibling).Parent;
        var newParent = AllocateNode();

        // Insert by replacing the chosen sibling with a new internal parent:
        //
        // Before: oldParent        After: oldParent
        //             |                       |
        //          sibling                newParent
        //                                  /      \
        //                             sibling    leaf
        ref var parentNode = ref GetRef(_nodes, newParent);
        parentNode.Parent = oldParent;
        parentNode.Bounds = leafBounds.Union(GetRef(_nodes, sibling).Bounds);
        parentNode.Height = GetRef(_nodes, sibling).Height + 1;
        parentNode.Child1 = sibling;
        parentNode.Child2 = leaf;
        parentNode.Visual = null;
        parentNode.Bucket = bucketIndex;

        ref var siblingNode = ref GetRef(_nodes, sibling);
        siblingNode.Parent = newParent;

        ref var leafNode = ref GetRef(_nodes, leaf);
        leafNode.Parent = newParent;

        if (oldParent == Null)
        {
            bucket.Root = newParent;
        }
        else
        {
            ref var oldParentNode = ref GetRef(_nodes, oldParent);
            if (oldParentNode.Child1 == sibling)
                oldParentNode.Child1 = newParent;
            else
                oldParentNode.Child2 = newParent;
        }

        FixAncestors(newParent);
    }

    private int FindBestSibling(int root, LtrbRect leafBounds)
    {
        var index = root;
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

            // Stop descending when pairing with this internal node is already cheaper.
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
        var bucketIndex = GetRef(_nodes, leaf).Bucket;
        var removedRoot = false;
        {
            ref var bucket = ref GetRef(_buckets, bucketIndex);
            if (leaf == bucket.Root)
            {
                bucket.Root = Null;
                removedRoot = true;
            }
        }

        if (removedRoot)
        {
            RemoveBucketIfEmpty(bucketIndex);
            return;
        }

        var parent = GetRef(_nodes, leaf).Parent;
        var parentNode = GetRef(_nodes, parent);
        var grandParent = parentNode.Parent;
        var sibling = parentNode.Child1 == leaf ? parentNode.Child2 : parentNode.Child1;

        // Collapse the removed leaf's parent and promote the sibling.
        if (grandParent != Null)
        {
            // Before: grandParent        After: grandParent
            //             |                       |
            //           parent                 sibling
            //           /    \
            //        leaf  sibling
            ref var grandParentNode = ref GetRef(_nodes, grandParent);
            if (grandParentNode.Child1 == parent)
                grandParentNode.Child1 = sibling;
            else
                grandParentNode.Child2 = sibling;

            ref var siblingNode = ref GetRef(_nodes, sibling);
            siblingNode.Parent = grandParent;

            FreeNode(parent);
            FixAncestors(grandParent);
        }
        else
        {
            // If the parent was the root, the sibling becomes the new root.
            //
            // Before: parent(root)       After: sibling(root)
            //          /    \
            //       leaf  sibling
            {
                ref var bucket = ref GetRef(_buckets, bucketIndex);
                bucket.Root = sibling;
            }

            ref var siblingNode = ref GetRef(_nodes, sibling);
            siblingNode.Parent = Null;
            FreeNode(parent);
        }

        {
            ref var leafNode = ref GetRef(_nodes, leaf);
            leafNode.Parent = Null;
        }

        RemoveBucketIfEmpty(bucketIndex);
    }

    private void FixAncestors(int index)
    {
        while (index != Null)
        {
            index = Balance(index);

            ref var node = ref GetRef(_nodes, index);
            var child1 = GetRef(_nodes, node.Child1);
            var child2 = GetRef(_nodes, node.Child2);

            // Ancestor bounds always cover both children after insert/remove/rotate.
            node.Bounds = child1.Bounds.Union(child2.Bounds);
            node.Height = 1 + Math.Max(child1.Height, child2.Height);

            index = node.Parent;
        }
    }

    private int Balance(int indexA)
    {
        var a = GetRef(_nodes, indexA);
        if (a.IsLeaf || a.Height < 2)
            return indexA;

        var indexB = a.Child1;
        var indexC = a.Child2;
        var b = GetRef(_nodes, indexB);
        var c = GetRef(_nodes, indexC);
        var balance = c.Height - b.Height;

        // The right subtree is heavier than the left. Rotate C up.
        if (balance > 1)
            return RotateCUp(indexA, indexB, indexC);

        // The left subtree is heavier than the right. Rotate B up.
        if (balance < -1)
            return RotateBUp(indexA, indexB, indexC);

        return indexA;
    }

    private int RotateCUp(int indexA, int indexB, int indexC)
    {
        // Rotate C above A:
        //
        // Before:      A                   After, if F taller:  C
        //             / \                                      / \
        //            B   C                                    A   F
        //               / \                                  / \
        //              F   G                                B   G
        //
        //                                  After, otherwise:    C
        //                                                      / \
        //                                                     A   G
        //                                                    / \
        //                                                   B   F
        ref var a = ref GetRef(_nodes, indexA);
        ref var c = ref GetRef(_nodes, indexC);
        var indexF = c.Child1;
        var indexG = c.Child2;
        ref var f = ref GetRef(_nodes, indexF);
        ref var g = ref GetRef(_nodes, indexG);

        c.Child1 = indexA;
        c.Parent = a.Parent;
        a.Parent = indexC;

        // C takes A's old place in the parent chain.
        ReplaceParentChild(indexA, indexC, c.Parent);

        // Keep the taller C child with C, and move the other child under A.
        if (f.Height > g.Height)
        {
            c.Child2 = indexF;
            a.Child2 = indexG;
            g.Parent = indexA;
            a.Bounds = GetRef(_nodes, indexB).Bounds.Union(g.Bounds);
            c.Bounds = a.Bounds.Union(f.Bounds);
            a.Height = 1 + Math.Max(GetRef(_nodes, indexB).Height, g.Height);
            c.Height = 1 + Math.Max(a.Height, f.Height);
        }
        else
        {
            c.Child2 = indexG;
            a.Child2 = indexF;
            f.Parent = indexA;
            a.Bounds = GetRef(_nodes, indexB).Bounds.Union(f.Bounds);
            c.Bounds = a.Bounds.Union(g.Bounds);
            a.Height = 1 + Math.Max(GetRef(_nodes, indexB).Height, f.Height);
            c.Height = 1 + Math.Max(a.Height, g.Height);
        }

        return indexC;
    }

    private int RotateBUp(int indexA, int indexB, int indexC)
    {
        // Rotate B above A:
        //
        // Before:      A                  After, if D taller:   B
        //             / \                                      / \
        //            B   C                                    A   D
        //           / \                                      / \
        //          D   E                                    E   C
        //
        //                                 After, otherwise:     B
        //                                                      / \
        //                                                     A   E
        //                                                    / \
        //                                                   D   C
        ref var a = ref GetRef(_nodes, indexA);
        ref var b = ref GetRef(_nodes, indexB);
        var indexD = b.Child1;
        var indexE = b.Child2;
        ref var d = ref GetRef(_nodes, indexD);
        ref var e = ref GetRef(_nodes, indexE);

        b.Child1 = indexA;
        b.Parent = a.Parent;
        a.Parent = indexB;

        // B takes A's old place in the parent chain.
        ReplaceParentChild(indexA, indexB, b.Parent);

        // Keep the taller B child with B, and move the other child under A.
        if (d.Height > e.Height)
        {
            b.Child2 = indexD;
            a.Child1 = indexE;
            e.Parent = indexA;
            a.Bounds = GetRef(_nodes, indexC).Bounds.Union(e.Bounds);
            b.Bounds = a.Bounds.Union(d.Bounds);
            a.Height = 1 + Math.Max(GetRef(_nodes, indexC).Height, e.Height);
            b.Height = 1 + Math.Max(a.Height, d.Height);
        }
        else
        {
            b.Child2 = indexE;
            a.Child1 = indexD;
            d.Parent = indexA;
            a.Bounds = GetRef(_nodes, indexC).Bounds.Union(d.Bounds);
            b.Bounds = a.Bounds.Union(e.Bounds);
            a.Height = 1 + Math.Max(GetRef(_nodes, indexC).Height, d.Height);
            b.Height = 1 + Math.Max(a.Height, e.Height);
        }

        return indexB;
    }

    private void ReplaceParentChild(int oldChild, int newChild, int parent)
    {
        if (parent == Null)
        {
            ref var bucket = ref GetOrCreateBucket(GetRef(_nodes, newChild).Bucket);
            bucket.Root = newChild;
            return;
        }

        ref var parentNode = ref GetRef(_nodes, parent);
        if (parentNode.Child1 == oldChild)
            parentNode.Child1 = newChild;
        else
            parentNode.Child2 = newChild;
    }

    private void AddUnbounded(CompositionVisual visual, int order, ref Entry entry)
    {
        var bucketIndex = GetBucketIndex(order);
        ref var bucket = ref GetOrCreateBucket(bucketIndex);
        (bucket.Unbounded ??= []).Add(visual);
        entry.IsUnbounded = true;
    }

    private void MoveUnbounded(CompositionVisual visual, int oldOrder, int order)
    {
        var oldBucketIndex = GetBucketIndex(oldOrder);
        var newBucketIndex = GetBucketIndex(order);

        if (oldBucketIndex == newBucketIndex)
            return;

        RemoveUnbounded(visual, oldOrder);
        ref var newBucket = ref GetOrCreateBucket(newBucketIndex);
        (newBucket.Unbounded ??= []).Add(visual);
    }

    private void RemoveUnbounded(CompositionVisual visual, int order)
    {
        var bucketIndex = GetBucketIndex(order);
        if (bucketIndex >= _buckets.Count)
            return;

        {
            ref var bucket = ref GetRef(_buckets, bucketIndex);
            if (bucket.Unbounded == null)
                return;

            bucket.Unbounded.Remove(visual);
        }

        RemoveBucketIfEmpty(bucketIndex);
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

    // Fatten the bounds by a small amount to avoid having to update the tree for every tiny movement.
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
        public int Bucket;

        public readonly bool IsLeaf => Child1 == Null;
    }

    private struct Bucket(int root)
    {
        public int Root = root;
        public List<CompositionVisual>? Unbounded = null;

        public readonly bool IsEmpty => Root == Null && (Unbounded == null || Unbounded.Count == 0);
    }

    private struct Entry(int order)
    {
        public int Order = order;
        public int Leaf = Null;
        public bool IsUnbounded;
    }

    private readonly struct Candidate(CompositionVisual visual, int order)
    {
        public CompositionVisual Visual { get; } = visual;
        public int Order { get; } = order;
    }

    private sealed class CandidateComparer : IComparer<Candidate>
    {
        // Higher child order is topmost, sort descending.
        public int Compare(Candidate left, Candidate right) => right.Order.CompareTo(left.Order);
    }
}
