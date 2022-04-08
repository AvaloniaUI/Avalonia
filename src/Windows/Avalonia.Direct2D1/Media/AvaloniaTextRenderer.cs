using System;
using System.Numerics;
using System.Runtime.InteropServices;
using SharpGen.Runtime;
using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.DirectWrite;

namespace Avalonia.Direct2D1.Media
{
    internal class AvaloniaTextRenderer : TextRendererBase
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

        public override Result DrawGlyphRun(
            IntPtr clientDrawingContext,
            float baselineOriginX,
            float baselineOriginY,
            MeasuringMode measuringMode,
            GlyphRun glyphRun,
            GlyphRunDescription glyphRunDescription,
            IUnknown clientDrawingEffect)
        {
            var comObject = (ComObject)clientDrawingEffect;
            var wrapper = (BrushWrapper)Marshal.GetObjectForIUnknown(comObject.NativePointer); 

            // TODO: Work out how to get the size below rather than passing new Size().
            var brush = (wrapper == null) ?
                _foreground :
                _context.CreateBrush(wrapper.Brush, new Size()).PlatformBrush;

            _renderTarget.DrawGlyphRun(
                new System.Numerics.Vector2(baselineOriginX, baselineOriginY),
                glyphRun,
                brush,
                measuringMode);

            if (wrapper != null)
            {
                brush.Dispose();
            }

            return Result.Ok;
        }

        public override Matrix3x2 GetCurrentTransform(IntPtr clientDrawingContext)
        {
            return _renderTarget.Transform;
        }

        public override float GetPixelsPerDip(IntPtr clientDrawingContext)
        {
            return _renderTarget.Dpi.Width / 96;
        }
    }
}
