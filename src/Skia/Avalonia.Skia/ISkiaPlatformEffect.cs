using Avalonia.Visuals.Effects;
using SkiaSharp;

namespace Avalonia.Skia
{
    public interface ISkiaPlatformEffectImpl: IEffectImpl
    {
        void Render(SKPaint paint);
    }
}