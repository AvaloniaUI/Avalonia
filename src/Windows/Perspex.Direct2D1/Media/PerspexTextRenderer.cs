﻿// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace Perspex.Direct2D1.Media
{
    internal class PerspexTextRenderer : TextRenderer
    {
        private readonly DrawingContext _context;

        private readonly RenderTarget _renderTarget;

        private readonly Brush _foreground;

        public PerspexTextRenderer(
            DrawingContext context,
            RenderTarget target,
            Brush foreground)
        {
            _context = context;
            _renderTarget = target;
            _foreground = foreground;
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
                _foreground :
                _context.CreateBrush(wrapper.Brush, new Size()).PlatformBrush;

            _renderTarget.DrawGlyphRun(
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
            return _renderTarget.Transform;
        }

        public float GetPixelsPerDip(object clientDrawingContext)
        {
            return _renderTarget.DotsPerInch.Width / 96;
        }

        public bool IsPixelSnappingDisabled(object clientDrawingContext)
        {
            return false;
        }
    }
}
