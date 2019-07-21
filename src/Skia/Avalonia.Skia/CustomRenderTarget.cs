// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Platform;
using Avalonia.Rendering;

namespace Avalonia.Skia
{
    /// <summary>
    /// Adapts <see cref="ICustomSkiaRenderTarget"/> to be used within Skia rendering pipeline.
    /// </summary>
    internal class CustomRenderTarget : IRenderTarget
    {
        private readonly ICustomSkiaRenderTarget _renderTarget;

        public CustomRenderTarget(ICustomSkiaRenderTarget renderTarget)
        {
            _renderTarget = renderTarget;
        }

        public void Dispose()
        {
            _renderTarget.Dispose();
        }

        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            ICustomSkiaRenderSession session = _renderTarget.BeginRendering();

            var nfo = new DrawingContextImpl.CreateInfo
            {
                GrContext = session.GrContext,
                Canvas = session.Canvas,
                Dpi = SkiaPlatform.DefaultDpi * session.ScaleFactor,
                VisualBrushRenderer = visualBrushRenderer,
                DisableTextLcdRendering = true
            };

            return new DrawingContextImpl(nfo, session);
        }
    }
}
