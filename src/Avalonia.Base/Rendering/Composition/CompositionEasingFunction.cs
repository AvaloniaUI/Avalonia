using System;
using System.Numerics;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Rendering.Composition.Utils;

namespace Avalonia.Rendering.Composition
{
    public abstract class CompositionEasingFunction : CompositionObject
    {
        internal CompositionEasingFunction(Compositor compositor) : base(compositor, null!)
        {
        }
        
        internal abstract IEasingFunction Snapshot();
    }
    
    internal interface IEasingFunction
    {
        float Ease(float progress);
    }

    public sealed class DelegateCompositionEasingFunction : CompositionEasingFunction
    {
        private readonly Easing _func;

        public delegate float EasingDelegate(float progress);

        internal DelegateCompositionEasingFunction(Compositor compositor, EasingDelegate func) : base(compositor)
        {
            _func = new Easing(func);
        }

        class Easing : IEasingFunction
        {
            private readonly EasingDelegate _func;

            public Easing(EasingDelegate func)
            {
                _func = func;
            }
            
            public float Ease(float progress) => _func(progress);
        }

        internal override IEasingFunction Snapshot() => _func;
    }

    public class LinearEasingFunction : CompositionEasingFunction
    {
        public LinearEasingFunction(Compositor compositor) : base(compositor)
        {
        }

        class Linear : IEasingFunction
        {
            public float Ease(float progress) => progress;
        }

        private static readonly Linear Instance = new Linear();
        internal override IEasingFunction Snapshot() => Instance;
    }

    public class CubicBezierEasingFunction : CompositionEasingFunction
    {
        private CubicBezier _bezier;
        public Vector2 ControlPoint1 { get; }
        public Vector2 ControlPoint2 { get; }
        //cubic-bezier(0.25, 0.1, 0.25, 1.0)
        internal CubicBezierEasingFunction(Compositor compositor, Vector2 controlPoint1, Vector2 controlPoint2) : base(compositor)
        {
            ControlPoint1 = controlPoint1;
            ControlPoint2 = controlPoint2;
            if (controlPoint1.X < 0 || controlPoint1.X > 1 || controlPoint2.X < 0 || controlPoint2.X > 1)
                throw new ArgumentException();
            _bezier = new CubicBezier(controlPoint1.X, controlPoint1.Y, controlPoint2.X, controlPoint2.Y);
        }

        class EasingFunction : IEasingFunction
        {
            private readonly CubicBezier _bezier;

            public EasingFunction(CubicBezier bezier)
            {
                _bezier = bezier;
            }
            
            public float Ease(float progress) => (float)_bezier.Solve(progress);
        }

        internal static IEasingFunction Ease { get; } = new EasingFunction(new CubicBezier(0.25, 0.1, 0.25, 1));

        internal override IEasingFunction Snapshot() => new EasingFunction(_bezier);
    }

}