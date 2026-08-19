using System;
using Avalonia.Animation.Animators;
using Avalonia.Media;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition for <see cref="FontVariationSettings"/> values, e.g. animating
    /// <c>TextElement.FontVariations</c> between weights. Axis values interpolate
    /// per axis when both endpoints set the same axes and switch discretely otherwise
    /// (CSS <c>font-variation-settings</c> semantics).
    /// </summary>
    public class FontVariationSettingsTransition : Transition<FontVariationSettings?>
    {
        private static readonly FontVariationSettingsAnimator s_animator = new();

        internal override IObservable<FontVariationSettings?> DoTransition(
            IObservable<double> progress,
            FontVariationSettings? oldValue,
            FontVariationSettings? newValue)
        {
            return new AnimatorTransitionObservable<FontVariationSettings?, FontVariationSettingsAnimator>(
                s_animator, progress, Easing, oldValue, newValue);
        }
    }
}
