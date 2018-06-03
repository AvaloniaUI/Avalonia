using Avalonia.Media;

namespace Avalonia.Visuals.Effects
{
    public interface IDropShadowEffectImpl: IEffectImpl
    {
        /// <summary>
        /// Gets or sets X offset
        /// </summary>
        double OffsetX { get; set; }

        /// <summary>
        /// Gets or set Y offset
        /// </summary>
        double OffsetY { get; set; }

        /// <summary>
        /// Gets or sets Blur
        /// </summary>
        double Blur { get; set; }

        /// <summary>
        /// Gets or sets Color
        /// </summary>
        Color Color { get; set; }
    }
}
