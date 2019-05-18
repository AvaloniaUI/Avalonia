// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;

namespace Avalonia.Direct2D1.Media
{
    using System.Linq;

    internal class AvaloniaTextRenderer : TextRendererBase
    {
        private readonly DrawingContextImpl _context;

        private readonly SharpDX.Direct2D1.RenderTarget _renderTarget;

        private readonly Brush _foreground;

        public AvaloniaTextRenderer(
            DrawingContextImpl context,
            SharpDX.Direct2D1.RenderTarget target,
            Brush foreground)
        {
            _context = context;
            _renderTarget = target;
            _foreground = foreground;
        }

        public override Result DrawGlyphRun(
            object clientDrawingContext,
            float baselineOriginX,
            float baselineOriginY,
            MeasuringMode measuringMode,
            GlyphRun glyphRun,
            GlyphRunDescription glyphRunDescription,
            ComObject clientDrawingEffect)
        {
            var wrapper = clientDrawingEffect as BrushWrapper;

            var width = glyphRun.Advances.Sum();

            var height = (glyphRun.FontFace.Metrics.Descent + glyphRun.FontFace.Metrics.Ascent
                                                            + glyphRun.FontFace.Metrics.LineGap) / 96.0;

            var brush = (wrapper == null) ?
                _foreground :
                _context.CreateBrush(wrapper.Brush, new Size(width, height)).PlatformBrush;

            _renderTarget.DrawGlyphRun(
                new RawVector2 { X = baselineOriginX, Y = baselineOriginY },
                glyphRun,
                brush,
                measuringMode);

            if (wrapper != null)
            {
                brush.Dispose();
            }

            return Result.Ok;
        }

        public override Result DrawInlineObject(object clientDrawingContext, float originX, float originY, InlineObject inlineObject, bool isSideways, bool isRightToLeft, ComObject clientDrawingEffect)
        {
            if(inlineObject != null)
            {
                inlineObject.Draw(_renderTarget, this, originX, originY, isSideways, isRightToLeft, clientDrawingEffect);
            }        

            return Result.Ok;
        }

        public override RawMatrix3x2 GetCurrentTransform(object clientDrawingContext)
        {
            return _renderTarget.Transform;
        }

        public override float GetPixelsPerDip(object clientDrawingContext)
        {
            return _renderTarget.DotsPerInch.Width / 96;
        }
    }
}
