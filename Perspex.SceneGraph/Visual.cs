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
    using Perspex.Media;
    using Perspex.Rendering;
    using Splat;

    public abstract class Visual : PerspexObject, IVisual
    {
        public static readonly PerspexProperty<bool> IsVisibleProperty =
            PerspexProperty.Register<Visual, bool>("IsVisible", true);

        public static readonly PerspexProperty<double> OpacityProperty =
            PerspexProperty.Register<Visual, double>("Opacity", 1);

        public static readonly PerspexProperty<ITransform> RenderTransformProperty =
            PerspexProperty.Register<Visual, ITransform>("RenderTransform");

        public static readonly PerspexProperty<Origin> TransformOriginProperty =
            PerspexProperty.Register<Visual, Origin>("TransformOrigin", defaultValue: Origin.Default);

        private Rect bounds;

        private PerspexList<Visual> visualChildren;

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

        public ITransform RenderTransform
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

        IEnumerable<IVisual> IVisual.VisualChildren
        {
            get
            {
                this.EnsureVisualChildrenCreated();
                return this.visualChildren;
            }
        }

        IVisual IVisual.VisualParent
        {
            get { return this.visualParent; }
        }

        public void InvalidateVisual()
        {
            IRenderRoot root = this.GetSelfAndVisualAncestors()
                .OfType<IRenderRoot>()
                .FirstOrDefault();

            if (root != null && root.RenderManager != null)
            {
                root.RenderManager.InvalidateRender(this);
            }
        }

        public virtual void Render(IDrawingContext context)
        {
            Contract.Requires<ArgumentNullException>(context != null);
        }

        protected static void AffectsRender(PerspexProperty property)
        {
            property.Changed.Subscribe(AffectsRenderInvalidate);
        }

        protected void AddVisualChild(Visual visual)
        {
            this.EnsureVisualChildrenCreated();
            this.visualChildren.Add(visual);
        }

        protected void AddVisualChildren(IEnumerable<Visual> visuals)
        {
            this.EnsureVisualChildrenCreated();
            this.visualChildren.AddRange(visuals);
        }

        protected void ClearVisualChildren()
        {
            this.EnsureVisualChildrenCreated();
            this.visualChildren.Clear();
        }

        protected void RemoveVisualChild(Visual visual)
        {
            this.EnsureVisualChildrenCreated();
            this.visualChildren.Remove(visual);
        }

        protected void SetVisualBounds(Rect bounds)
        {
            this.bounds = bounds;
        }

        protected virtual IEnumerable<Visual> CreateVisualChildren()
        {
            return Enumerable.Empty<Visual>();
        }

        protected virtual void OnAttachedToVisualTree(IRenderRoot root)
        {
        }

        protected virtual void OnDetachedFromVisualTree(IRenderRoot oldRoot)
        {
        }

        protected virtual void OnVisualParentChanged(IVisual oldParent)
        {
        }

        private static void AffectsRenderInvalidate(PerspexPropertyChangedEventArgs e)
        {
            Visual visual = e.Sender as Visual;

            if (visual != null)
            {
                visual.InvalidateVisual();
            }
        }

        private void EnsureVisualChildrenCreated()
        {
            if (this.visualChildren == null)
            {
                this.visualChildren = new PerspexList<Visual>(this.CreateVisualChildren());
                this.visualChildren.CollectionChanged += VisualChildrenChanged;
            }
        }

        private void VisualChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
        }

        private void NotifyAttachedToVisualTree(IRenderRoot root)
        {
            this.Log().Debug(string.Format(
                "Attached {0} (#{1:x8}) to visual tree",
                this.GetType().Name,
                this.GetHashCode()));

            this.OnAttachedToVisualTree(root);

            if (this.visualChildren != null)
            {
                foreach (Visual child in this.visualChildren.OfType<Visual>())
                {
                    child.NotifyAttachedToVisualTree(root);
                }
            }
        }

        private void NotifyDetachedFromVisualTree(IRenderRoot oldRoot)
        {
            this.Log().Debug(string.Format(
                "Detached {0} (#{1:x8}) from visual tree",
                this.GetType().Name,
                this.GetHashCode()));

            this.OnDetachedFromVisualTree(oldRoot);

            if (this.visualChildren != null)
            {
                foreach (Visual child in this.visualChildren.OfType<Visual>())
                {
                    child.NotifyDetachedFromVisualTree(oldRoot);
                }
            }
        }
    }
}
