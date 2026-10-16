using Avalonia.Media;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator for <see cref="FontVariationSettings"/>: per-axis linear interpolation
    /// when both keyframes set the same axes, discrete otherwise (CSS semantics — see
    /// <see cref="FontVariationSettings.Interpolate"/>).
    /// </summary>
    internal class FontVariationSettingsAnimator : Animator<FontVariationSettings?>
    {
        public override FontVariationSettings? Interpolate(double progress,
            FontVariationSettings? oldValue, FontVariationSettings? newValue)
        {
            return FontVariationSettings.Interpolate(oldValue, newValue, progress);
        }
    }
}
