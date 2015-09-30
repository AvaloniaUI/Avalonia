using Perspex.Media;
using System;
using System.Linq;
using System.Text;
using Perspex.Media.Imaging;
using UIKit;
using CoreGraphics;
using Perspex.Platform;
using System.Reactive.Disposables;
using Foundation;
using CoreText;

namespace Perspex.iOS.Rendering
{
    public class DrawingContext : IDrawingContext, IDisposable
    {
        CGContext _nativeContext;

        public DrawingContext()
        {
            _nativeContext = UIGraphics.GetCurrentContext();
        }

        public Matrix CurrentTransform
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void Dispose()
        {
            _nativeContext.Dispose();
            _nativeContext = null;
        }

        /// <summary>
        /// Draws a geometry.
        /// </summary>
        /// <param name="brush">The fill brush.</param>
        /// <param name="pen">The stroke pen.</param>
        /// <param name="geometry">The geometry.</param>
        public void DrawGeometry(Brush brush, Pen pen, Geometry geometry)
        {
            var impl = geometry.PlatformImpl as StreamGeometryImpl;

            // Do we need to do this in iOS, and why did Cairo implementation not include the pen stroke
            // in this transformation?
            //
            //using (var pop = PushTransform(impl.Transform))
            //{
            _nativeContext.AddPath(impl.Path);

            if (brush != null)
            {
                using (var b = SetBrush(brush, geometry.Bounds.Size, BrushUsage.Fill))
                {
                    //if (pen != null)
                    //    _nativeContext.FillPreserve();
                    //else
                        _nativeContext.FillPath();
                }
            }
            //}

            if (pen != null)
            {
                using (var p = SetPen(pen, geometry.Bounds.Size))
                {
                    _nativeContext.StrokePath();
                }
            }

            // this might be more performance
            //_nativeContext.DrawPath(CGPathDrawingMode.FillStroke);
        }

        public void DrawImage(IBitmap source, double opacity, Rect sourceRect, Rect destRect)
        {
            throw new NotImplementedException();
        }

        public void DrawLine(Pen pen, Point p1, Point p2)
        {
            throw new NotImplementedException();
        }

        public void DrawRectangle(Pen pen, Rect rect, float cornerRadius = 0)
        {
            using (SetPen(pen, rect.Size))
            {
                if (cornerRadius == 0)
                {
                    _nativeContext.StrokeRect(rect.ToCoreGraphics());
                }
                else
                {
                    throw new NotImplementedException();

                    //_renderTarget.FillRoundedRectangle(
                    //    new RoundedRectangle
                    //    {
                    //        Rect = new RectangleF(
                    //                (float)rect.X,
                    //                (float)rect.Y,
                    //                (float)rect.Width,
                    //                (float)rect.Height),
                    //        RadiusX = cornerRadius,
                    //        RadiusY = cornerRadius
                    //    },
                    //    b.PlatformBrush);
                }
            }
        }

        public void DrawText(Brush foreground, Point origin, FormattedText text)
        {
            // Useful resource:
            // https://developer.apple.com/library/mac/documentation/StringsTextFonts/Conceptual/CoreText_Programming/LayoutOperations/LayoutOperations.html#//apple_ref/doc/uid/TP40005533-CH12-SW2

            var impl = text.PlatformImpl as FormattedTextImpl;

            using (SetBrush(foreground, new Size(0, 0), BrushUsage.Fill))
            {

                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // Single Line drawing (good for labels)
                //
                //using (var textLine = new CTLine(impl.AttributedString))
                //{
                //    // Flip the context coordinates, in iOS only. TODO: Confirm this is true
                //    _nativeContext.TranslateCTM(0, textLine.GetBounds(CTLineBoundsOptions.UseGlyphPathBounds).Height);
                //    _nativeContext.ScaleCTM(1.0f, -1.0f);

                //    _nativeContext.TextPosition = new CGPoint(0, 0);
                //    textLine.Draw(_nativeContext);
                //}

                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // Simple Paragraph layout
                //
                _nativeContext.TranslateCTM(0, (nfloat) impl.Constraint.Height);
                _nativeContext.ScaleCTM(1.0f, -1.0f);
                _nativeContext.TextMatrix = CGAffineTransform.MakeIdentity();

                // Create a path which bounds the area where you will be drawing text.
                // The path need not be rectangular.
                var path = new CGPath();
                path.AddRect(new CGRect(origin.ToCoreGraphics(), impl.Constraint.ToCoreGraphics()));

                var framesetter = impl.Framesetter;
                var frame = framesetter.GetFrame(new NSRange(), path, null);
                frame.Draw(_nativeContext);
            }

        }

        /// <summary>
        /// Draws a filled rectangle.
        /// </summary>
        /// <param name="brush">The brush.</param>
        /// <param name="rect">The rectangle bounds.</param>
        /// <param name="cornerRadius">The corner radius.</param>
        public void FillRectangle(Perspex.Media.Brush brush, Rect rect, float cornerRadius)
        {
            using (var b = SetBrush(brush, rect.Size, BrushUsage.Fill))
            {
                if (cornerRadius == 0)
                {
                    _nativeContext.FillRect(rect.ToCoreGraphics());
                }
                else
                {
                    throw new NotImplementedException();

                    //_renderTarget.FillRoundedRectangle(
                    //    new RoundedRectangle
                    //    {
                    //        Rect = new RectangleF(
                    //                (float)rect.X,
                    //                (float)rect.Y,
                    //                (float)rect.Width,
                    //                (float)rect.Height),
                    //        RadiusX = cornerRadius,
                    //        RadiusY = cornerRadius
                    //    },
                    //    b.PlatformBrush);
                }
            }
        }


        public IDisposable PushClip(Rect clip)
        {
            _nativeContext.SaveState();
            _nativeContext.ClipToRect(clip.ToCoreGraphics());
            return Disposable.Create(() => _nativeContext.RestoreState());
        }

        private float _currentOpacity = 1.0f;

        public IDisposable PushOpacity(double opacity)
        {
            // DirectX style using layers
            //if (opacity < 1)
            //{
            //    //var parameters = new CGLayer()
            //    //{
            //    //    ContentBounds = RectangleF.Infinite,
            //    //    MaskTransform = Matrix3x2.Identity,
            //    //    Opacity = (float)opacity,
            //    //};

            //    var layer = new CGLayer(_renderTarget);

            //    _nativeContext.BeginTransparencyLayer();
            //    _renderTarget.PushLayer(ref parameters, layer);

            //    return Disposable.Create(() =>
            //    {
            //        _renderTarget.PopLayer();
            //        layer.Dispose();
            //    });
            //}
            //else
            //{
            //    return Disposable.Empty;
            //}

            // Cairo style
            var previous = _currentOpacity;
            _currentOpacity = (float)opacity;
            _nativeContext.SetAlpha(_currentOpacity);

            return Disposable.Create(() =>
            {
                _currentOpacity = previous;
                _nativeContext.SetAlpha(_currentOpacity);
            });

        }

        public IDisposable PushTransform(Matrix matrix)
        {
            // i wonder if we should use Save/Restore state instead?
            //
            _nativeContext.ConcatCTM(matrix.ToCoreGraphics());

            return Disposable.Create(() =>
            {
                _nativeContext.ConcatCTM(matrix.Invert().ToCoreGraphics());
            });
        }

        private IDisposable SetBrush(Brush brush, Size destinationSize, BrushUsage usage)
        {
            _nativeContext.SaveState();

            var solid = brush as SolidColorBrush;
            var linearGradientBrush = brush as LinearGradientBrush;
            var radialGradientBrush = brush as RadialGradientBrush;
            var imageBrush = brush as ImageBrush;
            var visualBrush = brush as VisualBrush;
            BrushImpl impl = null;

            if (solid != null)
            {
                impl = new SolidColorBrushImpl(solid, _currentOpacity);
            }
            else if (linearGradientBrush != null)
            {
                throw new NotImplementedException();
                //impl = new LinearGradientBrushImpl(linearGradientBrush, destinationSize);
            }
            else if (radialGradientBrush != null)
            {
                throw new NotImplementedException();
                //impl = new RadialGradientBrushImpl(radialGradientBrush, destinationSize);
            }
            else if (imageBrush != null)
            {
                throw new NotImplementedException();
                //impl = new ImageBrushImpl(imageBrush, destinationSize);
            }
            else if (visualBrush != null)
            {
                throw new NotImplementedException();
                //impl = new VisualBrushImpl(visualBrush, destinationSize);
            }
            else
            {
                impl = new SolidColorBrushImpl(null, _currentOpacity);
            }

            impl.Apply(_nativeContext, usage);

            return Disposable.Create(() =>
            {
                impl.Dispose();
                _nativeContext.RestoreState();
            });
        }

        private IDisposable SetPen(Pen pen, Size destinationSize)
        {
            if (pen.DashStyle != null)
            {
                if (pen.DashStyle.Dashes != null && pen.DashStyle.Dashes.Count > 0)
                {
                    var cray = pen.DashStyle.Dashes.Select(d => (nfloat)d).ToArray();
                    _nativeContext.SetLineDash((float)pen.DashStyle.Offset, cray);
                }
            }

            _nativeContext.SetLineWidth((nfloat)pen.Thickness);
            _nativeContext.SetMiterLimit((nfloat)pen.MiterLimit);

            // CoreGraphics does not have StartLineCap, EndLineCap, and DashCap properties, whereas Direct2D does. 
            // TODO: Figure out a solution for this.
            _nativeContext.SetLineJoin(pen.LineJoin.ToCoreGraphics());
            _nativeContext.SetLineCap(pen.StartLineCap.ToCoreGraphics());

            if (pen.Brush == null)
                return Disposable.Empty;

            return SetBrush(pen.Brush, destinationSize, BrushUsage.Stroke);
        }
    }
}
