using System;
using System.Collections.Generic;
using Avalonia.Animation.Animators;
using Avalonia.Media;

namespace Avalonia.Animation;

partial class Animation
{
    /// <summary>
    /// Sets the value of the Animator attached property for a setter.
    /// </summary>
    /// <param name="setter">The animation setter.</param>
    /// <param name="value">The property animator value.</param>
    [Obsolete("CustomAnimatorBase will be removed before 11.0, use InterpolatingAnimator<T>", true)]
    public static void SetAnimator(IAnimationSetter setter, CustomAnimatorBase value)
    {
        s_animators[setter] = (value.WrapperType, value.CreateWrapper);
    }

    /// <summary>
    /// Sets the value of the Animator attached property for a setter.
    /// </summary>
    /// <param name="setter">The animation setter.</param>
    /// <param name="value">The property animator value.</param>
    public static void SetAnimator(IAnimationSetter setter, ICustomAnimator value)
    {
        s_animators[setter] = (value.WrapperType, value.CreateWrapper);
    }

    private readonly static List<(Func<AvaloniaProperty, bool> Condition, Type Animator, Func<IAnimator> Factory)>
        Animators = new()
        {
            (prop =>(typeof(double).IsAssignableFrom(prop.PropertyType) && typeof(Transform).IsAssignableFrom(prop.OwnerType)),
                typeof(TransformAnimator), () => new TransformAnimator()),
            (prop => typeof(bool).IsAssignableFrom(prop.PropertyType), typeof(BoolAnimator), () => new BoolAnimator()),
            (prop => typeof(byte).IsAssignableFrom(prop.PropertyType), typeof(ByteAnimator), () => new ByteAnimator()),
            (prop => typeof(Int16).IsAssignableFrom(prop.PropertyType), typeof(Int16Animator), () => new Int16Animator()),
            (prop => typeof(Int32).IsAssignableFrom(prop.PropertyType), typeof(Int32Animator), () => new Int32Animator()),
            (prop => typeof(Int64).IsAssignableFrom(prop.PropertyType), typeof(Int64Animator), () => new Int64Animator()), 
            (prop => typeof(UInt16).IsAssignableFrom(prop.PropertyType), typeof(UInt16Animator), () => new UInt16Animator()), 
            (prop => typeof(UInt32).IsAssignableFrom(prop.PropertyType), typeof(UInt32Animator), () => new UInt32Animator()), 
            (prop => typeof(UInt64).IsAssignableFrom(prop.PropertyType), typeof(UInt64Animator), () => new UInt64Animator()),
            (prop => typeof(float).IsAssignableFrom(prop.PropertyType), typeof(FloatAnimator), () => new FloatAnimator()), 
            (prop => typeof(double).IsAssignableFrom(prop.PropertyType), typeof(DoubleAnimator), () => new DoubleAnimator()), 
            (prop => typeof(decimal).IsAssignableFrom(prop.PropertyType), typeof(DecimalAnimator), () => new DecimalAnimator()),
        };

    static Animation()
    {
        RegisterAnimator<IEffect?, EffectAnimator>();
        RegisterAnimator<BoxShadow, BoxShadowAnimator>();
        RegisterAnimator<BoxShadows, BoxShadowsAnimator>();
        RegisterAnimator<IBrush?, BaseBrushAnimator>();
        RegisterAnimator<CornerRadius, CornerRadiusAnimator>();
        RegisterAnimator<Color, ColorAnimator>();
        RegisterAnimator<Vector, VectorAnimator>();
        RegisterAnimator<Point, PointAnimator>();
        RegisterAnimator<Rect, RectAnimator>();
        RegisterAnimator<RelativePoint, RelativePointAnimator>();
        RegisterAnimator<RelativeScalar, RelativeScalarAnimator>();
        RegisterAnimator<Size, SizeAnimator>();
        RegisterAnimator<Thickness, ThicknessAnimator>();
    }

    /// <summary>
    /// Registers a <see cref="Animator{T}"/> that can handle
    /// a value type that matches the specified condition.
    /// </summary>
    static void RegisterAnimator<T, TAnimator>()
        where TAnimator : Animator<T>, new()
    {
        Animators.Insert(0,
            (prop => typeof(T).IsAssignableFrom(prop.PropertyType), typeof(TAnimator), () => new TAnimator()));
    }

    public static void RegisterCustomAnimator<T, TAnimator>() where TAnimator : InterpolatingAnimator<T>, new()
    {
        Animators.Insert(0, (prop => typeof(T).IsAssignableFrom(prop.PropertyType),
            typeof(InterpolatingAnimator<T>.AnimatorWrapper), () => new TAnimator().CreateWrapper()));
    }

    private static (Type Type, Func<IAnimator> Factory)? GetAnimatorType(AvaloniaProperty property)
    {
        foreach (var (condition, type, factory) in Animators)
        {
            if (condition(property))
            {
                return (type, factory);
            }
        }

        return null;
    }
}