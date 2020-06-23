// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace Avalonia.Controls.Utils
{
    internal static class SelectionTreeHelper
    {
        public static void TraverseIndexPath(
            SelectionNode root,
            IndexPath path,
            bool realizeChildren,
            Action<SelectionNode, IndexPath, int, int> nodeAction)
        {
            var node = root;

            for (int depth = 0; depth < path.GetSize(); depth++)
            {
                int childIndex = path.GetAt(depth);
                nodeAction(node, path, depth, childIndex);

                if (depth < path.GetSize() - 1)
                {
                    node = node.GetAt(childIndex, realizeChildren, path)!;
                }
            }
        }

        public static void Traverse(
            SelectionNode root,
            bool realizeChildren,
            Action<TreeWalkNodeInfo> nodeAction)
        {
            var pendingNodes = new List<TreeWalkNodeInfo>();
            var current = new IndexPath(null);

            pendingNodes.Add(new TreeWalkNodeInfo(root, current));

            while (pendingNodes.Count > 0)
            {
                var nextNode = pendingNodes.Last();
                pendingNodes.RemoveAt(pendingNodes.Count - 1);
                int count = realizeChildren ? nextNode.Node.DataCount : nextNode.Node.ChildrenNodeCount;
                for (int i = count - 1; i >= 0; i--)
                {
                    var child = nextNode.Node.GetAt(i, realizeChildren, nextNode.Path);
                    var childPath = nextNode.Path.CloneWithChildIndex(i);
                    if (child != null)
                    {
                        pendingNodes.Add(new TreeWalkNodeInfo(child, childPath, nextNode.Node));
                    }
                }

                // Queue the children first and then perform the action. This way
                // the action can remove the children in the action if necessary
                nodeAction(nextNode);
            }
        }

        public static void TraverseRangeRealizeChildren(
            SelectionNode root,
            IndexPath start,
            IndexPath end,
            Action<TreeWalkNodeInfo> nodeAction)
        {
            var pendingNodes = new List<TreeWalkNodeInfo>();
            var current = start;

            // Build up the stack to account for the depth first walk up to the 
            // start index path.
            TraverseIndexPath(
                root,
                start,
                true,
                (node, path, depth, childIndex) =>
                {
                    var currentPath = StartPath(path, depth);
                    bool isStartPath = IsSubSet(start, currentPath);
                    bool isEndPath = IsSubSet(end, currentPath);

                    int startIndex = depth < start.GetSize() && isStartPath ? start.GetAt(depth) : 0;
                    int endIndex = depth < end.GetSize() && isEndPath ? end.GetAt(depth) : node.DataCount - 1;

                    for (int i = endIndex; i >= startIndex; i--)
                    {
                        var child = node.GetAt(i, true, end);
                        if (child != null)
                        {
                            var childPath = currentPath.CloneWithChildIndex(i);
                            pendingNodes.Add(new TreeWalkNodeInfo(child, childPath, node));
                        }
                    }
                });

            // From the start index path, do a depth first walk as long as the
            // current path is less than the end path.
            while (pendingNodes.Count > 0)
            {
                var info = pendingNodes.Last();
                pendingNodes.RemoveAt(pendingNodes.Count - 1);
                int depth = info.Path.GetSize();
                bool isStartPath = IsSubSet(start, info.Path);
                bool isEndPath = IsSubSet(end, info.Path);
                int startIndex = depth < start.GetSize() && isStartPath ? start.GetAt(depth) : 0;
                int endIndex = depth < end.GetSize() && isEndPath ? end.GetAt(depth) : info.Node.DataCount - 1;
                for (int i = endIndex; i >= startIndex; i--)
                {
                    var child = info.Node.GetAt(i, true, end);
                    if (child != null)
                    {
                        var childPath = info.Path.CloneWithChildIndex(i);
                        pendingNodes.Add(new TreeWalkNodeInfo(child, childPath, info.Node));
                    }
                }

                nodeAction(info);

                if (info.Path.CompareTo(end) == 0)
                {
                    // We reached the end index path. stop iterating.
                    break;
                }
            }
        }

        private static bool IsSubSet(IndexPath path, IndexPath subset)
        {
            var subsetSize = subset.GetSize();
            if (path.GetSize() < subsetSize)
            {
                return false;
            }

            for (int i = 0; i < subsetSize; i++)
            {
                if (path.GetAt(i) != subset.GetAt(i))
                {
                    return false;
                }
            }

            return true;
        }

        private static IndexPath StartPath(IndexPath path, int length)
        {
            var subPath = new List<int>();
            for (int i = 0; i < length; i++)
            {
                subPath.Add(path.GetAt(i));
            }

            return new IndexPath(subPath);
        }

        public struct TreeWalkNodeInfo
        {
            public TreeWalkNodeInfo(SelectionNode node, IndexPath indexPath, SelectionNode? parent)
            {
                node = node ?? throw new ArgumentNullException(nameof(node));

                Node = node;
                Path = indexPath;
                ParentNode = parent;
            }

            public TreeWalkNodeInfo(SelectionNode node, IndexPath indexPath)
            {
                node = node ?? throw new ArgumentNullException(nameof(node));

                Node = node;
                Path = indexPath;
                ParentNode = null;
            }

            public SelectionNode Node { get; }
            public IndexPath Path { get; }
            public SelectionNode? ParentNode { get; }
        };

    }
}
