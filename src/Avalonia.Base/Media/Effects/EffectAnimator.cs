using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data;
using Avalonia.Logging;
using Avalonia.Media;

// ReSharper disable once CheckNamespace
namespace Avalonia.Animation.Animators;

internal class EffectAnimator : Animator<IEffect?>
{
    public override IDisposable? Apply(Animation animation, Animatable control, IClock? clock,
        IObservable<bool> match, Action? onComplete)
    {
        if (TryCreateAnimator<BlurEffectAnimator, IBlurEffect>(out var animator)
            || TryCreateAnimator<DropShadowEffectAnimator, IDropShadowEffect>(out animator))
            return animator.Apply(animation, control, clock, match, onComplete);

        Logger.TryGet(LogEventLevel.Error, LogArea.Animations)?.Log(
            this,
            "The animation's keyframe value types set is not supported.");

        return base.Apply(animation, control, clock, match, onComplete);
    }

    private bool TryCreateAnimator<TAnimator, TInterface>([NotNullWhen(true)] out IAnimator? animator)
        where TAnimator : EffectAnimatorBase<TInterface>, new() where TInterface : class, IEffect
    {
        TAnimator? createdAnimator = null;
        foreach (var keyFrame in this)
        {
            if (keyFrame.Value is TInterface)
            {
                createdAnimator ??= new TAnimator()
                {
                    Property = Property
                };
                createdAnimator.Add(new AnimatorKeyFrame(typeof(TAnimator), () => new TAnimator(), keyFrame.Cue,
                    keyFrame.KeySpline)
                {
                    Value = keyFrame.Value,
                    FillBefore = keyFrame.FillBefore,
                    FillAfter = keyFrame.FillAfter
                });
            }
            else
            {
                animator = null;
                return false;
            }
        }

        animator = createdAnimator;
        return animator != null;
    }

    /// <summary>
    /// Fallback implementation of <see cref="IEffect"/> animation.
    /// </summary>
    public override IEffect? Interpolate(double progress, IEffect? oldValue, IEffect? newValue) => progress >= 0.5 ? newValue : oldValue;

    private static bool s_Registered;
    public static void EnsureRegistered()
    {
        if(s_Registered)
            return;
        s_Registered = true;
    }
}

internal abstract class EffectAnimatorBase<T> : Animator<IEffect?> where T : class, IEffect?
{
    public override IDisposable BindAnimation(Animatable control, IObservable<IEffect?> instance)
    {
        if (Property is null)
        {
            throw new InvalidOperationException("Animator has no property specified.");
        }

        return control.Bind((AvaloniaProperty<IEffect?>)Property, instance, BindingPriority.Animation);
    }

    protected abstract T Interpolate(double progress, T oldValue, T newValue);
    public override IEffect? Interpolate(double progress, IEffect? oldValue, IEffect? newValue)
    {
        var old = oldValue as T;
        var n = newValue as T;
        if (old == null || n == null)
            return progress >= 0.5 ? newValue : oldValue;
        return Interpolate(progress, old, n);
    }
}

internal class BlurEffectAnimator : EffectAnimatorBase<IBlurEffect>
{
    private static readonly DoubleAnimator s_doubleAnimator = new DoubleAnimator();

    protected override IBlurEffect Interpolate(double progress, IBlurEffect oldValue, IBlurEffect newValue)
    {
        return new ImmutableBlurEffect(
            s_doubleAnimator.Interpolate(progress, oldValue.Radius, newValue.Radius));
    }
}

internal class DropShadowEffectAnimator : EffectAnimatorBase<IDropShadowEffect>
{
    private static readonly DoubleAnimator s_doubleAnimator = new DoubleAnimator();

    protected override IDropShadowEffect Interpolate(double progress, IDropShadowEffect oldValue,
        IDropShadowEffect newValue)
    {
        var blur = s_doubleAnimator.Interpolate(progress, oldValue.BlurRadius, newValue.BlurRadius);
        var color = ColorAnimator.InterpolateCore(progress, oldValue.Color, newValue.Color);
        var opacity = s_doubleAnimator.Interpolate(progress, oldValue.Opacity, newValue.Opacity);

        if (oldValue is IDirectionDropShadowEffect oldDirection && newValue is IDirectionDropShadowEffect newDirection)
        {
            return new ImmutableDropShadowDirectionEffect(
                s_doubleAnimator.Interpolate(progress, oldDirection.Direction, newDirection.Direction),
                s_doubleAnimator.Interpolate(progress, oldDirection.ShadowDepth, newDirection.ShadowDepth),
                blur, color, opacity
            );
        }

        return new ImmutableDropShadowEffect(
            s_doubleAnimator.Interpolate(progress, oldValue.OffsetX, newValue.OffsetX),
            s_doubleAnimator.Interpolate(progress, oldValue.OffsetY, newValue.OffsetY),
            blur, color, opacity
        );
    }
}
