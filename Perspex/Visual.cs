// -----------------------------------------------------------------------
// <copyright file="Visual.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
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

        private Rect bounds;

        private PerspexList<IVisual> visualChildren;

        private IVisual visualParent;

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
        }

        IEnumerable<IVisual> IVisual.ExistingVisualChildren
        {
            get { return this.visualChildren != null ? this.visualChildren : Enumerable.Empty<IVisual>(); }
        }

        PerspexList<IVisual> IVisual.VisualChildren
        {
            get
            {
                if (this.visualChildren == null)
                {
                    this.visualChildren = new PerspexList<IVisual>(this.CreateVisualChildren());
                    this.visualChildren.CollectionChanged += VisualChildrenChanged;
                }

                return this.visualChildren;
            }
        }

        IVisual IVisual.VisualParent
        {
            get  {  return this.visualParent; }
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
            IRenderRoot root = this.GetVisualAncestorOrSelf<IRenderRoot>();

            if (root != null && root.RenderManager != null)
            {
                root.RenderManager.InvalidateRender(this);
            }
        }

        public virtual void Render(IDrawingContext context)
        {
            Contract.Requires<ArgumentNullException>(context != null);
        }

        private IEnumerable<IVisual> CreateVisualChildren()
        {
            return Enumerable.Empty<IVisual>();
        }

        protected virtual void OnAttachedToVisualTree(ILayoutRoot root)
        {
        }

        protected virtual void OnDetachedFromVisualTree(ILayoutRoot oldRoot)
        {
        }

        protected virtual void OnVisualParentChanged(IVisual oldParent)
        {
        }

        private void VisualChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
        }

        private void NotifyAttachedToVisualTree(ILayoutRoot root)
        {
            this.Log().Debug(string.Format(
                "Attached {0} (#{1:x8}) to visual tree",
                this.GetType().Name,
                this.GetHashCode()));

            this.OnAttachedToVisualTree(root);

            foreach (Visual child in ((IVisual)this).ExistingVisualChildren.OfType<Visual>())
            {
                child.NotifyAttachedToVisualTree(root);
            }
        }

        private void NotifyDetachedFromVisualTree(ILayoutRoot oldRoot)
        {
            this.Log().Debug(string.Format(
                "Detached {0} (#{1:x8}) from visual tree",
                this.GetType().Name,
                this.GetHashCode()));

            this.OnDetachedFromVisualTree(oldRoot);

            foreach (Visual child in ((IVisual)this).ExistingVisualChildren.OfType<Visual>())
            {
                child.NotifyDetachedFromVisualTree(oldRoot);
            }
        }
    }
}
