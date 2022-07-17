using System;
using System.Numerics;

namespace Avalonia.Rendering.Composition.Animations
{
    /// <summary>
    ///  An interface to define interpolation logic for a particular type
    /// </summary>
    internal interface IInterpolator<T>
    {
        T Interpolate(T from, T to, float progress);
    }

    class ScalarInterpolator : IInterpolator<float>
    {
        public float Interpolate(float @from, float to, float progress) => @from + (to - @from) * progress;
        
        public static ScalarInterpolator Instance { get; } = new ScalarInterpolator();
    }

    class Vector2Interpolator : IInterpolator<Vector2>
    {
        public Vector2 Interpolate(Vector2 @from, Vector2 to, float progress) 
            => Vector2.Lerp(@from, to, progress);
        
        public static Vector2Interpolator Instance { get; } = new Vector2Interpolator();
    }
    
    class Vector3Interpolator : IInterpolator<Vector3>
    {
        public Vector3 Interpolate(Vector3 @from, Vector3 to, float progress) 
            => Vector3.Lerp(@from, to, progress);
        
        public static Vector3Interpolator Instance { get; } = new Vector3Interpolator();
    }
    
    class Vector4Interpolator : IInterpolator<Vector4>
    {
        public Vector4 Interpolate(Vector4 @from, Vector4 to, float progress) 
            => Vector4.Lerp(@from, to, progress);
        
        public static Vector4Interpolator Instance { get; } = new Vector4Interpolator();
    }
    
    class QuaternionInterpolator : IInterpolator<Quaternion>
    {
        public Quaternion Interpolate(Quaternion @from, Quaternion to, float progress) 
            => Quaternion.Lerp(@from, to, progress);

        public static QuaternionInterpolator Instance { get; } = new QuaternionInterpolator();
    }
    
    class ColorInterpolator : IInterpolator<Avalonia.Media.Color>
    {
        static byte Lerp(float a, float b, float p) => (byte) Math.Max(0, Math.Min(255, (p * (b - a) + a)));

        public static Avalonia.Media.Color
            LerpRGB(Avalonia.Media.Color to, Avalonia.Media.Color from, float progress) =>
            new Avalonia.Media.Color(Lerp(to.A, @from.A, progress),
                Lerp(to.R, @from.R, progress),
                Lerp(to.G, @from.G, progress),
                Lerp(to.B, @from.B, progress));

        public Avalonia.Media.Color Interpolate(Avalonia.Media.Color @from, Avalonia.Media.Color to, float progress)
            => LerpRGB(@from, to, progress);
        
        public static ColorInterpolator Instance { get; } = new ColorInterpolator();
    }

    class BooleanInterpolator : IInterpolator<bool>
    {
        public bool Interpolate(bool @from, bool to, float progress) => progress >= 1 ? to : @from;
        
        public static BooleanInterpolator Instance { get; } = new BooleanInterpolator();
    }
}