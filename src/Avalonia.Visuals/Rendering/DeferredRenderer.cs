using System;
using System.Diagnostics;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Avalonia.Rendering
{
    public class DeferredRenderer : IRenderer
    {
        private readonly IRenderLoop _renderLoop;
        private readonly IRenderRoot _root;
        private Scene _scene;
        private IRenderTarget _renderTarget;
        private List<IVisual> _dirty = new List<IVisual>();
        private DirtyRects _dirtyRects;
        private bool _needsUpdate;
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
            // If the root of the scene has no children, then the scene is being set up; don't
            // bother filling the dirty list with every control in the window.
            if (_scene.Root.Children.Count > 0)
            {
                _dirty.Add(visual);
            }

            _needsUpdate = true;
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
            if (_renderLoop == null && _needsUpdate)
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
            clipBounds = node.Bounds.Intersect(clipBounds);

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

                if (_dirty.Count > 0)
                {
                    foreach (var visual in _dirty)
                    {
                        SceneBuilder.Update(scene, visual, dirtyRects);
                    }

                    dirtyRects.Coalesce();
                }
                else
                {
                    SceneBuilder.UpdateAll(scene);
                    dirtyRects.Add(new Rect(_root.ClientSize));
                }

                lock (_scene)
                {
                    _scene = scene;
                    _dirtyRects = dirtyRects;
                }

                _dirty.Clear();
                _needsUpdate = false;
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

            if (_needsUpdate && !_updateQueued)
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

            if (dirtyRects != null)
            {
                if (_renderTarget == null)
                {
                    _renderTarget = _root.CreateRenderTarget();
                }

                try
                {
                    _totalFrames++;

                    int count = 0;

                    using (var context = _renderTarget.CreateDrawingContext())
                    {
                        foreach (var rect in dirtyRects)
                        {
                            context.PushClip(rect);
                            Render(context, _scene.Root, rect);
                            context.PopClip();
                            ++count;
                        }

                        if (DrawFps)
                        {
                            RenderFps(context, count);
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

            _rendering = false;
        }
    }
}
