// -----------------------------------------------------------------------
// <copyright file="VisualTreeNode.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics.ViewModels
{
    using Perspex.Controls;
    using Perspex.VisualTree;
    using ReactiveUI;

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
