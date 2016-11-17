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
        private ConcurrentQueue<Rect> _renderQueue = new ConcurrentQueue<Rect>();
        private bool _needsUpdate;
        private bool _updateQueued;
        private bool _needsRender;

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
            _renderLoop = renderLoop;
            _renderLoop.Tick += OnRenderLoopTick;
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
            _renderLoop.Tick -= OnRenderLoopTick;
        }

        public IEnumerable<IVisual> HitTest(Point p, Func<IVisual, bool> filter)
        {
            if (_needsUpdate)
            {
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
            using (
                var txt = new FormattedText("Frame #" + _totalFrames + " FPS: " + _fps, "Arial", 18,
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

                if (_dirty.Count > 0)
                {
                    var dirtyRects = new DirtyRects();

                    foreach (var visual in _dirty)
                    {
                        SceneBuilder.Update(scene, visual, dirtyRects);
                    }

                    foreach (var r in dirtyRects.Coalesce())
                    {
                        _renderQueue.Enqueue(r);
                    }

                    _dirty.Clear();
                }
                else
                {
                    SceneBuilder.UpdateAll(scene);
                    _renderQueue.Enqueue(new Rect(_root.ClientSize));
                }

                _scene = scene;

                _needsUpdate = false;
                _needsRender = true;
                _root.Invalidate(new Rect(_root.ClientSize));
            }
            finally
            {
                _updateQueued = false;
            }
        }

        private void OnRenderLoopTick(object sender, EventArgs e)
        {
            if (_needsUpdate && !_updateQueued)
            {
                Dispatcher.UIThread.InvokeAsync(UpdateScene, DispatcherPriority.Render);
                _updateQueued = true;
            }

            if (_needsRender)
            {
                if (_renderTarget == null)
                {
                    _renderTarget = _root.CreateRenderTarget();
                }

                try
                {
                    _totalFrames++;

                    using (var context = _renderTarget.CreateDrawingContext())
                    {
                        Rect rect;

                        while (_renderQueue.TryDequeue(out rect))
                        {
                            context.PushClip(rect);
                            Render(context, _scene.Root, rect);
                            context.PopClip();
                        }

                        if (DrawFps)
                        {
                            RenderFps(context);
                        }
                    }

                    _needsRender = false;
                }
                catch (RenderTargetCorruptedException ex)
                {
                    Logging.Logger.Information("Renderer", this, "Render target was corrupted. Exception: {0}", ex);
                    _renderTarget.Dispose();
                    _renderTarget = null;
                }
            }
        }
    }
}
