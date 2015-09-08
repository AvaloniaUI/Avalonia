// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls;
using Perspex.VisualTree;
using ReactiveUI;

namespace Perspex.Diagnostics.ViewModels
{
    internal class VisualTreeNode : TreeNode
    {
        public VisualTreeNode(IVisual visual)
            : base((Control)visual)
        {
            var host = visual as IVisualTreeHost;

            if (host == null || host.Root == null)
            {
                this.Children = visual.VisualChildren.CreateDerivedCollection(x => new VisualTreeNode(x));
            }
            else
            {
                this.Children = new ReactiveList<VisualTreeNode>(new[] { new VisualTreeNode(host.Root) });
            }

            if (this.Control != null)
            {
                this.IsInTemplate = this.Control.TemplatedParent != null;
            }
        }

        public bool IsInTemplate { get; private set; }

        public static VisualTreeNode[] Create(object control)
        {
            var visual = control as IVisual;
            return visual != null ? new[] { new VisualTreeNode(visual) } : null;
        }
    }
}
