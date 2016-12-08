using System;
using System.Diagnostics;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Collections.Generic;

namespace Avalonia.Rendering
{
    public class DeferredRenderer : IRenderer
    {
        private readonly IDispatcher _dispatcher;
        private readonly IRenderLoop _renderLoop;
        private readonly IRenderRoot _root;
        private readonly ISceneBuilder _sceneBuilder;
        private readonly RenderLayers _layers;
        private readonly IRenderLayerFactory _layerFactory;

        private Scene _scene;
        private IRenderTarget _renderTarget;
        private HashSet<IVisual> _dirty;
        private IRenderTargetBitmapImpl _overlay;
        private bool _updateQueued;
        private bool _rendering;
        private int _lastSceneId = -1;

        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private int _totalFrames;
        private int _framesThisSecond;
        private int _fps;
        private TimeSpan _lastFpsUpdate;
        private DisplayDirtyRects _dirtyRectsDisplay = new DisplayDirtyRects();

        public DeferredRenderer(
            IRenderRoot root,
            IRenderLoop renderLoop,
            ISceneBuilder sceneBuilder = null,
            IRenderLayerFactory layerFactory = null,
            IDispatcher dispatcher = null)
        {
            Contract.Requires<ArgumentNullException>(root != null);

            _dispatcher = dispatcher ?? Dispatcher.UIThread;
            _root = root;
            _sceneBuilder = sceneBuilder ?? new SceneBuilder();
            _scene = new Scene(root);
            _layerFactory = layerFactory ?? new DefaultRenderLayerFactory();
            _layers = new RenderLayers(_layerFactory);

            if (renderLoop != null)
            {
                _renderLoop = renderLoop;
                _renderLoop.Tick += OnRenderLoopTick;
            }
        }

        public bool DrawFps { get; set; }
        public bool DrawDirtyRects { get; set; }

        public void AddDirty(IVisual visual)
        {
            _dirty?.Add(visual);
        }

        public void Dispose()
        {
            if (_renderLoop != null)
            {
                _renderLoop.Tick -= OnRenderLoopTick;
            }
        }

        public IEnumerable<IVisual> HitTest(Point p, Func<IVisual, bool> filter)
        {
            if (_renderLoop == null && (_dirty == null || _dirty.Count > 0))
            {
                // When unit testing the renderLoop may be null, so update the scene manually.
                UpdateScene();
            }

            return _scene.HitTest(p, filter);
        }

        public void Render(Rect rect)
        {
        }

        private void Render(IDrawingContextImpl context, VisualNode node, IVisual layer, Rect clipBounds)
        {
            if (node.LayerRoot == layer)
            {
                clipBounds = node.ClipBounds.Intersect(clipBounds);

                if (!clipBounds.IsEmpty)
                {
                    node.BeginRender(context);

                    foreach (var operation in node.DrawOperations)
                    {
                        operation.Render(context);
                    }

                    foreach (var child in node.Children)
                    {
                        Render(context, (VisualNode)child, layer, clipBounds);
                    }

                    node.EndRender(context);
                }
            }
        }

        private void RenderToLayers(Scene scene)
        {
            if (scene.Layers.HasDirty)
            {
                foreach (var layer in scene.Layers)
                {
                    var renderTarget = GetRenderTargetForLayer(layer.LayerRoot);
                    var node = (VisualNode)scene.FindNode(layer.LayerRoot);

                    using (var context = renderTarget.CreateDrawingContext())
                    {
                        foreach (var rect in layer.Dirty)
                        {
                            context.Transform = Matrix.Identity;
                            context.PushClip(rect);
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

        private void RenderOverlay()
        {
            if (DrawFps || DrawDirtyRects)
            {
                var overlay = GetOverlay(_root.ClientSize);

                using (var context = overlay.CreateDrawingContext())
                {
                    context.Clear(Colors.Transparent);

                    if (DrawFps)
                    {
                        RenderFps(context);
                    }

                    if (DrawDirtyRects)
                    {
                        RenderDirtyRects(context);
                    }
                }
            }
            else
            {
                _overlay?.Dispose();
                _overlay = null;
            }
        }

        private void RenderFps(IDrawingContextImpl context)
        {
            var now = _stopwatch.Elapsed;
            var elapsed = now - _lastFpsUpdate;

            _framesThisSecond++;

            if (elapsed.TotalSeconds > 1)
            {
                _fps = (int)(_framesThisSecond / elapsed.TotalSeconds);
                _framesThisSecond = 0;
                _lastFpsUpdate = now;
            }

            var pt = new Point(40, 40);
            var txt = new FormattedText($"Frame #{_totalFrames} FPS: {_fps}", "Arial", 18,
                Size.Infinity,
                FontStyle.Normal,
                TextAlignment.Left,
                FontWeight.Normal,
                TextWrapping.NoWrap);
            context.Transform = Matrix.Identity;
            context.FillRectangle(Brushes.White, new Rect(pt, txt.Measure()));
            context.DrawText(Brushes.Black, pt, txt.PlatformImpl);
        }

        private void RenderDirtyRects(IDrawingContextImpl context)
        {
            foreach (var r in _dirtyRectsDisplay)
            {
                var brush = new SolidColorBrush(Colors.Magenta, r.Opacity);
                context.FillRectangle(brush, r.Rect);
            }
        }

        private void RenderComposite(Scene scene)
        {
            try
            {
                if (_renderTarget == null)
                {
                    _renderTarget = _root.CreateRenderTarget();
                }

                using (var context = _renderTarget.CreateDrawingContext())
                {
                    var clientRect = new Rect(_root.ClientSize);

                    foreach (var layer in scene.Layers)
                    {
                        var renderLayer = _layers.Get(layer.LayerRoot);
                        context.DrawImage(renderLayer.Bitmap, layer.Opacity, clientRect, clientRect);
                    }

                    if (_overlay != null)
                    {
                        context.DrawImage(_overlay, 0.5, clientRect, clientRect);
                    }
                }
            }
            catch (RenderTargetCorruptedException ex)
            {
                Logging.Logger.Information("Renderer", this, "Render target was corrupted. Exception: {0}", ex);
                _renderTarget.Dispose();
                _renderTarget = null;
            }
        }

        private void UpdateScene()
        {
            Dispatcher.UIThread.VerifyAccess();

            try
            {
                var scene = _scene.Clone();

                if (_dirty == null)
                {
                    _dirty = new HashSet<IVisual>();
                    _sceneBuilder.UpdateAll(scene);
                }
                else if (_dirty.Count > 0)
                {
                    foreach (var visual in _dirty)
                    {
                        _sceneBuilder.Update(scene, visual);
                    }
                }

                lock (_scene)
                {
                    _scene = scene;
                }

                _dirty.Clear();
                _root.Invalidate(new Rect(_root.ClientSize));
            }
            finally
            {
                _updateQueued = false;
            }
        }

        private void OnRenderLoopTick(object sender, EventArgs e)
        {
            if (_rendering)
            {
                return;
            }

            if (!_updateQueued && (_dirty == null || _dirty.Count > 0))
            {
                _updateQueued = true;
                _dispatcher.InvokeAsync(UpdateScene, DispatcherPriority.Render);
            }

            _rendering = true;
            _totalFrames++;
            _dirtyRectsDisplay.Tick();

            Scene scene;

            lock (_scene)
            {
                scene = _scene;
            }

            if (scene.Id != _lastSceneId)
            {
                _layers.RemoveUnused(scene);
                RenderToLayers(scene);
                _lastSceneId = scene.Id;
            }

            RenderOverlay();
            RenderComposite(scene);

            _rendering = false;
        }

        private IRenderTargetBitmapImpl GetOverlay(Size size)
        {
            int width = (int)Math.Ceiling(size.Width);
            int height = (int)Math.Ceiling(size.Height);

            if (_overlay == null || _overlay.PixelWidth != width || _overlay.PixelHeight != height)
            {
                _overlay?.Dispose();
                _overlay = _layerFactory.CreateLayer(null, size);
            }

            return _overlay;
        }

        private IRenderTargetBitmapImpl GetRenderTargetForLayer(IVisual layerRoot)
        {
            return (_layers.Get(layerRoot) ?? _layers.Add(layerRoot, _root.ClientSize)).Bitmap;
        }
    }
}
