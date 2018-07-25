using Avalonia.Visuals.Effects;
using SkiaSharp;

namespace Avalonia.Skia.Effects
{
    public interface ISkiaPlatformEffectImpl: IEffectImpl
    {
        void Render(SKCanvas canvas, SKBitmap bitmap);
    }
}