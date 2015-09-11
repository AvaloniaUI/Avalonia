﻿// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
using Serilog;
using Serilog.Core.Enrichers;

namespace Perspex
{
    /// <summary>
    /// Base class for controls that provides rendering and related visual properties.
    /// </summary>
    /// <remarks>
    /// The <see cref="Visual"/> class acts as a node in the Perspex scene graph and holds
    /// all the information needed for an <see cref="IRenderer"/> to render the control.
    /// To traverse the scene graph (aka Visual Tree), use the extension methods defined
    /// in <see cref="VisualExtensions"/>.
    /// </remarks>
    public class Visual : Animatable, IVisual
    {
        /// <summary>
        /// Defines the <see cref="Bounds"/> property.
        /// </summary>
        public static readonly PerspexProperty<Rect> BoundsProperty =
            PerspexProperty.Register<Visual, Rect>(nameof(Bounds));

        /// <summary>
        /// Defines the <see cref="ClipToBounds"/> property.
        /// </summary>
        public static readonly PerspexProperty<bool> ClipToBoundsProperty =
            PerspexProperty.Register<Visual, bool>(nameof(ClipToBounds));

        /// <summary>
        /// Defines the <see cref="IsVisibleProperty"/> property.
        /// </summary>
        public static readonly PerspexProperty<bool> IsVisibleProperty =
            PerspexProperty.Register<Visual, bool>(nameof(IsVisible), true);

        /// <summary>
        /// Defines the <see cref="Opacity"/> property.
        /// </summary>
        public static readonly PerspexProperty<double> OpacityProperty =
            PerspexProperty.Register<Visual, double>(nameof(Opacity), 1);

        /// <summary>
        /// Defines the <see cref="RenderTransform"/> property.
        /// </summary>
        public static readonly PerspexProperty<Transform> RenderTransformProperty =
            PerspexProperty.Register<Visual, Transform>(nameof(RenderTransform));

        /// <summary>
        /// Defines the <see cref="TransformOrigin"/> property.
        /// </summary>
        public static readonly PerspexProperty<Origin> TransformOriginProperty =
            PerspexProperty.Register<Visual, Origin>(nameof(TransformOrigin), defaultValue: Origin.Default);

        /// <summary>
        /// Defines the <see cref="ZIndex"/> property.
        /// </summary>
        public static readonly PerspexProperty<int> ZIndexProperty =
            PerspexProperty.Register<Visual, int>(nameof(ZIndex));

        /// <summary>
        /// Holds the children of the visual.
        /// </summary>
        private readonly PerspexList<IVisual> _visualChildren;

        /// <summary>
        /// Holds the parent of the visual.
        /// </summary>
        private Visual _visualParent;

        /// <summary>
        /// Whether the element is attached to the visual tree.
        /// </summary>
        private bool _isAttachedToVisualTree;

        /// <summary>
        /// The logger for visual-level events.
        /// </summary>
        private readonly ILogger _visualLogger;

        /// <summary>
        /// Initializes static members of the <see cref="Visual"/> class.
        /// </summary>
        static Visual()
        {
            AffectsRender(IsVisibleProperty);
            AffectsRender(OpacityProperty);
            RenderTransformProperty.Changed.Subscribe(RenderTransformChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Visual"/> class.
        /// </summary>
        public Visual()
        {
            _visualLogger = Log.ForContext(new[]
            {
                new PropertyEnricher("Area", "Visual"),
                new PropertyEnricher("SourceContext", GetType()),
                new PropertyEnricher("Id", GetHashCode()),
            });

            _visualChildren = new PerspexList<IVisual>();
            _visualChildren.CollectionChanged += VisualChildrenChanged;
        }

        /// <summary>
        /// Gets the bounds of the scene graph node.
        /// </summary>
        public Rect Bounds
        {
            get { return GetValue(BoundsProperty); }
            protected set { SetValue(BoundsProperty, value); }
        }

        /// <summary>
        /// Gets a value indicating whether the scene graph node should be clipped to its bounds.
        /// </summary>
        public bool ClipToBounds
        {
            get { return GetValue(ClipToBoundsProperty); }
            set { SetValue(ClipToBoundsProperty, value); }
        }

        /// <summary>
        /// Gets a value indicating whether this scene graph node and all its parents are visible.
        /// </summary>
        public bool IsEffectivelyVisible
        {
            get { return this.GetSelfAndVisualAncestors().All(x => x.IsVisible); }
        }

        /// <summary>
        /// Gets a value indicating whether this scene graph node is visible.
        /// </summary>
        public bool IsVisible
        {
            get { return GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }

        /// <summary>
        /// Gets the opacity of the scene graph node.
        /// </summary>
        public double Opacity
        {
            get { return GetValue(OpacityProperty); }
            set { SetValue(OpacityProperty, value); }
        }

        /// <summary>
        /// Gets the render transform of the scene graph node.
        /// </summary>
        public Transform RenderTransform
        {
            get { return GetValue(RenderTransformProperty); }
            set { SetValue(RenderTransformProperty, value); }
        }

        /// <summary>
        /// Gets the transform origin of the scene graph node.
        /// </summary>
        public Origin TransformOrigin
        {
            get { return GetValue(TransformOriginProperty); }
            set { SetValue(TransformOriginProperty, value); }
        }

        /// <summary>
        /// Gets the Z index of the node.
        /// </summary>
        public int ZIndex
        {
            get { return GetValue(ZIndexProperty); }
            set { SetValue(ZIndexProperty, value); }
        }

        /// <summary>
        /// Gets a value indicating whether this scene graph node is attached to a visual root.
        /// </summary>
        bool IVisual.IsAttachedToVisualTree => _isAttachedToVisualTree;

        /// <summary>
        /// Gets the scene graph node's child nodes.
        /// </summary>
        IPerspexReadOnlyList<IVisual> IVisual.VisualChildren => _visualChildren;

        /// <summary>
        /// Gets the scene graph node's parent node.
        /// </summary>
        IVisual IVisual.VisualParent => _visualParent;

        /// <summary>
        /// Invalidates the visual and queues a repaint.
        /// </summary>
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

        /// <summary>
        /// Renders the visual to a <see cref="IDrawingContext"/>.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        public virtual void Render(IDrawingContext context)
        {
            Contract.Requires<ArgumentNullException>(context != null);
        }

        /// <summary>
        /// Converts a point from control coordinates to screen coordinates.
        /// </summary>
        /// <param name="point">The point to convert.</param>
        /// <returns>The point in screen coordinates.</returns>
        public Point PointToScreen(Point point)
        {
            var p = GetOffsetFromRoot(this);
            return p.Item1.TranslatePointToScreen(point + p.Item2);
        }

        /// <summary>
        /// Returns a transform that transforms the visual's coordinates into the coordinates
        /// of the specified <paramref name="visual"/>.
        /// </summary>
        /// <param name="visual">The visual to translate the coordinates to.</param>
        /// <returns>A <see cref="Matrix"/> containing the transform.</returns>
        public Matrix TransformToVisual(IVisual visual)
        {
            var thisOffset = GetOffsetFromRoot(this).Item2;
            var thatOffset = GetOffsetFromRoot(visual).Item2;
            return Matrix.CreateTranslation(-thatOffset) * Matrix.CreateTranslation(thisOffset);
        }

        /// <summary>
        /// Indicates that a property change should cause <see cref="InvalidateVisual"/> to be
        /// called.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <remarks>
        /// This method should be called in a control's static constructor for each property
        /// on the control which when changed should cause a redraw. This is similar to WPF's
        /// FrameworkPropertyMetadata.AffectsRender flag.
        /// </remarks>
        protected static void AffectsRender(PerspexProperty property)
        {
            property.Changed.Subscribe(AffectsRenderInvalidate);
        }

        /// <summary>
        /// Adds a visual child to the control.
        /// </summary>
        /// <param name="visual">The child to add.</param>
        protected void AddVisualChild(IVisual visual)
        {
            Contract.Requires<ArgumentNullException>(visual != null);

            _visualChildren.Add(visual);
        }

        /// <summary>
        /// Adds visual children to the control.
        /// </summary>
        /// <param name="visuals">The children to add.</param>
        protected void AddVisualChildren(IEnumerable<IVisual> visuals)
        {
            Contract.Requires<ArgumentNullException>(visuals != null);

            _visualChildren.AddRange(visuals);
        }

        /// <summary>
        /// Removes all visual children from the control.
        /// </summary>
        protected void ClearVisualChildren()
        {
            _visualChildren.Clear();
        }

        /// <summary>
        /// Removes a visual child from the control;
        /// </summary>
        /// <param name="visual">The child to remove.</param>
        protected void RemoveVisualChild(IVisual visual)
        {
            Contract.Requires<ArgumentNullException>(visual != null);

            _visualChildren.Remove(visual);
        }

        /// <summary>
        /// Removes a visual children from the control;
        /// </summary>
        /// <param name="visuals">The children to remove.</param>
        protected void RemoveVisualChildren(IEnumerable<IVisual> visuals)
        {
            Contract.Requires<ArgumentNullException>(visuals != null);

            foreach (var v in visuals)
            {
                _visualChildren.Remove(v);
            }
        }

        /// <summary>
        /// Called when the control is added to a visual tree.
        /// </summary>
        /// <param name="root">The root of the visual tree.</param>
        protected virtual void OnAttachedToVisualTree(IRenderRoot root)
        {
        }

        /// <summary>
        /// Called when the control is removed from a visual tree.
        /// </summary>
        /// <param name="root">The root of the visual tree.</param>
        protected virtual void OnDetachedFromVisualTree(IRenderRoot root)
        {
        }

        /// <summary>
        /// Called when a property changes that should invalidate the visual.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void AffectsRenderInvalidate(PerspexPropertyChangedEventArgs e)
        {
            Visual visual = e.Sender as Visual;

            if (visual != null)
            {
                visual.InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets the root of the controls visual tree and the distance from the root.
        /// </summary>
        /// <param name="v">The visual.</param>
        /// <returns>A tuple containing the root and the distance from the root</returns>
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

        /// <summary>
        /// Called when a visual's <see cref="RenderTransform"/> changes.
        /// </summary>
        /// <param name="e">The event args.</param>
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

        /// <summary>
        /// Called when the <see cref="RenderTransform"/>'s <see cref="Transform.Changed"/> event
        /// is fired.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void RenderTransformChanged(object sender, EventArgs e)
        {
            InvalidateVisual();
        }

        /// <summary>
        /// Sets the visual parent of the Visual.
        /// </summary>
        /// <param name="value">The visual parent.</param>
        private void SetVisualParent(Visual value)
        {
            if (_visualParent != value)
            {
                var old = _visualParent;
                var oldRoot = this.GetVisualAncestors().OfType<IRenderRoot>().FirstOrDefault();
                var newRoot = default(IRenderRoot);

                if (value != null)
                {
                    newRoot = value.GetSelfAndVisualAncestors().OfType<IRenderRoot>().FirstOrDefault();
                }

                _visualParent = value;

                if (oldRoot != null)
                {
                    NotifyDetachedFromVisualTree(oldRoot);
                }

                if (newRoot != null)
                {
                    NotifyAttachedToVisualTree(newRoot);
                }
            }
        }

        /// <summary>
        /// Called when the <see cref="_visualChildren"/> collection changes.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
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

        /// <summary>
        /// Calls the <see cref="OnAttachedToVisualTree(IRenderRoot)"/> method for this control
        /// and all of its visual descendents.
        /// </summary>
        /// <param name="root">The root of the visual tree.</param>
        private void NotifyAttachedToVisualTree(IRenderRoot root)
        {
            _visualLogger.Verbose("Attached to visual tree");

            _isAttachedToVisualTree = true;
            OnAttachedToVisualTree(root);

            if (_visualChildren != null)
            {
                foreach (Visual child in _visualChildren.OfType<Visual>())
                {
                    child.NotifyAttachedToVisualTree(root);
                }
            }
        }

        /// <summary>
        /// Calls the <see cref="OnDetachedFromVisualTree(IRenderRoot)"/> method for this control
        /// and all of its visual descendents.
        /// </summary>
        /// <param name="root">The root of the visual tree.</param>
        private void NotifyDetachedFromVisualTree(IRenderRoot root)
        {
            _visualLogger.Verbose("Detached from visual tree");

            _isAttachedToVisualTree = false;
            OnDetachedFromVisualTree(root);

            if (_visualChildren != null)
            {
                foreach (Visual child in _visualChildren.OfType<Visual>())
                {
                    child.NotifyDetachedFromVisualTree(root);
                }
            }
        }
    }
}
