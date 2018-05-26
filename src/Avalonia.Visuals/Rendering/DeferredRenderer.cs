// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media.Immutable;
using System.Threading;
using System.Linq;
using Avalonia.Utilities;

namespace Avalonia.Rendering
{
    /// <summary>
    /// A renderer which renders the state of the visual tree to an intermediate scene graph
    /// representation which is then rendered on a rendering thread.
    /// </summary>
    public class DeferredRenderer : RendererBase, IRenderer, IVisualBrushRenderer
    {
        private readonly IDispatcher _dispatcher;
        private readonly IRenderLoop _renderLoop;
        private readonly IVisual _root;
        private readonly ISceneBuilder _sceneBuilder;

        private bool _running;
        private volatile IRef<Scene> _scene;
        private DirtyVisuals _dirty;
        private IRef<IRenderTargetBitmapImpl> _overlay;
        private bool _updateQueued;
        private object _rendering = new object();
        private int _lastSceneId = -1;
        private DisplayDirtyRects _dirtyRectsDisplay = new DisplayDirtyRects();
        private IRef<IDrawOperation> _currentDraw;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeferredRenderer"/> class.
        /// </summary>
        /// <param name="root">The control to render.</param>
        /// <param name="renderLoop">The render loop.</param>
        /// <param name="sceneBuilder">The scene builder to use. Optional.</param>
        /// <param name="dispatcher">The dispatcher to use. Optional.</param>
        public DeferredRenderer(
            IRenderRoot root,
            IRenderLoop renderLoop,
            ISceneBuilder sceneBuilder = null,
            IDispatcher dispatcher = null)
        {
            Contract.Requires<ArgumentNullException>(root != null);

            _dispatcher = dispatcher ?? Dispatcher.UIThread;
            _root = root;
            _sceneBuilder = sceneBuilder ?? new SceneBuilder();
            Layers = new RenderLayers();
            _renderLoop = renderLoop;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeferredRenderer"/> class.
        /// </summary>
        /// <param name="root">The control to render.</param>
        /// <param name="renderTarget">The render target.</param>
        /// <param name="sceneBuilder">The scene builder to use. Optional.</param>
        /// <remarks>
        /// This constructor is intended to be used for unit testing.
        /// </remarks>
        public DeferredRenderer(
            IVisual root,
            IRenderTarget renderTarget,
            ISceneBuilder sceneBuilder = null)
        {
            Contract.Requires<ArgumentNullException>(root != null);
            Contract.Requires<ArgumentNullException>(renderTarget != null);

            _root = root;
            RenderTarget = renderTarget;
            _sceneBuilder = sceneBuilder ?? new SceneBuilder();
            Layers = new RenderLayers();
        }

        /// <inheritdoc/>
        public bool DrawFps { get; set; }

        /// <inheritdoc/>
        public bool DrawDirtyRects { get; set; }

        /// <summary>
        /// Gets or sets a path to which rendered frame should be rendered for debugging.
        /// </summary>
        public string DebugFramesPath { get; set; }

        /// <summary>
        /// Gets the render layers.
        /// </summary>
        internal RenderLayers Layers { get; }

        /// <summary>
        /// Gets the current render target.
        /// </summary>
        internal IRenderTarget RenderTarget { get; private set; }

        /// <inheritdoc/>
        public void AddDirty(IVisual visual)
        {
            _dirty?.Add(visual);
        }

        /// <summary>
        /// Disposes of the renderer and detaches from the render loop.
        /// </summary>
        public void Dispose()
        {
            var scene = Interlocked.Exchange(ref _scene, null);
            scene?.Dispose();
            Stop();
        }

        /// <inheritdoc/>
        public IEnumerable<IVisual> HitTest(Point p, IVisual root, Func<IVisual, bool> filter)
        {
            if (_renderLoop == null && (_dirty == null || _dirty.Count > 0))
            {
                // When unit testing the renderLoop may be null, so update the scene manually.
                UpdateScene();
            }

            return _scene?.Item.HitTest(p, root, filter) ?? Enumerable.Empty<IVisual>();
        }

        /// <inheritdoc/>
        public void Paint(Rect rect)
        {
        }

        /// <inheritdoc/>
        public void Resized(Size size)
        {
        }

        /// <inheritdoc/>
        public void Start()
        {
            if (!_running && _renderLoop != null)
            {
                _renderLoop.Tick += OnRenderLoopTick;
                _running = true;
            }
        }

        /// <inheritdoc/>
        public void Stop()
        {
            if (_running && _renderLoop != null)
            {
                _renderLoop.Tick -= OnRenderLoopTick;
                _running = false;
            }
        }

        /// <inheritdoc/>
        Size IVisualBrushRenderer.GetRenderTargetSize(IVisualBrush brush)
        {
            return (_currentDraw.Item as BrushDrawOperation)?.ChildScenes?[brush.Visual]?.Size ?? Size.Empty;
        }

        /// <inheritdoc/>
        void IVisualBrushRenderer.RenderVisualBrush(IDrawingContextImpl context, IVisualBrush brush)
        {
            var childScene = (_currentDraw.Item as BrushDrawOperation)?.ChildScenes?[brush.Visual];

            if (childScene != null)
            {
                Render(context, (VisualNode)childScene.Root, null, new Rect(childScene.Size));
            }
        }

        internal void UnitTestUpdateScene() => UpdateScene();

        internal void UnitTestRender() => Render(_scene.Item);

        private void Render(Scene scene)
        {
            bool renderOverlay = DrawDirtyRects || DrawFps;
            bool composite = false;

            if (RenderTarget == null)
            {
                RenderTarget = ((IRenderRoot)_root).CreateRenderTarget();
            }

            if (renderOverlay)
            {
                _dirtyRectsDisplay.Tick();
            }

            try
            {
                if (scene != null && scene.Size != Size.Empty)
                {
                    IDrawingContextImpl context = null;

                    if (scene.Generation != _lastSceneId)
                    {
                        context = RenderTarget.CreateDrawingContext(this);
                        Layers.Update(scene, context);

                        RenderToLayers(scene);

                        if (DebugFramesPath != null)
                        {
                            SaveDebugFrames(scene.Generation);
                        }

                        _lastSceneId = scene.Generation;

                        composite = true;
                    }

                    if (renderOverlay)
                    {
                        context = context ?? RenderTarget.CreateDrawingContext(this);
                        RenderOverlay(scene, context);
                        RenderComposite(scene, context);
                    }
                    else if (composite)
                    {
                        context = context ?? RenderTarget.CreateDrawingContext(this);
                        RenderComposite(scene, context);
                    }

                    context?.Dispose();
                }
            }
            catch (RenderTargetCorruptedException ex)
            {
                Logging.Logger.Information("Renderer", this, "Render target was corrupted. Exception: {0}", ex);
                RenderTarget?.Dispose();
                RenderTarget = null;
            }
        }

        private void Render(IDrawingContextImpl context, VisualNode node, IVisual layer, Rect clipBounds)
        {
            if (layer == null || node.LayerRoot == layer)
            {
                clipBounds = node.ClipBounds.Intersect(clipBounds);

                if (!clipBounds.IsEmpty && node.Opacity > 0)
                {
                    var isLayerRoot = node.Visual == layer;

                    node.BeginRender(context, isLayerRoot);

                    foreach (var operation in node.DrawOperations)
                    {
                        _currentDraw = operation;
                        operation.Item.Render(context);
                        _currentDraw = null;
                    }

                    foreach (var child in node.Children)
                    {
                        Render(context, (VisualNode)child, layer, clipBounds);
                    }

                    node.EndRender(context, isLayerRoot);
                }
            }
        }

        private void RenderToLayers(Scene scene)
        {
            if (scene.Layers.HasDirty)
            {
                foreach (var layer in scene.Layers)
                {
                    var renderTarget = Layers[layer.LayerRoot].Bitmap;
                    var node = (VisualNode)scene.FindNode(layer.LayerRoot);

                    if (node != null)
                    {
                        using (var context = renderTarget.Item.CreateDrawingContext(this))
                        {
                            foreach (var rect in layer.Dirty)
                            {
                                context.Transform = Matrix.Identity;
                                context.PushClip(rect);
                                context.Clear(Colors.Transparent);
                                Render(context, node, layer.LayerRoot, rect);
                                context.PopClip();

                                if (DrawDirtyRects)
                                {
                                    _dirtyRectsDisplay.Add(rect);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void RenderOverlay(Scene scene, IDrawingContextImpl parentContent)
        {
            if (DrawDirtyRects)
            {
                var overlay = GetOverlay(parentContent, scene.Size, scene.Scaling);

                using (var context = overlay.Item.CreateDrawingContext(this))
                {
                    context.Clear(Colors.Transparent);
                    RenderDirtyRects(context);
                }
            }
            else
            {
                _overlay?.Dispose();
                _overlay = null;
            }
        }

        private void RenderDirtyRects(IDrawingContextImpl context)
        {
            foreach (var r in _dirtyRectsDisplay)
            {
                var brush = new ImmutableSolidColorBrush(Colors.Magenta, r.Opacity);
                context.FillRectangle(brush, r.Rect);
            }
        }

        private void RenderComposite(Scene scene, IDrawingContextImpl context)
        {
            var clientRect = new Rect(scene.Size);

            foreach (var layer in scene.Layers)
            {
                var bitmap = Layers[layer.LayerRoot].Bitmap;
                var sourceRect = new Rect(0, 0, bitmap.Item.PixelWidth, bitmap.Item.PixelHeight);

                if (layer.GeometryClip != null)
                {
                    context.PushGeometryClip(layer.GeometryClip);
                }

                if (layer.OpacityMask == null)
                {
                    context.DrawImage(bitmap, layer.Opacity, sourceRect, clientRect);
                }
                else
                {
                    context.DrawImage(bitmap, layer.OpacityMask, layer.OpacityMaskRect, sourceRect);
                }

                if (layer.GeometryClip != null)
                {
                    context.PopGeometryClip();
                }
            }

            if (_overlay != null)
            {
                var sourceRect = new Rect(0, 0, _overlay.Item.PixelWidth, _overlay.Item.PixelHeight);
                context.DrawImage(_overlay, 0.5, sourceRect, clientRect);
            }

            if (DrawFps)
            {
                RenderFps(context, clientRect, scene.Layers.Count);
            }
        }

        private void UpdateScene()
        {
            Dispatcher.UIThread.VerifyAccess();

            try
            {
                if (_root.IsVisible)
                {
                    var sceneRef = RefCountable.Create(_scene?.Item.CloneScene() ?? new Scene(_root));
                    var scene = sceneRef.Item;

                    if (_dirty == null)
                    {
                        _dirty = new DirtyVisuals();
                        _sceneBuilder.UpdateAll(scene);
                    }
                    else if (_dirty.Count > 0)
                    {
                        foreach (var visual in _dirty)
                        {
                            _sceneBuilder.Update(scene, visual);
                        }
                    }

                    var oldScene = Interlocked.Exchange(ref _scene, sceneRef);
                    oldScene?.Dispose();

                    _dirty.Clear();
                    (_root as IRenderRoot)?.Invalidate(new Rect(scene.Size));
                }
                else
                {
                    var oldScene = Interlocked.Exchange(ref _scene, null);
                    oldScene?.Dispose();
                }
            }
            finally
            {
                _updateQueued = false;
            }
        }

        private void OnRenderLoopTick(object sender, EventArgs e)
        {
            if (Monitor.TryEnter(_rendering))
            {
                try
                {
                    if (!_updateQueued && (_dirty == null || _dirty.Count > 0))
                    {
                        _updateQueued = true;
                        _dispatcher.Post(UpdateScene, DispatcherPriority.Render);
                    }
                    
                    using (var scene = _scene?.Clone())
                    {
                        Render(scene?.Item);
                    }
                }
                catch { }
                finally
                {
                    Monitor.Exit(_rendering);
                }
            }
        }

        private IRef<IRenderTargetBitmapImpl> GetOverlay(
            IDrawingContextImpl parentContext,
            Size size,
            double scaling)
        {
            var pixelSize = size * scaling;

            if (_overlay == null ||
                _overlay.Item.PixelWidth != pixelSize.Width ||
                _overlay.Item.PixelHeight != pixelSize.Height)
            {
                _overlay?.Dispose();
                _overlay = RefCountable.Create(parentContext.CreateLayer(size));
            }

            return _overlay;
        }

        private void SaveDebugFrames(int id)
        {
            var index = 0;

            foreach (var layer in Layers)
            {
                var fileName = Path.Combine(DebugFramesPath, $"frame-{id}-layer-{index++}.png");
                layer.Bitmap.Item.Save(fileName);
            }
        }
    }
}
