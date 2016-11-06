using System;
using System.Diagnostics;
using System.Linq;
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
        private bool _needsUpdate;
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
            if (!_needsUpdate)
            {
                _needsUpdate = true;
                Dispatcher.UIThread.InvokeAsync(UpdateScene, DispatcherPriority.Render);
            }
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
            if (_renderTarget == null)
            {
                _renderTarget = _root.CreateRenderTarget();
            }

            try
            {
                _totalFrames++;

                using (var context = _renderTarget.CreateDrawingContext())
                {
                    _scene.Root.Render(context);

                    if (DrawFps)
                    {
                        RenderFps(context);
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

            _scene = SceneBuilder.Update(_scene);
            _needsUpdate = false;
            _needsRender = true;
            _root.Invalidate(new Rect(_root.ClientSize));
        }

        private void OnRenderLoopTick(object sender, EventArgs e)
        {
            //if (_needsRender)
            //{
            //    _needsRender = false;
            //}
        }
    }
}
