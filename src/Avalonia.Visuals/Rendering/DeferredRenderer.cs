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
        private Scene _scene;
        private IRenderTarget _renderTarget;
        private List<IVisual> _dirty;
        private LayerDirtyRects _dirtyRects;
        private bool _updateQueued;
        private bool _rendering;

        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private int _totalFrames;
        private int _framesThisSecond;
        private int _fps;
        private TimeSpan _lastFpsUpdate;

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
            _layers = new RenderLayers(layerFactory ?? new DefaultRenderLayerFactory());

            if (renderLoop != null)
            {
                _renderLoop = renderLoop;
                _renderLoop.Tick += OnRenderLoopTick;
            }
        }

        public bool DrawFps { get; set; }

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

        private void RenderFps(IDrawingContextImpl context, int count)
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
            var txt = new FormattedText($"Frame #{_totalFrames} FPS: {_fps} Updates: {count}", "Arial", 18,
                Size.Infinity,
                FontStyle.Normal,
                TextAlignment.Left,
                FontWeight.Normal,
                TextWrapping.NoWrap);
            context.Transform = Matrix.Identity;
            context.FillRectangle(Brushes.White, new Rect(pt, txt.Measure()));
            context.DrawText(Brushes.Black, pt, txt.PlatformImpl);
        }

        private void UpdateScene()
        {
            Dispatcher.UIThread.VerifyAccess();

            try
            {
                var scene = _scene.Clone();
                var dirtyRects = new LayerDirtyRects();

                if (_dirty == null)
                {
                    _dirty = new List<IVisual>();
                    _sceneBuilder.UpdateAll(scene, dirtyRects);
                }
                else if (_dirty.Count > 0)
                {
                    foreach (var visual in _dirty)
                    {
                        _sceneBuilder.Update(scene, visual, dirtyRects);
                    }

                    dirtyRects.Coalesce();
                }

                lock (_scene)
                {
                    _scene = scene;
                    _dirtyRects = dirtyRects.IsEmpty ? null : dirtyRects;
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

            if (!_updateQueued && (_dirty == null || _dirty.Count > 0 || _dirtyRects != null))
            {
                _updateQueued = true;
                _dispatcher.InvokeAsync(UpdateScene, DispatcherPriority.Render);
            }

            _rendering = true;
            _totalFrames++;

            Scene scene;
            LayerDirtyRects dirtyRects;
            int updateCount = 0;

            lock (_scene)
            {
                scene = _scene;
                dirtyRects = _dirtyRects;
            }

            var toRemove = new List<IVisual>();

            if (dirtyRects != null)
            {
                foreach (var layer in dirtyRects)
                {
                    var renderTarget = GetRenderTargetForLayer(layer.Key);
                    var node = (VisualNode)scene.FindNode(layer.Key);

                    using (var context = renderTarget.CreateDrawingContext())
                    {
                        foreach (var rect in layer.Value)
                        {
                            context.PushClip(rect);
                            Render(context, node, layer.Key, rect);
                            context.PopClip();
                            ++updateCount;
                        }
                    }
                }

                _layers.RemoveUnused(scene);
            }

            try
            {
                if (_renderTarget == null)
                {
                    _renderTarget = _root.CreateRenderTarget();
                }

                using (var context = _renderTarget.CreateDrawingContext())
                {
                    if (dirtyRects != null)
                    {
                        var rect = new Rect(_root.ClientSize);

                        foreach (var layer in _layers)
                        {
                            context.DrawImage(layer.Bitmap, layer.LayerRoot.Opacity, rect, rect);
                        }
                    }

                    if (DrawFps)
                    {
                        RenderFps(context, updateCount);
                    }
                }
            }
            catch (RenderTargetCorruptedException ex)
            {
                Logging.Logger.Information("Renderer", this, "Render target was corrupted. Exception: {0}", ex);
                _renderTarget.Dispose();
                _renderTarget = null;
            }

            _rendering = false;
        }

        private IRenderTargetBitmapImpl GetRenderTargetForLayer(IVisual layerRoot)
        {
            return (_layers.Get(layerRoot) ?? _layers.Add(layerRoot, _root.ClientSize)).Bitmap;
        }
    }
}
