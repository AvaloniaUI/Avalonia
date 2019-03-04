// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using Avalonia.Controls.Generators;

namespace Avalonia.Controls
{
    /// <summary>
    /// Helper for <see cref="TreeView"/> related operations.
    /// </summary>
    internal static class TreeViewHelper
    {
        /// <summary>
        /// Find which node from search info is first in hierarchy.
        /// </summary>
        /// <param name="treeView">Search root.</param>
        /// <param name="searchInfo">Nodes to search for.</param>
        /// <returns>Found first node.</returns>
        public static TreeViewItem FindFirstNode(TreeView treeView, in SearchInfo searchInfo)
        {
            return FindInContainers(treeView.ItemContainerGenerator, in searchInfo);
        }

        private static TreeViewItem FindInContainers(ITreeItemContainerGenerator containerGenerator,
            in SearchInfo searchInfo)
        {
            IEnumerable<ItemContainerInfo> containers = containerGenerator.Containers;

            foreach (ItemContainerInfo container in containers)
            {
                TreeViewItem node = FindFirstNode(container.ContainerControl as TreeViewItem, in searchInfo);

                if (node != null)
                {
                    return node;
                }
            }

            return null;
        }

        private static TreeViewItem FindFirstNode(TreeViewItem node, in SearchInfo searchInfo)
        {
            if (node == null)
            {
                return null;
            }

            TreeViewItem match = searchInfo.GetMatch(node);

            if (match != null)
            {
                return match;
            }

            return FindInContainers(node.ItemContainerGenerator, in searchInfo);
        }

        /// <summary>
        /// Node search info.
        /// </summary>
        public readonly struct SearchInfo
        {
            public readonly TreeViewItem Search1;
            public readonly TreeViewItem Search2;

            public SearchInfo(TreeViewItem search1, TreeViewItem search2)
            {
                Search1 = search1;
                Search2 = search2;
            }

            public TreeViewItem GetMatch(TreeViewItem candidate)
            {
                if (candidate == Search1)
                {
                    return Search1;
                }

                if (candidate == Search2)
                {
                    return Search2;
                }

                return null;
            }
        }
    }
}
