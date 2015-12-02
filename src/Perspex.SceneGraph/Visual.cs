// Copyright (c) The Perspex Project. All rights reserved.
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
    /// all the information needed for an <see cref="IRenderTarget"/> to render the control.
    /// To traverse the scene graph (aka Visual Tree), use the extension methods defined
    /// in <see cref="VisualExtensions"/>.
    /// </remarks>
    public class Visual : Animatable, IVisual, INamed
    {
        /// <summary>
        /// Defines the <see cref="Bounds"/> property.
        /// </summary>
        public static readonly PerspexProperty<Rect> BoundsProperty =
            PerspexProperty.RegisterDirect<Visual, Rect>(nameof(Bounds), o => o.Bounds);

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
        public static readonly PerspexProperty<RelativePoint> TransformOriginProperty =
            PerspexProperty.Register<Visual, RelativePoint>(nameof(TransformOrigin), defaultValue: RelativePoint.Center);

        /// <summary>
        /// Defines the <see cref="IVisual.VisualParent"/> property.
        /// </summary>
        public static readonly PerspexProperty<IVisual> VisualParentProperty =
            PerspexProperty.RegisterDirect<Visual, IVisual>("VisualParent", o => o._visualParent);

        /// <summary>
        /// Defines the <see cref="ZIndex"/> property.
        /// </summary>
        public static readonly PerspexProperty<int> ZIndexProperty =
            PerspexProperty.Register<Visual, int>(nameof(ZIndex));

        /// <summary>
        /// The name of the visual, if any.
        /// </summary>
        private string _name;

        /// <summary>
        /// Holds the children of the visual.
        /// </summary>
        private readonly PerspexList<IVisual> _visualChildren;

        /// <summary>
        /// The visual's bounds relative to its parent.
        /// </summary>
        private Rect _bounds;

        /// <summary>
        /// Holds the parent of the visual.
        /// </summary>
        private IVisual _visualParent;

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
            _visualChildren.ResetBehavior = ResetBehavior.Remove;
            _visualChildren.CollectionChanged += VisualChildrenChanged;
        }

        /// <summary>
        /// Raised when the control is attached to a rooted visual tree.
        /// </summary>
        public event EventHandler<VisualTreeAttachmentEventArgs> AttachedToVisualTree;

        /// <summary>
        /// Raised when the control is detached from a rooted visual tree.
        /// </summary>
        public event EventHandler<VisualTreeAttachmentEventArgs> DetachedFromVisualTree;

        /// <summary>
        /// Gets the bounds of the scene graph node relative to its parent.
        /// </summary>
        public Rect Bounds
        {
            get { return _bounds; }
            protected set { SetAndRaise(BoundsProperty, ref _bounds, value); }
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
        /// Gets or sets the name of the visual.
        /// </summary>
        /// <remarks>
        /// An element's name is used to uniquely identify a control within the control's name
        /// scope. Once the element is added to a visual tree, its name cannot be changed.
        /// </remarks>
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (value.Trim() == string.Empty)
                {
                    throw new InvalidOperationException("Cannot set Name to empty string.");
                }

                if (_isAttachedToVisualTree)
                {
                    throw new InvalidOperationException("Cannot set Name : control already added to tree.");
                }

                _name = value;
            }
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
        public RelativePoint TransformOrigin
        {
            get { return GetValue(TransformOriginProperty); }
            set { SetValue(TransformOriginProperty, value); }
        }

        /// <summary>
        /// Gets the Z index of the node.
        /// </summary>
        /// <remarks>
        /// Controls with a higher <see cref="ZIndex"/> will appear in front of controls with
        /// a lower ZIndex. If two controls have the same ZIndex then the control that appears
        /// later in the containing element's children collection will appear on top.
        /// </remarks>
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
            root?.RenderQueueManager?.InvalidateRender(this);
        }

        /// <summary>
        /// Renders the visual to a <see cref="DrawingContext"/>.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        public virtual void Render(DrawingContext context)
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
        /// <returns>
        /// A <see cref="Matrix"/> containing the transform or null if the visuals don't share a
        /// common ancestor.
        /// </returns>
        public Matrix? TransformToVisual(IVisual visual)
        {
            var common = this.FindCommonVisualAncestor(visual);

            if (common != null)
            {
                var thisOffset = GetOffsetFrom(common, this);
                var thatOffset = GetOffsetFrom(common, visual);
                return Matrix.CreateTranslation(-thatOffset) * Matrix.CreateTranslation(thisOffset);
            }

            return null;
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
        /// <param name="e">The event args.</param>
        /// <remarks>
        /// It is vital that if you override this method you call the base implementation;
        /// failing to do so will cause numerous features to not work as expected.
        /// </remarks>
        protected virtual void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            if (RenderTransform != null)
            {
                RenderTransform.Changed += RenderTransformChanged;
            }

            AttachedToVisualTree?.Invoke(this, e);
        }

        /// <summary>
        /// Called when the control is removed from a visual tree.
        /// </summary>
        /// <param name="e">The event args.</param>
        /// <remarks>
        /// It is vital that if you override this method you call the base implementation;
        /// failing to do so will cause numerous features to not work as expected.
        /// </remarks>
        protected virtual void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            if (RenderTransform != null)
            {
                RenderTransform.Changed -= RenderTransformChanged;
            }

            DetachedFromVisualTree?.Invoke(this, e);
        }

        /// <summary>
        /// Called when a property changes that should invalidate the visual.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void AffectsRenderInvalidate(PerspexPropertyChangedEventArgs e)
        {
            (e.Sender as Visual)?.InvalidateVisual();
        }

        /// <summary>
        /// Gets the event args for an <see cref="AttachedToVisualTree"/> or
        /// <see cref="DetachedFromVisualTree"/> event.
        /// </summary>
        /// <returns>
        /// A <see cref="VisualTreeAttachmentEventArgs"/> if the visual currently has a root;
        /// otherwise null.
        /// </returns>
        private VisualTreeAttachmentEventArgs GetAttachmentEventArgs()
        {
            var root = this.GetSelfAndVisualAncestors().OfType<IRenderRoot>().FirstOrDefault();
            return root != null ? new VisualTreeAttachmentEventArgs(root) : null;
        }

        /// <summary>
        /// Gets the visual offset from the specified ancestor.
        /// </summary>
        /// <param name="ancestor">The ancestor visual.</param>
        /// <param name="visual">The visual.</param>
        /// <returns>The visual offset.</returns>
        private static Vector GetOffsetFrom(IVisual ancestor, IVisual visual)
        {
            var result = new Vector();

            while (visual != ancestor)
            {
                result = new Vector(result.X + visual.Bounds.X, result.Y + visual.Bounds.Y);
                visual = visual.VisualParent;

                if (visual == null)
                {
                    throw new ArgumentException("'visual' is not a descendent of 'ancestor'.");
                }
            }

            return result;
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

            if (sender?._isAttachedToVisualTree == true)
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
            if (_visualParent == value)
            {
                return;
            }
            
            var old = _visualParent;

            if (_isAttachedToVisualTree)
            {
                var oldArgs = GetAttachmentEventArgs();

                _visualParent = value;

                if (oldArgs != null)
                {
                    NotifyDetachedFromVisualTree(oldArgs);
                }
            }
            else
            {
                _visualParent = value;
            }

            if (_visualParent is IRenderRoot || _visualParent?.IsAttachedToVisualTree == true)
            {
                var newArgs = GetAttachmentEventArgs();

                if (newArgs != null)
                {
                    NotifyAttachedToVisualTree(newArgs);
                }
            }

            RaisePropertyChanged(VisualParentProperty, old, value, BindingPriority.LocalValue);
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
        /// Calls the <see cref="OnAttachedToVisualTree(VisualTreeAttachmentEventArgs)"/> method 
        /// for this control and all of its visual descendents.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void NotifyAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _visualLogger.Verbose("Attached to visual tree");

            _isAttachedToVisualTree = true;

            OnAttachedToVisualTree(e);

            if (_visualChildren != null)
            {
                foreach (Visual child in _visualChildren.OfType<Visual>())
                {
                    child.NotifyAttachedToVisualTree(e);
                }
            }
        }

        /// <summary>
        /// Calls the <see cref="OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs)"/> method 
        /// for this control and all of its visual descendents.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void NotifyDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _visualLogger.Verbose("Detached from visual tree");

            _isAttachedToVisualTree = false;
            OnDetachedFromVisualTree(e);

            if (_visualChildren != null)
            {
                foreach (Visual child in _visualChildren.OfType<Visual>())
                {
                    child.NotifyDetachedFromVisualTree(e);
                }
            }
        }
    }
}
