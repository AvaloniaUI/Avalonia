// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{
    public class Renderer : IDisposable, IRenderer
    {
        private readonly IRenderLoop _renderLoop;
        private readonly IRenderRoot _root;
        private IRenderTarget _renderTarget;
        private bool _dirty;

        public Renderer(IRenderRoot root, IRenderLoop renderLoop)
        {
            Contract.Requires<ArgumentNullException>(root != null);

            _root = root;
            _renderLoop = renderLoop;
            _renderLoop.Tick += OnRenderLoopTick;
        }

        public void AddDirty(IVisual visual)
        {
            _dirty = true;
        }

        public void Dispose()
        {
            _renderLoop.Tick -= OnRenderLoopTick;
        }

        public void Render(Rect rect)
        {
            if (_renderTarget == null)
            {
                _renderTarget = _root.CreateRenderTarget();
            }

            try
            {
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
            }
        }

        private void OnRenderLoopTick(object sender, EventArgs e)
        {
            if (_dirty)
            {
                _root.Invalidate(new Rect(_root.ClientSize));
            }
        }
    }
}
