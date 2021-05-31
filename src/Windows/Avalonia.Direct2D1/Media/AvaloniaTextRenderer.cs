using System;
using System.Drawing;
using System.Numerics;
using SharpGen.Runtime;
using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.DirectWrite;

namespace Avalonia.Direct2D1.Media
{
    internal class AvaloniaTextRenderer : CallbackBase, IDWriteTextRenderer
    {
        private readonly DrawingContextImpl _context;

        private readonly ID2D1RenderTarget _renderTarget;

        private readonly ID2D1Brush _foreground;

        public AvaloniaTextRenderer(
            DrawingContextImpl context,
            ID2D1RenderTarget target,
            ID2D1Brush foreground)
        {
            _context = context;
            _renderTarget = target;
            _foreground = foreground;
        }

        public void DrawGlyphRun(
            IntPtr clientDrawingContext,
            float baselineOriginX,
            float baselineOriginY,
            MeasuringMode measuringMode,
            GlyphRun glyphRun,
            ref GlyphRunDescription glyphRunDescription,
            IUnknown clientDrawingEffect)
        {
            var wrapper = clientDrawingEffect as BrushWrapper;

            // TODO: Work out how to get the size below rather than passing new Size().
            var brush = (wrapper == null) ?
                _foreground :
                _context.CreateBrush(wrapper.Brush, new Size()).PlatformBrush;

            _renderTarget.DrawGlyphRun(
                new PointF { X = baselineOriginX, Y = baselineOriginY },
                glyphRun,
                brush,
                measuringMode);

            if (wrapper != null)
            {
                brush.Dispose();
            }
        }

        public void DrawUnderline(IntPtr clientDrawingContext, float baselineOriginX, float baselineOriginY, ref Underline underline,
                                  IUnknown clientDrawingEffect)
        {
            throw new SharpGenException(Result.NotImplemented);
        }

        public void DrawStrikethrough(IntPtr clientDrawingContext, float baselineOriginX, float baselineOriginY,
                                      ref Strikethrough strikethrough, IUnknown clientDrawingEffect)
        {
            throw new SharpGenException(Result.NotImplemented);
        }

        public void DrawInlineObject(IntPtr clientDrawingContext, float originX, float originY, IDWriteInlineObject inlineObject,
                                     RawBool isSideways, RawBool isRightToLeft, IUnknown clientDrawingEffect)
        {
            throw new SharpGenException(Result.NotImplemented);
        }

        public void GetCurrentTransform(IntPtr clientDrawingContext, out Matrix3x2 transform)
        {
            transform = _renderTarget.Transform;
        }

        public void GetPixelsPerDip(IntPtr clientDrawingContext, out float pixelsPerDip)
        {
            pixelsPerDip = _renderTarget.Dpi.X / 96;
        }

        public void IsPixelSnappingDisabled(IntPtr clientDrawingContext, out RawBool isDisabled)
        {
            isDisabled = false;
        }
    }
}
