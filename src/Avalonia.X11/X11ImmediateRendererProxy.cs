using System;
using System.Collections.Generic;
using Avalonia.Rendering;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.X11
{
    public class X11ImmediateRendererProxy : IRenderer, IRenderLoopTask
    {
        private readonly IRenderLoop _loop;
        private ImmediateRenderer _renderer;
        private bool _invalidated;
        private bool _running;
        private object _lock = new object();

        public X11ImmediateRendererProxy(Visual root, IRenderLoop loop)
        {
            _loop = loop;
            _renderer = new ImmediateRenderer(root);
            
        }

        public void Dispose()
        {
            _running = false;
            _renderer.Dispose();
        }

        public bool DrawFps
        {
            get => _renderer.DrawFps;
            set => _renderer.DrawFps = value;
        }

        public bool DrawDirtyRects
        {
            get => _renderer.DrawDirtyRects;
            set => _renderer.DrawDirtyRects = value;
        }

        public event EventHandler<SceneInvalidatedEventArgs> SceneInvalidated
        {
            add => _renderer.SceneInvalidated += value;
            remove => _renderer.SceneInvalidated -= value;
        }

        public void AddDirty(Visual visual)
        {
            lock (_lock)
                _invalidated = true;
            _renderer.AddDirty(visual);
        }

        public IEnumerable<Visual> HitTest(Point p, Visual root, Func<Visual, bool> filter)
        {
            return _renderer.HitTest(p, root, filter);
        }

        public Visual HitTestFirst(Point p, Visual root, Func<Visual, bool> filter)
        {
            return _renderer.HitTestFirst(p, root, filter);
        }

        public void RecalculateChildren(Visual visual)
        {
            _renderer.RecalculateChildren(visual);
        }

        public void Resized(Size size)
        {
            _renderer.Resized(size);
        }

        public void Paint(Rect rect)
        {
            _invalidated = false;
            _renderer.Paint(rect);
        }

        public void Start()
        {
            _running = true;
            _loop.Add(this);
            _renderer.Start();
        }

        public void Stop()
        {
            _running = false;
            _loop.Remove(this);
            _renderer.Stop();
        }

        public bool NeedsUpdate => false;
        public void Update(TimeSpan time)
        {
            
        }

        public void Render()
        {
            if (_invalidated)
            {
                lock (_lock)
                    _invalidated = false;
                Dispatcher.UIThread.Post(() =>
                {
                    if (_running)
                        Paint(new Rect(0, 0, 100000, 100000));
                });
            }
        }
    }
}
