using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;

namespace Avalonia.Direct2D1.Media
{
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

            // TODO: Work out how to get the size below rather than passing new Size().
            var brush = (wrapper == null) ?
                _foreground :
                _context.CreateBrush(wrapper.Brush, default).PlatformBrush;

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
