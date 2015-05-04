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
    using System.Reactive.Linq;
    using Perspex.Animation;
    using Perspex.Collections;
    using Perspex.Media;
    using Perspex.Platform;
    using Perspex.Rendering;
    using Perspex.VisualTree;
    using Splat;

    public class Visual : Animatable, IVisual
    {
        public static readonly PerspexProperty<Rect> BoundsProperty =
            PerspexProperty.Register<Visual, Rect>("Bounds");

        public static readonly PerspexProperty<bool> ClipToBoundsProperty =
            PerspexProperty.Register<Visual, bool>("ClipToBounds");

        public static readonly PerspexProperty<bool> IsVisibleProperty =
            PerspexProperty.Register<Visual, bool>("IsVisible", true);

        public static readonly PerspexProperty<double> OpacityProperty =
            PerspexProperty.Register<Visual, double>("Opacity", 1);

        public static readonly PerspexProperty<Transform> RenderTransformProperty =
            PerspexProperty.Register<Visual, Transform>("RenderTransform");

        public static readonly PerspexProperty<Origin> TransformOriginProperty =
            PerspexProperty.Register<Visual, Origin>("TransformOrigin", defaultValue: Origin.Default);

        public static readonly PerspexProperty<int> ZIndexProperty =
            PerspexProperty.Register<Visual, int>("ZIndex");

        private PerspexList<IVisual> visualChildren;

        private Visual visualParent;

        static Visual()
        {
            AffectsRender(IsVisibleProperty);
            AffectsRender(OpacityProperty);
            RenderTransformProperty.Changed.Subscribe(RenderTransformChanged);
        }

        public Visual()
        {
            this.visualChildren = new PerspexList<IVisual>();
            this.visualChildren.CollectionChanged += this.VisualChildrenChanged;
        }

        public Rect Bounds
        {
            get { return this.GetValue(BoundsProperty); }
            protected set { this.SetValue(BoundsProperty, value); }
        }

        public bool ClipToBounds
        {
            get { return this.GetValue(ClipToBoundsProperty); }
            set { this.SetValue(ClipToBoundsProperty, value); }
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

        public int ZIndex
        {
            get { return this.GetValue(ZIndexProperty); }
            set { this.SetValue(ZIndexProperty, value); }
        }

        IPerspexReadOnlyList<IVisual> IVisual.VisualChildren
        {
            get { return this.visualChildren; }
        }

        IVisual IVisual.VisualParent
        {
            get
            {
                return this.visualParent;
            }
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

        public Point PointToScreen(Point point)
        {
            var p = GetOffsetFromRoot(this);
            return p.Item1.TranslatePointToScreen(point + p.Item2);
        }

        public Matrix TransformToVisual(IVisual visual)
        {
            var thisOffset = GetOffsetFromRoot(this).Item2;
            var thatOffset = GetOffsetFromRoot(visual).Item2;
            return Matrix.Translation(-thatOffset) * Matrix.Translation(thisOffset);
        }

        protected static void AffectsRender(PerspexProperty property)
        {
            property.Changed.Subscribe(AffectsRenderInvalidate);
        }

        protected void AddVisualChild(Visual visual)
        {
            Contract.Requires<ArgumentNullException>(visual != null);

            this.visualChildren.Add(visual);
        }

        protected void AddVisualChildren(IEnumerable<Visual> visuals)
        {
            Contract.Requires<ArgumentNullException>(visuals != null);

            this.visualChildren.AddRange(visuals);
        }

        protected void ClearVisualChildren()
        {
            this.visualChildren.Clear();
        }

        protected void RemoveVisualChild(Visual visual)
        {
            Contract.Requires<ArgumentNullException>(visual != null);

            this.visualChildren.Remove(visual);
        }

        protected void RemoveVisualChildren(IEnumerable<Visual> visuals)
        {
            Contract.Requires<ArgumentNullException>(visuals != null);

            foreach (var v in visuals)
            {
                this.visualChildren.Remove(v);
            }
        }

        protected virtual void OnAttachedToVisualTree(IRenderRoot root)
        {
        }

        protected virtual void OnDetachedFromVisualTree(IRenderRoot oldRoot)
        {
        }

        protected virtual void OnVisualParentChanged(Visual oldParent)
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

        private static Tuple<IRenderRoot, Vector> GetOffsetFromRoot(IVisual v)
        {
            var result = new Vector();

            while (!(v is IRenderRoot))
            {
                result = new Vector(result.X + v.Bounds.X, result.Y + v.Bounds.Y);
                v = v.VisualParent;

                if (v == null)
                {
                    throw new InvalidOperationException("Control is not attached to visual tree.");
                }
            }

            return Tuple.Create((IRenderRoot)v, result);
        }

        private static void RenderTransformChanged(PerspexPropertyChangedEventArgs e)
        {
            var sender = e.Sender as Visual;

            if (sender != null)
            {
                var oldValue = e.OldValue as Transform;
                var newValue = e.NewValue as Transform;

                if (oldValue != null)
                {
                    oldValue.Changed -= sender.RenderTransformChanged;
                }

                if (newValue != null)
                {
                    newValue.Changed += sender.RenderTransformChanged;
                }

                sender.InvalidateVisual();
            }
        }

        private void RenderTransformChanged(object sender, EventArgs e)
        {
            this.InvalidateVisual();
        }

        private void SetVisualParent(Visual value)
        {
            if (this.visualParent != value)
            {
                var old = this.visualParent;
                var oldRoot = this.GetVisualAncestors().OfType<IRenderRoot>().FirstOrDefault();
                var newRoot = default(IRenderRoot);

                if (value != null)
                {
                    newRoot = value.GetSelfAndVisualAncestors().OfType<IRenderRoot>().FirstOrDefault();
                }

                this.visualParent = value;
                this.OnVisualParentChanged(old);

                if (oldRoot != null)
                {
                    this.NotifyDetachedFromVisualTree(oldRoot);
                }

                if (newRoot != null)
                {
                    this.NotifyAttachedToVisualTree(newRoot);
                }
            }
        }

        private void VisualChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Visual v in e.NewItems)
                    {
                        v.InheritanceParent = this;
                        v.SetVisualParent(this);
                    }

                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (Visual v in e.OldItems)
                    {
                        v.InheritanceParent = null;
                        v.SetVisualParent(null);
                    }

                    break;
            }
        }

        private void NotifyAttachedToVisualTree(IRenderRoot root)
        {
            this.Log().Debug(
                "Attached {0} (#{1:x8}) to visual tree",
                this.GetType().Name,
                this.GetHashCode());

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
            this.Log().Debug(
                "Detached {0} (#{1:x8}) from visual tree",
                this.GetType().Name,
                this.GetHashCode());

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
