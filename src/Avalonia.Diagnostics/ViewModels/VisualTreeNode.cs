// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.VisualTree;
using ReactiveUI;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class VisualTreeNode : TreeNode
    {
        public VisualTreeNode(IVisual visual, TreeNode parent)
            : base((Control)visual, parent)
        {
            var host = visual as IVisualTreeHost;

            if (host?.Root == null)
            {
                Children = visual.VisualChildren.CreateDerivedCollection(x => new VisualTreeNode(x, this));
            }
            else
            {
                Children = new ReactiveList<VisualTreeNode>(new[] { new VisualTreeNode(host.Root, this) });
            }

            if (Control != null)
            {
                IsInTemplate = Control.TemplatedParent != null;
            }
        }

        public bool IsInTemplate { get; private set; }

        public static VisualTreeNode[] Create(object control)
        {
            var visual = control as IVisual;
            return visual != null ? new[] { new VisualTreeNode(visual, null) } : null;
        }
    }
}
