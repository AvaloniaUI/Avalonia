using System;
using System.Collections.Generic;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;

namespace Avalonia.Media
{
    public sealed class DrawingGroup : Drawing
    {
        public static readonly StyledProperty<double> OpacityProperty =
            AvaloniaProperty.Register<DrawingGroup, double>(nameof(Opacity), 1);

        public static readonly StyledProperty<Transform?> TransformProperty =
            AvaloniaProperty.Register<DrawingGroup, Transform?>(nameof(Transform));

        public static readonly StyledProperty<Geometry?> ClipGeometryProperty =
            AvaloniaProperty.Register<DrawingGroup, Geometry?>(nameof(ClipGeometry));

        public static readonly StyledProperty<IBrush?> OpacityMaskProperty =
            AvaloniaProperty.Register<DrawingGroup, IBrush?>(nameof(OpacityMask));

        public static readonly DirectProperty<DrawingGroup, DrawingCollection> ChildrenProperty =
            AvaloniaProperty.RegisterDirect<DrawingGroup, DrawingCollection>(
                nameof(Children),
                o => o.Children,
                (o, v) => o.Children = v);

        private DrawingCollection _children = new DrawingCollection();

        public double Opacity
        {
            get => GetValue(OpacityProperty);
            set => SetValue(OpacityProperty, value);
        }

        public Transform? Transform
        {
            get => GetValue(TransformProperty);
            set => SetValue(TransformProperty, value);
        }

        public Geometry? ClipGeometry
        {
            get => GetValue(ClipGeometryProperty);
            set => SetValue(ClipGeometryProperty, value);
        }

        public IBrush? OpacityMask
        {
            get => GetValue(OpacityMaskProperty);
            set => SetValue(OpacityMaskProperty, value);
        }

        internal RenderOptions? RenderOptions { get; set; }

        /// <summary>
        /// Gets or sets the collection that contains the child geometries.
        /// </summary>
        [Content]
        public DrawingCollection Children
        {
            get => _children;
            set
            {
                SetAndRaise(ChildrenProperty, ref _children, value);
            }
        }

        public DrawingContext Open() => new DrawingGroupDrawingContext(this);

        internal override void DrawCore(DrawingContext context)
        {
            var bounds = GetBounds();
            using (context.PushTransform(Transform?.Value ?? Matrix.Identity))
            using (context.PushOpacity(Opacity))
            using (ClipGeometry != null ? context.PushGeometryClip(ClipGeometry) : default)
            using (OpacityMask != null ? context.PushOpacityMask(OpacityMask, bounds) : default)
            using (RenderOptions != null ? context.PushRenderOptions(RenderOptions.Value) : default)
            {
                foreach (var drawing in Children)
                {
                    drawing.Draw(context);
                }
            }
        }

        public override Rect GetBounds()
        {
            var rect = new Rect();

            foreach (var drawing in Children)
            {
                rect = rect.Union(drawing.GetBounds());
            }

            if (Transform != null)
            {
                rect = rect.TransformToAABB(Transform.Value);
            }

            return rect;
        }

        private sealed class DrawingGroupDrawingContext : DrawingContext
        {
            private readonly DrawingGroup _drawingGroup;
            private readonly IPlatformRenderInterface _platformRenderInterface = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();

            private bool _disposed;

            // Root drawing created by this DrawingContext.
            //
            // If there is only a single child of the root DrawingGroup, _rootDrawing
            // will reference the single child, and the root _currentDrawingGroup
            // value will be null.  Otherwise, _rootDrawing will reference the
            // root DrawingGroup, and be the same value as the root _currentDrawingGroup.
            //
            // Either way, _rootDrawing always references the root drawing.
            private Drawing? _rootDrawing;

            // Current DrawingGroup that new children are added to
            private DrawingGroup? _currentDrawingGroup;

            // Previous values of _currentDrawingGroup
            private Stack<DrawingGroup?>? _previousDrawingGroupStack;

            public DrawingGroupDrawingContext(DrawingGroup drawingGroup)
            {
                _drawingGroup = drawingGroup;
            }

            protected override void DrawEllipseCore(IBrush? brush, IPen? pen, Rect rect)
            {
                if ((brush == null) && (pen == null))
                {
                    return;
                }

                // Instantiate the geometry
                var geometry = _platformRenderInterface.CreateEllipseGeometry(rect);

                // Add Drawing to the Drawing graph
                AddNewGeometryDrawing(brush, pen, new PlatformGeometry(geometry));
            }

            protected override void DrawGeometryCore(IBrush? brush, IPen? pen, IGeometryImpl geometry)
            {
                if ((brush == null) && (pen == null))
                {
                    return;
                }

                AddNewGeometryDrawing(brush, pen, new PlatformGeometry(geometry));
            }

            public override void DrawGlyphRun(IBrush? foreground, GlyphRun glyphRun)
            {
                if (foreground == null)
                {
                    return;
                }

                GlyphRunDrawing glyphRunDrawing = new GlyphRunDrawing
                {
                    Foreground = foreground,
                    GlyphRun = glyphRun
                };

                // Add Drawing to the Drawing graph
                AddDrawing(glyphRunDrawing);
            }

            protected override void PushClipCore(RoundedRect rect)
            {
                throw new NotImplementedException();
            }

            protected override void PushClipCore(Rect rect)
            {
                var drawingGroup = PushNewDrawingGroup();

                drawingGroup.ClipGeometry = new RectangleGeometry(rect);
            }

            protected override void PushGeometryClipCore(Geometry clip)
            {
                var drawingGroup = PushNewDrawingGroup();

                drawingGroup.ClipGeometry = clip;
            }

            protected override void PushOpacityCore(double opacity)
            {
                var drawingGroup = PushNewDrawingGroup();

                drawingGroup.Opacity = opacity;
            }

            protected override void PushOpacityMaskCore(IBrush mask, Rect bounds)
            {
                var drawingGroup = PushNewDrawingGroup();

                drawingGroup.OpacityMask = mask;
            }

            internal override void DrawBitmap(IRef<IBitmapImpl> source, double opacity, Rect sourceRect, Rect destRect)
            {
                throw new NotImplementedException();
            }

            protected override void DrawLineCore(IPen pen, Point p1, Point p2)
            {
                // Instantiate the geometry
                var geometry = _platformRenderInterface.CreateLineGeometry(p1, p2);

                // Add Drawing to the Drawing graph
                AddNewGeometryDrawing(null, pen, new PlatformGeometry(geometry));
            }

            protected override void DrawRectangleCore(IBrush? brush, IPen? pen, RoundedRect rrect, BoxShadows boxShadows = default)
            {
                // Instantiate the geometry
                var geometry = _platformRenderInterface.CreateRectangleGeometry(rrect.Rect);

                // Add Drawing to the Drawing graph
                AddNewGeometryDrawing(brush, pen, new PlatformGeometry(geometry));
            }

            public override void Custom(ICustomDrawOperation custom) => throw new NotSupportedException();

            protected override void DisposeCore()
            {
                // Dispose may be called multiple times without throwing
                // an exception.
                if (!_disposed)
                {
                    // Match any outstanding Push calls with a Pop
                    if (_previousDrawingGroupStack != null)
                    {
                        int stackCount = _previousDrawingGroupStack.Count;
                        for (int i = 0; i < stackCount; i++)
                        {
                            Pop();
                        }
                    }

                    // Call CloseCore with the root DrawingGroup's children
                    DrawingCollection rootChildren;

                    if (_currentDrawingGroup != null)
                    {
                        // If we created a root DrawingGroup because multiple elements
                        // exist at the root level, provide it's Children collection
                        // directly.
                        rootChildren = _currentDrawingGroup.Children;
                    }
                    else
                    {
                        // Create a new DrawingCollection if we didn't create a
                        // root DrawingGroup because the root level only contained
                        // a single child.
                        //
                        // This collection is needed by DrawingGroup.Open because
                        // Open always replaces it's Children collection.  It isn't
                        // strictly needed for Append, but always using a collection
                        // simplifies the TransactionalAppend implementation (i.e.,
                        // a seperate implemention isn't needed for a single element)
                        rootChildren = new DrawingCollection();

                        //
                        // We may need to opt-out of inheritance through the new Freezable.
                        // This is controlled by this.CanBeInheritanceContext.
                        //
                        if (_rootDrawing != null)
                        {
                            rootChildren.Add(_rootDrawing);
                        }
                    }

                    // Inform our derived classes that Close was called
                    _drawingGroup.Children = rootChildren;

                    _disposed = true;
                }
            }

            /// <summary>
            /// Pop
            /// </summary>
            private void Pop()
            {
                // Verify that Pop hasn't been called too many times
                if ((_previousDrawingGroupStack == null) || (_previousDrawingGroupStack.Count == 0))
                {
                    throw new InvalidOperationException("DrawingGroupStack count missmatch.");
                }

                // Restore the previous value of the current drawing group
                _currentDrawingGroup = _previousDrawingGroupStack.Pop();
            }
            
            /// <summary>
            ///     PushTransform -
            ///     Push a Transform which will apply to all drawing operations until the corresponding
            ///     Pop.
            /// </summary>
            /// <param name="matrix"> The transform to push. </param>
            protected override void PushTransformCore(Matrix matrix)
            {
                // Instantiate a new drawing group and set it as the _currentDrawingGroup
                var drawingGroup = PushNewDrawingGroup();

                // Set the transform on the new DrawingGroup
                drawingGroup.Transform = new MatrixTransform(matrix);
            }

            protected override void PushRenderOptionsCore(RenderOptions renderOptions)
            {
                // Instantiate a new drawing group and set it as the _currentDrawingGroup
                var drawingGroup = PushNewDrawingGroup();

                // Set the render options on the new DrawingGroup
                drawingGroup.RenderOptions = renderOptions;
            }

            protected override void PopClipCore() => Pop();

            protected override void PopGeometryClipCore() => Pop();

            protected override void PopOpacityCore() => Pop();

            protected override void PopOpacityMaskCore() => Pop();

            protected override void PopTransformCore() => Pop();

            protected override void PopRenderOptionsCore() => Pop();

            /// <summary>
            /// Creates a new DrawingGroup for a Push* call by setting the
            /// _currentDrawingGroup to a newly instantiated DrawingGroup,
            /// and saving the previous _currentDrawingGroup value on the
            /// _previousDrawingGroupStack.
            /// </summary>
            private DrawingGroup PushNewDrawingGroup()
            {
                // Instantiate a new drawing group
                DrawingGroup drawingGroup = new DrawingGroup();

                // Add it to the drawing graph, like any other Drawing
                AddDrawing(drawingGroup);

                // Lazily allocate the stack when it is needed because many uses
                // of DrawingDrawingContext will have a depth of one.
                if (null == _previousDrawingGroupStack)
                {
                    _previousDrawingGroupStack = new Stack<DrawingGroup?>(2);
                }

                // Save the previous _currentDrawingGroup value.
                //
                // If this is the first call, the value of _currentDrawingGroup
                // will be null because AddDrawing doesn't create a _currentDrawingGroup
                // for the first drawing.  Having null on the stack is valid, and simply
                // denotes that this new DrawingGroup is the first child in the root
                // DrawingGroup.  It is also possible for the first value on the stack
                // to be non-null, which means that the root DrawingGroup has other
                // children.
                _previousDrawingGroupStack.Push(_currentDrawingGroup);

                // Set this drawing group as the current one so that subsequent drawing's
                // are added as it's children until Pop is called.
                _currentDrawingGroup = drawingGroup;

                return drawingGroup;
            }

            /// <summary>
            /// Contains the functionality common to GeometryDrawing operations of
            /// instantiating the GeometryDrawing, setting it's Freezable state,
            /// and Adding it to the Drawing Graph.
            /// </summary>
            private void AddNewGeometryDrawing(IBrush? brush, IPen? pen, Geometry? geometry)
            {
                if (geometry == null)
                {
                    throw new ArgumentNullException(nameof(geometry));
                }

                // Instantiate the GeometryDrawing
                GeometryDrawing geometryDrawing = new GeometryDrawing
                {
                    // We may need to opt-out of inheritance through the new Freezable.
                    // This is controlled by this.CanBeInheritanceContext.
                    Brush = brush,
                    Pen = pen,
                    Geometry = geometry
                };

                // Add it to the drawing graph
                AddDrawing(geometryDrawing);
            }

            /// <summary>
            /// Adds a new Drawing to the DrawingGraph.
            ///
            /// This method avoids creating a DrawingGroup for the common case
            /// where only a single child exists in the root DrawingGroup.
            /// </summary>
            private void AddDrawing(Drawing newDrawing)
            {
                if (newDrawing == null)
                {
                    throw new ArgumentNullException(nameof(newDrawing));
                }

                if (_rootDrawing == null)
                {
                    if (_currentDrawingGroup != null)
                    {
                        throw new NotSupportedException("When a DrawingGroup is set, it should be made the root if a root drawing didnt exist.");
                    }

                    // If this is the first Drawing being added, avoid creating a DrawingGroup
                    // and set this drawing as the root drawing.  This optimizes the common
                    // case where only a single child exists in the root DrawingGroup.
                    _rootDrawing = newDrawing;
                }
                else if (_currentDrawingGroup == null)
                {
                    // When the second drawing is added at the root level, set a
                    // DrawingGroup as the root and add both drawings to it.

                    // Instantiate the DrawingGroup
                    _currentDrawingGroup = new DrawingGroup();

                    // Add both Children
                    _currentDrawingGroup.Children.Add(_rootDrawing);
                    _currentDrawingGroup.Children.Add(newDrawing);

                    // Set the new DrawingGroup as the current
                    _rootDrawing = _currentDrawingGroup;
                }
                else
                {
                    // If there already is a current drawing group, then simply add
                    // the new drawing too it.
                    _currentDrawingGroup.Children.Add(newDrawing);
                }
            }
        }
    }
}
