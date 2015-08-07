// -----------------------------------------------------------------------
// <copyright file="TestVisual.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------
namespace Perspex.SceneGraph.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Perspex.Rendering;

    public class ParamEventArgs<T> : EventArgs
    {
        public ParamEventArgs(T param)
        {
            this.Param = param;
        }

        public T Param { get; set; }
    }

    public class TestVisual : Visual
    {
        public new PerspexObject InheritanceParent
        {
            get { return base.InheritanceParent; }
        }

        public event EventHandler<ParamEventArgs<IRenderRoot>> AttachedToVisualTreeCalled;

        public event EventHandler<ParamEventArgs<IRenderRoot>> DetachedFromVisualTreeCalled;

        public void AddChild(Visual v)
        {
            this.AddVisualChild(v);
        }

        public void AddChildren(IEnumerable<Visual> v)
        {
            this.AddVisualChildren(v);
        }

        public void RemoveChild(Visual v)
        {
            this.RemoveVisualChild(v);
        }

        public void ClearChildren()
        {
            this.ClearVisualChildren();
        }

        protected override void OnAttachedToVisualTree(IRenderRoot root)
        {
            if (this.AttachedToVisualTreeCalled != null)
            {
                this.AttachedToVisualTreeCalled(this, new ParamEventArgs<IRenderRoot>(root));
            }
        }

        protected override void OnDetachedFromVisualTree(IRenderRoot oldRoot)
        {
            if (this.DetachedFromVisualTreeCalled != null)
            {
                this.DetachedFromVisualTreeCalled(this, new ParamEventArgs<IRenderRoot>(oldRoot));
            }
        }
    }
}
