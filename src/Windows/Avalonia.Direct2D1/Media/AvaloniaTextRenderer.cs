// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;

namespace Avalonia.Direct2D1.Media
{
    internal class AvaloniaTextRenderer : TextRenderer
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

        public IDisposable Shadow
        {
            get;
            set;
        }

        public void Dispose()
        {
            Shadow?.Dispose();
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
            if (glyphRun.Indices.Length == 0)
            {
                return Result.Ok;
            }

            var baselineOrigin = new RawVector2(baselineOriginX, baselineOriginY);

            Brush brush = null;

            if (clientDrawingEffect != null)
            {
                if (clientDrawingEffect is BrushWrapper wrapper)
                {
                    // TODO: Work out how to get the size below rather than passing new Size().
                    brush = _context.CreateBrush(wrapper.Brush, new Size()).PlatformBrush;
                }
            }
            else
            {
                brush = _foreground;
            }

            var factory = AvaloniaLocator.Current.GetService<SharpDX.DirectWrite.Factory>();

            var factory2 = factory.QueryInterface<SharpDX.DirectWrite.Factory2>();

            if (factory2 != null)
            {
                var typeface = glyphRun.FontFace.QueryInterface<FontFace2>();

                if (typeface != null && typeface.IsColorFont)
                {
                    ColorGlyphRunEnumerator glyphRunEnumerator = null;

                    try
                    {
                        factory2.TranslateColorGlyphRun(
                            baselineOriginX,
                            baselineOriginY,
                            glyphRun,
                            glyphRunDescription,
                            measuringMode,
                            null,
                            0,
                            out glyphRunEnumerator);
                    }
                    catch (SharpDXException)
                    {
                        // No color glyphs
                    }

                    if (glyphRunEnumerator != null)
                    {
                        RawBool hasRun;

                        while (true)
                        {
                            glyphRunEnumerator.MoveNext(out hasRun);

                            if (!hasRun)
                            {
                                break;
                            }

                            var colorGlyphRun = glyphRunEnumerator.CurrentRun;

                            brush = new SolidColorBrush(_renderTarget, colorGlyphRun.RunColor);

                            _renderTarget.DrawGlyphRun(
                                new RawVector2 { X = colorGlyphRun.BaselineOriginX, Y = colorGlyphRun.BaselineOriginY },
                                glyphRun,
                                brush,
                                measuringMode);

                            glyphRunEnumerator.MoveNext(out hasRun);
                        }
                    }
                }
                else
                {
                    _renderTarget.DrawGlyphRun(baselineOrigin, glyphRun, brush, measuringMode);
                }
            }
            else
            {
                _renderTarget.DrawGlyphRun(baselineOrigin, glyphRun, brush, measuringMode);
            }

            if (clientDrawingEffect != null)
            {
                brush?.Dispose();
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

        public RawMatrix3x2 GetCurrentTransform(object clientDrawingContext)
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
