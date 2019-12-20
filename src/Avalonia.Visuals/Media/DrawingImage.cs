using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Visuals.Media.Imaging;

namespace Avalonia.Media
{
    public class DrawingImage : AvaloniaObject, IImage
    {
        public static readonly StyledProperty<Drawing> DrawingProperty =
            AvaloniaProperty.Register<DrawingImage, Drawing>(nameof(Drawing));

        [Content]
        public Drawing Drawing
        {
            get => GetValue(DrawingProperty);
            set => SetValue(DrawingProperty, value);
        }

        public Size Size => Drawing?.GetBounds().Size ?? default;

        public void Draw(
            DrawingContext context,
            double opacity,
            Rect sourceRect,
            Rect destRect,
            BitmapInterpolationMode bitmapInterpolationMode)
        {
            var drawing = Drawing;

            if (drawing == null)
            {
                return;
            }

            var bounds = drawing.GetBounds();
            var scale = Matrix.CreateScale(
                destRect.Width / sourceRect.Width,
                destRect.Height / sourceRect.Height);
            var translate = Matrix.CreateTranslation(
                -sourceRect.X + destRect.X - bounds.X,
                -sourceRect.Y + destRect.Y - bounds.Y);

            using (context.PushClip(destRect))
            using (context.PushPreTransform(translate * scale))
            {
                Drawing?.Draw(context);
            }
        }
    }
}
