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
    using Perspex.Controls;
    using Perspex.Layout;
    using Perspex.Media;
    using Splat;

    public abstract class Visual : PerspexObject, IVisual
    {
        private IVisual visualParent;

        public Rect Bounds
        {
            get;
            protected set;
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
                        this.Log().Debug(string.Format(
                            "Attached {0} (#{1:x8}) to visual tree",
                            this.GetType().Name,
                            this.GetHashCode()));

                        this.AttachedToVisualTree();
                    }
                }
            }
        }

        public virtual void Render(IDrawingContext context)
        {
            Contract.Requires<ArgumentNullException>(context != null);
        }

        protected virtual void AttachedToVisualTree()
        {
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
