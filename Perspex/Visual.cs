// -----------------------------------------------------------------------
// <copyright file="Visual.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Perspex.Layout;
    using Perspex.Media;
    using Perspex.Rendering;
    using Splat;

    public enum Visibility
    {
        Visible,
        Hidden,
        Collapsed,
    }

    public abstract class Visual : PerspexObject, IVisual
    {
        public static readonly PerspexProperty<Visibility> VisibilityProperty =
            PerspexProperty.Register<Visual, Visibility>("Visibility");

        private IVisual visualParent;

        private Rect bounds;

        public Visual()
        {
            this.GetObservable(VisibilityProperty).Subscribe(_ => this.InvalidateVisual());
        }

        public Visibility Visibility
        {
            get { return this.GetValue(VisibilityProperty); }
            set { this.SetValue(VisibilityProperty, value); }
        }

        Rect IVisual.Bounds
        {
            get { return this.bounds; }
            set { this.bounds = value; }
        }

        IEnumerable<IVisual> IVisual.ExistingVisualChildren
        {
            get { return ((IVisual)this).VisualChildren; }
        }

        IEnumerable<IVisual> IVisual.VisualChildren
        {
            get { return Enumerable.Empty<Visual>(); }
        }

        IVisual IVisual.VisualParent
        {
            get 
            { 
                return this.visualParent;
            }

            set
            {
                if (this.visualParent != value)
                {
                    IVisual oldValue = this.visualParent;
                    this.visualParent = value;
                    this.InheritanceParent = (PerspexObject)value;
                    this.VisualParentChanged(oldValue, value);

                    if (this.GetVisualAncestor<ILayoutRoot>() != null)
                    {
                        this.AttachedToVisualTree();
                    }
                }
            }
        }

        public void InvalidateVisual()
        {
            IRendered root = this.GetVisualAncestorOrSelf<IRendered>();

            if (root != null && root.RenderManager != null)
            {
                root.RenderManager.InvalidateRender(this);
            }
        }

        public virtual void Render(IDrawingContext context)
        {
            Contract.Requires<ArgumentNullException>(context != null);
        }

        protected virtual void AttachedToVisualTree()
        {
            this.Log().Debug(string.Format(
                "Attached {0} (#{1:x8}) to visual tree",
                this.GetType().Name,
                this.GetHashCode()));

            foreach (Visual child in ((IVisual)this).ExistingVisualChildren.OfType<Visual>())
            {
                child.AttachedToVisualTree();
            }
        }

        protected virtual void VisualParentChanged(IVisual oldValue, IVisual newValue)
        {
        }
    }
}
