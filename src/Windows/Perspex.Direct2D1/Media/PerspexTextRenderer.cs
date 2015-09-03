// -----------------------------------------------------------------------
// <copyright file="PerspexTextRenderer.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1.Media
{
    using System;
    using SharpDX;
    using SharpDX.Direct2D1;
    using SharpDX.DirectWrite;

    internal class PerspexTextRenderer : TextRenderer
    {
        private DrawingContext context;

        private RenderTarget renderTarget;

        private Brush foreground;

        public PerspexTextRenderer(
            DrawingContext context,
            RenderTarget target,
            Brush foreground)
        {
            this.context = context;
            this.renderTarget = target;
            this.foreground = foreground;
        }

        public IDisposable Shadow
        {
            get;
            set;
        }

        public void Dispose()
        {
        }

        public Result DrawGlyphRun(
            object clientDrawingContext,
            float baselineOriginX,
            float baselineOriginY,
            MeasuringMode measuringMode,
            GlyphRun glyphRun,
            GlyphRunDescription glyphRunDescription,
            ComObject clientDrawingEffect)
        {
            var wrapper = clientDrawingEffect as BrushWrapper;

            // TODO: Work out how to get the size below rather than passing new Size().
            var brush = (wrapper == null) ?
                this.foreground :
                this.context.CreateBrush(wrapper.Brush, new Size());

            this.renderTarget.DrawGlyphRun(
                new Vector2(baselineOriginX, baselineOriginY),
                glyphRun,
                brush,
                measuringMode);

            if (wrapper != null)
            {
                brush.Dispose();
            }

            return Result.Ok;
        }

        public Result DrawInlineObject(object clientDrawingContext, float originX, float originY, InlineObject inlineObject, bool isSideways, bool isRightToLeft, ComObject clientDrawingEffect)
        {
            throw new NotImplementedException();
        }

        public Result DrawStrikethrough(object clientDrawingContext, float baselineOriginX, float baselineOriginY, ref Strikethrough strikethrough, ComObject clientDrawingEffect)
        {
            throw new NotImplementedException();
        }

        public Result DrawUnderline(object clientDrawingContext, float baselineOriginX, float baselineOriginY, ref Underline underline, ComObject clientDrawingEffect)
        {
            throw new NotImplementedException();
        }

        public Matrix3x2 GetCurrentTransform(object clientDrawingContext)
        {
            return this.renderTarget.Transform;
        }

        public float GetPixelsPerDip(object clientDrawingContext)
        {
            return this.renderTarget.DotsPerInch.Width / 96;
        }

        public bool IsPixelSnappingDisabled(object clientDrawingContext)
        {
            return false;
        }
    }
}
