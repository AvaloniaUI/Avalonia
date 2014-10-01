// -----------------------------------------------------------------------
// <copyright file="VisualTreeNode.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics.ViewModels
{
    using Perspex.Controls;
    using ReactiveUI;

    internal class VisualTreeNode : ReactiveObject
    {
        public VisualTreeNode(IVisual visual)
        {
            this.Children = visual.VisualChildren.CreateDerivedCollection(x => new VisualTreeNode(x));
            this.Type = visual.GetType().Name;
            this.Visual = visual;

            Control control = visual as Control;

            if (control != null)
            {
                this.IsInTemplate = control.TemplatedParent != null;
            }
        }

        public IReactiveDerivedList<VisualTreeNode> Children { get; private set; }

        public bool IsInTemplate { get; private set; }

        public string Type { get; private set; }

        public IVisual Visual { get; private set; }
    }
}
