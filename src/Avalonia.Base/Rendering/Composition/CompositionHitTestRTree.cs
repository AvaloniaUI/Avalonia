using System;
using System.Collections.Generic;
using Avalonia.Collections.Pooled;
using Avalonia.Platform;
using Avalonia.Rendering;

namespace Avalonia.Rendering.Composition;

internal readonly struct CompositionHitTestCandidate(CompositionVisual visual, int order)
{

    public CompositionVisual Visual { get; } = visual;
    public int Order { get; } = order;
}

internal sealed class CompositionHitTestRTree
{
    private const int MaxChildren = 8;
    private readonly List<Entry> _entries = new();
    private readonly List<CompositionHitTestCandidate> _unbounded = new();
    private Node? _root;
    private CompositionVisual? _indexedRoot;

    public ulong Revision { get; private set; }

    public bool IsCurrent(CompositionVisual root, ulong revision) =>
        ReferenceEquals(_indexedRoot, root) && Revision == revision;

    public void Rebuild(CompositionVisual? root, ulong revision)
    {
        _indexedRoot = root;
        Revision = revision;
        _root = null;
        _entries.Clear();
        _unbounded.Clear();

        if (root == null)
            return;

        var order = 0;
        AddVisual(root, Matrix.Identity, ref order);
        _root = BuildTree(_entries);
        _entries.Clear();
    }

    public void Query(Point point, PooledList<CompositionHitTestCandidate> results)
    {
        if (_root is { } root)
            Query(in root, point, results);

        foreach (var candidate in _unbounded)
            results.Add(candidate);
    }

    private void AddVisual(CompositionVisual visual, Matrix parentTransform, ref int order)
    {
        if (!TryGetGlobalTransform(visual, parentTransform, out var transform))
            return;

        if (visual is CompositionContainerVisual container)
        {
            for (var c = container.Children.Count - 1; c >= 0; c--)
                AddVisual(container.Children[c], transform, ref order);
        }

        if (visual is not CompositionDrawListVisual drawListVisual)
            return;

        var visualOrder = order++;

        if (drawListVisual.Visual is ICustomHitTest)
        {
            _unbounded.Add(new CompositionHitTestCandidate(drawListVisual, visualOrder));
            return;
        }

        if (TryGetTransformedBounds(drawListVisual, transform, out var bounds))
            _entries.Add(new Entry(bounds, drawListVisual, visualOrder));
    }

    private static bool TryGetGlobalTransform(CompositionVisual visual, Matrix parentTransform, out Matrix transform)
    {
        if (visual.TryGetValidReadback() is { } readback)
        {
            transform = readback.Matrix * parentTransform;
            return true;
        }

        transform = default;
        return false;
    }

    private static bool TryGetTransformedBounds(CompositionDrawListVisual visual, Matrix transform, out LtrbRect bounds)
    {
        bounds = default;

        if (visual.DrawList?.Bounds is not { } localBounds)
            return false;

        bounds = localBounds.TransformToAABB(transform);
        return !bounds.IsZeroSize;
    }

    private static Node? BuildTree(List<Entry> entries)
    {
        if (entries.Count == 0)
            return null;

        entries.Sort(CompareEntries);

        var nodes = new List<Node>((entries.Count + MaxChildren - 1) / MaxChildren);
        for (var i = 0; i < entries.Count; i += MaxChildren)
        {
            var count = Math.Min(MaxChildren, entries.Count - i);
            var leafEntries = new Entry[count];
            entries.CopyTo(i, leafEntries, 0, count);
            nodes.Add(new Node(GetBounds(leafEntries), leafEntries, null));
        }

        while (nodes.Count > 1)
        {
            nodes.Sort(CompareNodes);

            var parents = new List<Node>((nodes.Count + MaxChildren - 1) / MaxChildren);
            for (var i = 0; i < nodes.Count; i += MaxChildren)
            {
                var count = Math.Min(MaxChildren, nodes.Count - i);
                var children = new Node[count];
                nodes.CopyTo(i, children, 0, count);
                parents.Add(new Node(GetBounds(children), null, children));
            }

            nodes = parents;
        }

        return nodes[0];
    }

    private static void Query(in Node node, Point point, PooledList<CompositionHitTestCandidate> results)
    {
        if (!Contains(node.Bounds, point))
            return;

        if (node.Entries != null)
        {
            foreach (var entry in node.Entries)
            {
                if (Contains(entry.Bounds, point))
                    results.Add(new CompositionHitTestCandidate(entry.Visual, entry.Order));
            }
        }
        else if (node.Children != null)
        {
            var children = node.Children;
            for (var i = 0; i < children.Length; i++)
                Query(in children[i], point, results);
        }
    }

    private static bool Contains(LtrbRect bounds, Point point) =>
        point.X >= bounds.Left && point.X <= bounds.Right &&
        point.Y >= bounds.Top && point.Y <= bounds.Bottom;

    private static int CompareEntries(Entry left, Entry right)
    {
        var result = (left.Bounds.Left + left.Bounds.Right).CompareTo(right.Bounds.Left + right.Bounds.Right);
        return result != 0
            ? result
            : (left.Bounds.Top + left.Bounds.Bottom).CompareTo(right.Bounds.Top + right.Bounds.Bottom);
    }

    private static int CompareNodes(Node left, Node right)
    {
        var result = (left.Bounds.Left + left.Bounds.Right).CompareTo(right.Bounds.Left + right.Bounds.Right);
        return result != 0
            ? result
            : (left.Bounds.Top + left.Bounds.Bottom).CompareTo(right.Bounds.Top + right.Bounds.Bottom);
    }

    private static LtrbRect GetBounds(Entry[] entries)
    {
        var bounds = entries[0].Bounds;
        for (var i = 1; i < entries.Length; i++)
            bounds = Union(bounds, entries[i].Bounds);
        return bounds;
    }

    private static LtrbRect GetBounds(Node[] nodes)
    {
        var bounds = nodes[0].Bounds;
        for (var i = 1; i < nodes.Length; i++)
            bounds = Union(bounds, nodes[i].Bounds);
        return bounds;
    }

    private static LtrbRect Union(LtrbRect left, LtrbRect right) =>
        new(
            Math.Min(left.Left, right.Left),
            Math.Min(left.Top, right.Top),
            Math.Max(left.Right, right.Right),
            Math.Max(left.Bottom, right.Bottom));

    private readonly struct Entry(LtrbRect bounds, CompositionVisual visual, int order)
    {
        public LtrbRect Bounds { get; } = bounds;
        public CompositionVisual Visual { get; } = visual;
        public int Order { get; } = order;
    }

    private readonly struct Node(LtrbRect bounds, Entry[]? entries, Node[]? children)
    {
        public LtrbRect Bounds { get; } = bounds;
        public Entry[]? Entries { get; } = entries;
        public Node[]? Children { get; } = children;
    }
}
