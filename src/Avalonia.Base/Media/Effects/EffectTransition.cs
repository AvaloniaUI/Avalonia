using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Animation.Animators;
using Avalonia.Animation.Easings;
using Avalonia.Media;


// ReSharper disable once CheckNamespace
namespace Avalonia.Animation;

/// <summary>
/// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="IEffect"/> type.
/// </summary>
public class EffectTransition : Transition<IEffect?>
{
    private static readonly BlurEffectAnimator s_blurEffectAnimator = new();
    private static readonly DropShadowEffectAnimator s_dropShadowEffectAnimator = new();
    private static readonly ImmutableBlurEffect s_DefaultBlur = new ImmutableBlurEffect(0);
    private static readonly ImmutableDropShadowDirectionEffect s_DefaultDropShadow = new(0, 0, 0, default, 0);

    bool TryWithAnimator<TAnimator, TInterface>(
        IObservable<double> progress,
        TAnimator animator,
        IEffect? oldValue, IEffect? newValue, TInterface defaultValue, [MaybeNullWhen(false)] out IObservable<IEffect?> observable)
        where TAnimator : EffectAnimatorBase<TInterface> where TInterface : class, IEffect
    {
        observable = null;
        TInterface? oldI = null, newI = null;
        if (oldValue is TInterface oi)
        {
            oldI = oi;
            if (newValue is TInterface ni)
                newI = ni;
            else if (newValue == null)
                newI = defaultValue;
            else
                return false;
        }
        else if (newValue is TInterface nv)
        {
            oldI = defaultValue;
            newI = nv;

        }
        else
            return false;

        observable = new AnimatorTransitionObservable<IEffect?, Animator<IEffect?>>(animator, progress, Easing, oldI, newI);
        return true;

    }

    internal override IObservable<IEffect?> DoTransition(IObservable<double> progress, IEffect? oldValue, IEffect? newValue)
    {
        if ((oldValue != null || newValue != null)
            && (
                TryWithAnimator<BlurEffectAnimator, IBlurEffect>(progress, s_blurEffectAnimator,
                    oldValue, newValue, s_DefaultBlur, out var observable)
                || TryWithAnimator<DropShadowEffectAnimator, IDropShadowEffect>(progress, s_dropShadowEffectAnimator,
                    oldValue, newValue, s_DefaultDropShadow, out observable)
            ))
            return observable;
        
        return new IncompatibleTransitionObservable(progress, Easing, oldValue, newValue);
    }

    private sealed class IncompatibleTransitionObservable : TransitionObservableBase<IEffect?>
    {
        private readonly IEffect? _from;
        private readonly IEffect? _to;

        public IncompatibleTransitionObservable(IObservable<double> progress, Easing easing, IEffect? from, IEffect? to) : base(progress, easing)
        {
            _from = from;
            _to = to;
        }

        protected override IEffect? ProduceValue(double progress)
        {
            return progress >= 0.5 ? _to : _from;
        }
    }
}