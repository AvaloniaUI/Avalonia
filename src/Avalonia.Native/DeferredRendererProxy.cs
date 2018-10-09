// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Rendering;
using Avalonia.VisualTree;

namespace Avalonia.Native
{
    public class DeferredRendererProxy : IRenderer, IRenderLoopTask, IRenderLoop
    {
        object _lock = new object();
        void IRenderLoop.Add(IRenderLoopTask i)
        {
            AvaloniaLocator.Current.GetService<IRenderLoop>().Add(this);
        }

        void IRenderLoop.Remove(IRenderLoopTask i)
        {
            AvaloniaLocator.Current.GetService<IRenderLoop>().Remove(this);
        }

        private DeferredRenderer _renderer;
        private IRenderLoopTask _rendererTask;

        public DeferredRendererProxy(IRenderRoot root)
        {
            _renderer = new DeferredRenderer(root, this);
            _rendererTask = (IRenderLoopTask)_renderer;
        }

        public bool DrawFps{
            get => _renderer.DrawFps;
            set => _renderer.DrawFps = value;
        }
        public bool DrawDirtyRects 
        {
            get => _renderer.DrawDirtyRects;
            set => _renderer.DrawDirtyRects = value;
        }

        public bool NeedsUpdate => _rendererTask.NeedsUpdate;

        public void AddDirty(IVisual visual) => _renderer.AddDirty(visual);

        public void Dispose() => _renderer.Dispose();

        public IEnumerable<IVisual> HitTest(Point p, IVisual root, Func<IVisual, bool> filter)
        {
            return _renderer.HitTest(p, root, filter);
        }

        public void Paint(Rect rect)
        {
            if (NeedsUpdate)
            {
                Update(TimeSpan.FromMilliseconds(Environment.TickCount));
            }

            Render();
        }

        public void Resized(Size size) => _renderer.Resized(size);

        public void Start() => _renderer.Start();

        public void Stop() => _renderer.Stop();

        public void Update(TimeSpan time)
        {
            _rendererTask.Update(time);
        }

        public void Render()
        {
            lock(_lock)
            {
                _rendererTask.Render();
            }
        }
    }
}
