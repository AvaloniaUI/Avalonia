// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform;
using Avalonia.VisualTree;
using System.Collections.Generic;
using Avalonia.Threading;

namespace Avalonia.Rendering
{
    public class Renderer : IDisposable, IRenderer
    {
        private readonly IRenderLoop _renderLoop;
        private readonly IRenderRoot _root;
        private IRenderTarget _renderTarget;
        private bool _dirty;
        private bool _renderQueued;

        public Renderer(IRenderRoot root, IRenderLoop renderLoop)
        {
            Contract.Requires<ArgumentNullException>(root != null);

            _root = root;
            _renderLoop = renderLoop;
            _renderLoop.Tick += OnRenderLoopTick;
        }

        public bool DrawFps { get; set; }
        public bool DrawDirtyRects { get; set; }

        public void AddDirty(IVisual visual)
        {
            _dirty = true;
        }

        public void Dispose()
        {
            _renderLoop.Tick -= OnRenderLoopTick;
        }

        public IEnumerable<IVisual> HitTest(Point p, Func<IVisual, bool> filter)
        {
            return HitTest(_root, p, filter);
        }

        public void Render(Rect rect)
        {
            if (_renderTarget == null)
            {
                _renderTarget = _root.CreateRenderTarget();
            }

            try
            {
                RendererMixin.DrawFpsCounter = DrawFps;
                _renderTarget.Render(_root);
            }
            catch (RenderTargetCorruptedException ex)
            {
                Logging.Logger.Information("Renderer", this, "Render target was corrupted. Exception: {0}", ex);
                _renderTarget.Dispose();
                _renderTarget = null;
            }
            finally
            {
                _dirty = false;
                _renderQueued = false;
            }
        }

        static IEnumerable<IVisual> HitTest(
           IVisual visual,
           Point p,
           Func<IVisual, bool> filter)
        {
            Contract.Requires<ArgumentNullException>(visual != null);

            if (filter?.Invoke(visual) != false)
            {
                bool containsPoint = BoundsTracker.GetTransformedBounds((Visual)visual)?.Contains(p) == true;

                if ((containsPoint || !visual.ClipToBounds) && visual.VisualChildren.Count > 0)
                {
                    foreach (var child in visual.VisualChildren.SortByZIndex())
                    {
                        foreach (var result in HitTest(child, p, filter))
                        {
                            yield return result;
                        }
                    }
                }

                if (containsPoint)
                {
                    yield return visual;
                }
            }
        }

        private void OnRenderLoopTick(object sender, EventArgs e)
        {
            if (_dirty && !_renderQueued)
            {
                _renderQueued = true;
                Dispatcher.UIThread.InvokeAsync(() => Render(new Rect(_root.ClientSize)));
            }
        }
    }
}
