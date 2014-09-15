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

        public static readonly PerspexProperty<double> OpacityProperty =
            PerspexProperty.Register<Visual, double>("Opacity", 1);

        public static readonly PerspexProperty<Transform> RenderTransformProperty =
            PerspexProperty.Register<Visual, Transform>("RenderTransform");

        public static readonly PerspexProperty<Origin> TransformOriginProperty =
            PerspexProperty.Register<Visual, Origin>("TransformOrigin", defaultValue: Origin.Default);

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

        public double Opacity
        {
            get { return this.GetValue(OpacityProperty); }
            set { this.SetValue(OpacityProperty, value); }
        }

        public Transform RenderTransform
        {
            get { return this.GetValue(RenderTransformProperty); }
            set { this.SetValue(RenderTransformProperty, value); }
        }

        public Origin TransformOrigin
        {
            get { return this.GetValue(TransformOriginProperty); }
            set { this.SetValue(TransformOriginProperty, value); }
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
                    this.OnVisualParentChanged(oldValue, value);

                    if (this.GetVisualAncestor<ILayoutRoot>() != null)
                    {
                        this.NotifyAttachedToVisualTree();
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

        protected virtual void OnAttachedToVisualTree()
        {
        }

        protected virtual void OnVisualParentChanged(IVisual oldValue, IVisual newValue)
        {
        }

        private void NotifyAttachedToVisualTree()
        {
            this.Log().Debug(string.Format(
                "Attached {0} (#{1:x8}) to visual tree",
                this.GetType().Name,
                this.GetHashCode()));

            this.OnAttachedToVisualTree();

            foreach (Visual child in ((IVisual)this).ExistingVisualChildren.OfType<Visual>())
            {
                child.NotifyAttachedToVisualTree();
            }
        }
    }
}
