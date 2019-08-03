// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Collections;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class VisualTreeNode : TreeNode
    {
        public VisualTreeNode(IVisual visual, TreeNode parent)
            : base(visual, parent)
        {
            var host = visual as IVisualTreeHost;

            if (host?.Root == null)
            {
                Children = visual.VisualChildren.CreateDerivedList(x => new VisualTreeNode(x, this));
            }
            else
            {
                Children = new AvaloniaList<VisualTreeNode>(new[] { new VisualTreeNode(host.Root, this) });
            }

            if ((Visual is IStyleable styleable))
            {
                IsInTemplate = styleable.TemplatedParent != null;
            }
        }

        public bool IsInTemplate { get; }

        public static VisualTreeNode[] Create(object control)
        {
            return control is IVisual visual ? new[] { new VisualTreeNode(visual, null) } : null;
        }
    }
}
