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
        private readonly IRenderLoop _renderLoop;
        private readonly IRenderRoot _root;
        private Scene _scene;
        private IRenderTarget _renderTarget;
        private List<IVisual> _dirty;
        private DirtyRects _dirtyRects;
        private bool _updateQueued;
        private bool _rendering;

        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private int _totalFrames;
        private int _framesThisSecond;
        private int _fps;
        private TimeSpan _lastFpsUpdate;

        public DeferredRenderer(IRenderRoot root, IRenderLoop renderLoop)
        {
            Contract.Requires<ArgumentNullException>(root != null);

            _root = root;
            _scene = new Scene(root);

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

        private void Render(IDrawingContextImpl context, IVisualNode node, Rect clipBounds)
        {
            clipBounds = node.ClipBounds.Intersect(clipBounds);

            if (!clipBounds.IsEmpty)
            {
                node.Render(context);

                foreach (var child in node.Children)
                {
                    var visualChild = child as IVisualNode;

                    if (visualChild != null)
                    {
                        Render(context, visualChild, clipBounds);
                    }
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
            using (
                var txt = new FormattedText($"Frame #{_totalFrames} FPS: {_fps} Updates: {count}", "Arial", 18,
                    FontStyle.Normal,
                    TextAlignment.Left,
                    FontWeight.Normal,
                    TextWrapping.NoWrap))
            {
                context.Transform = Matrix.Identity;
                context.FillRectangle(Brushes.White, new Rect(pt, txt.Measure()));
                context.DrawText(Brushes.Black, pt, txt.PlatformImpl);
            }
        }

        private void UpdateScene()
        {
            Dispatcher.UIThread.VerifyAccess();

            try
            {
                var scene = _scene.Clone();
                var dirtyRects = new DirtyRects();

                if (_dirty == null)
                {
                    _dirty = new List<IVisual>();
                    SceneBuilder.UpdateAll(scene);
                    dirtyRects.Add(new Rect(_root.ClientSize));
                }
                else if (_dirty.Count > 0)
                {
                    foreach (var visual in _dirty)
                    {
                        SceneBuilder.Update(scene, visual, dirtyRects);
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
                Dispatcher.UIThread.InvokeAsync(UpdateScene, DispatcherPriority.Render);
                _updateQueued = true;
            }

            _rendering = true;

            Scene scene;
            DirtyRects dirtyRects;

            lock (_scene)
            {
                scene = _scene;
                dirtyRects = _dirtyRects;
            }

            try
            {
                if (_renderTarget == null)
                {
                    _renderTarget = _root.CreateRenderTarget();
                }

                using (var context = _renderTarget.CreateDrawingContext())
                {
                    int updateCount = 0;

                    _totalFrames++;

                    if (dirtyRects != null)
                    {
                        foreach (var rect in dirtyRects)
                        {
                            context.PushClip(rect);
                            Render(context, _scene.Root, rect);
                            context.PopClip();
                            ++updateCount;
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
    }
}
