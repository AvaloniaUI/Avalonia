using System;
using System.Diagnostics;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Avalonia.Controls
{
    public class SlicedImage : Image
    {
        public double Left;
        public double Right;
        public double Top;
        public double Bottom;

        public static readonly DirectProperty<SlicedImage, double> LeftProperty =
            AvaloniaProperty.RegisterDirect<SlicedImage, double>(nameof(Left), o => o.Left, (o, v) => o.Left = v);
        
        public static readonly DirectProperty<SlicedImage, double> RightProperty =   
            AvaloniaProperty.RegisterDirect<SlicedImage, double>(nameof(Right), o => o.Right, (o, v) => o.Right = v);
        
        public static readonly DirectProperty<SlicedImage, double> TopProperty =    
            AvaloniaProperty.RegisterDirect<SlicedImage, double>(nameof(Top), o => o.Top, (o, v) => o.Top = v);
        
        public static readonly DirectProperty<SlicedImage, double> BottomProperty =
            AvaloniaProperty.RegisterDirect<SlicedImage, double>(nameof(Bottom), o => o.Bottom, (o, v) => o.Bottom = v);
        
        /// <summary>
        /// Renders the control.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        public override void Render(DrawingContext context)
        {
            var source = Source;

            if (source != null)
            {
                Rect viewPort = new Rect(Bounds.Size);
                Size sourceSize = new Size(source.PixelWidth, source.PixelHeight);
                Vector scale = Stretch.CalculateScaling(Bounds.Size, sourceSize);
                Size scaledSize = sourceSize * scale;
                Rect destRect = viewPort
                    .CenterRect(new Rect(scaledSize))
                    .Intersect(viewPort);

                Rect sourceRect = new Rect(sourceSize)
                    .CenterRect(new Rect(destRect.Size / scale));

                if (Left != 0 || Right != 0 || Top != 0 || Bottom != 0)
                {
                    DrawCenter(context, source, sourceRect, destRect);
                    DrawBorders(context, source, sourceRect, destRect);
                    DrawCorners(context, source, sourceRect, destRect);
                }
                else
                    context.DrawImage(source, 1, sourceRect, destRect);
            }
        }

        private void DrawCorners(DrawingContext context, IBitmap source, Rect sourceRect, Rect destRect)
        {
            //top left
            DrawImageWithOffset(context, source, sourceRect, destRect, new Rect(0, 0, Left, Top), new Rect(0, 0, Left, Top));
            //top right
            DrawImageWithOffset(context, source, sourceRect, destRect, new Rect(sourceRect.Width - Right, 0, Right, Top),
                                new Rect(destRect.Width - Right, 0, Right, Top));
            //bottom left
            DrawImageWithOffset(context, source, sourceRect, destRect, new Rect(0, sourceRect.Height - Bottom, Left, Bottom),
                                new Rect(0, destRect.Height - Bottom, Left, Bottom));
            //bottom right
            DrawImageWithOffset(context, source, sourceRect, destRect, new Rect(sourceRect.Width - Right, sourceRect.Height - Bottom, Right, Top),
                                new Rect(destRect.Width - Right, destRect.Height - Bottom, Right, Top));
        }

        private void DrawBorders(DrawingContext context, IBitmap source, Rect sourceRect, Rect destRect)
        {
            //top
            DrawImageWithOffset(context, source, sourceRect, destRect, new Rect(Left, 0, sourceRect.Width - (Left + Right), Top),
                                new Rect(Left, 0, destRect.Width - (Left + Right), Top));
            //left
            DrawImageWithOffset(context, source, sourceRect, destRect, new Rect(0, Top, Left, sourceRect.Height - (Top + Bottom)),
                                new Rect(0, Top, Left, destRect.Height - (Top + Bottom)));
            //bottom
            DrawImageWithOffset(context, source, sourceRect, destRect, new Rect(Left, sourceRect.Height - Bottom, sourceRect.Width - (Left + Right), Bottom),
                                new Rect(Left, destRect.Height - Bottom, destRect.Width - (Left + Right), Bottom));
            //right
            DrawImageWithOffset(context, source, sourceRect, destRect, new Rect(sourceRect.Width - Right, Top, Left, sourceRect.Height - (Top + Bottom)),
                                new Rect(destRect.Width - Right, Top, Left, destRect.Height - (Top + Bottom)));
        }

        private void DrawCenter(DrawingContext context, IBitmap source, Rect sourceRect, Rect destRect)
        {
            DrawImageWithOffset(context, source, sourceRect, destRect, new Rect(Left, Top, sourceRect.Width - Left - Right, sourceRect.Height - Top - Bottom),
                                new Rect(Left, Top, destRect.Width - Left - Right, destRect.Height - Top - Bottom));
        }

        private void DrawImageWithOffset(DrawingContext context, IBitmap source, Rect sourceRect, Rect destRect, Rect sourceOffset, Rect destOffset)
        {
            Rect newSource = OffsetPosAndCopySize(sourceOffset, sourceRect);

            Rect newDest = OffsetPosAndCopySize(destOffset, destRect);

            context.DrawImage(source, 1, newSource, newDest);
        }

        private Rect OffsetPosAndCopySize(Rect @from, Rect to)
        {
            Point pos = new Point(Math.Round(to.X + (@from.X)), Math.Round(to.Y + (@from.Y)));
            return new Rect(pos, from.Size);
        }
    }
}