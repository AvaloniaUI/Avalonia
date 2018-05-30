using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Visuals.Effects;
using SkiaSharp;

namespace Avalonia.Skia
{
    internal class DropShadowEffectImpl : ISkiaPlatformEffectImpl
    {
        public void Render(SKPaint paint)
        {
            using (var filter = SKImageFilter.CreateDropShadow(5, 5, 5, 5, new SKColor(255, 0, 0), SKDropShadowImageFilterShadowMode.DrawShadowAndForeground, null))
            {
                paint.IsAntialias = true;
                paint.ImageFilter = filter;
            }
        }
    }
}