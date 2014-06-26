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

    public abstract class Visual : PerspexObject, IVisual
    {
        public static readonly PerspexProperty<bool> IsVisibleProperty =
            PerspexProperty.Register<Visual, bool>("IsVisible", true);

        private IVisual visualParent;

        private Rect bounds;

        static Visual()
        {
            AffectsRender(IsVisibleProperty);
        }

        public bool IsVisible
        {
            get { return this.GetValue(IsVisibleProperty); }
            set { this.SetValue(IsVisibleProperty, value); }
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

        protected static void AffectsRender(PerspexProperty property)
        {
            property.Changed.Subscribe(AffectsRenderInvalidate);
        }

        private static void AffectsRenderInvalidate(PerspexPropertyChangedEventArgs e)
        {
            Visual visual = e.Sender as Visual;

            if (visual != null)
            {
                visual.InvalidateVisual();
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
