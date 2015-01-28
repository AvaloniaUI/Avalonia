// -----------------------------------------------------------------------
// <copyright file="VisualTreeNode.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics.ViewModels
{
    using Perspex.Controls;
    using ReactiveUI;

    internal class VisualTreeNode : TreeNode
    {
        public VisualTreeNode(IVisual visual)
            : base((Control)visual)
        {
            this.Children = visual.VisualChildren.CreateDerivedCollection(x => new VisualTreeNode(x));

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
