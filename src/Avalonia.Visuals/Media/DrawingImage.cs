using Avalonia.Platform;
using Avalonia.Visuals.Media.Imaging;

namespace Avalonia.Media
{
    public class DrawingImage : AvaloniaObject, IImage
    {
        public static readonly StyledProperty<Drawing> DrawingProperty =
            AvaloniaProperty.Register<DrawingImage, Drawing>(nameof(Drawing));

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
            Drawing?.Draw(context);
        }
    }
}
