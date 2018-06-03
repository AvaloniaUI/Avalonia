using Avalonia.Visuals.Effects;
using SkiaSharp;

namespace Avalonia.Skia.Effects
{
    public interface ISkiaPlatformEffectImpl
    {
        void Render(SKPaint paint);
    }
}