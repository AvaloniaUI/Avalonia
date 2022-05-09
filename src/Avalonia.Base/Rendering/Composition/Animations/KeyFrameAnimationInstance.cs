using System;
using Avalonia.Rendering.Composition.Expressions;

namespace Avalonia.Rendering.Composition.Animations
{
    class KeyFrameAnimationInstance<T> : IAnimationInstance where T : struct
    {
        private readonly IInterpolator<T> _interpolator;
        private readonly ServerKeyFrame<T>[] _keyFrames;
        private readonly PropertySetSnapshot _snapshot;
        private readonly ExpressionVariant? _finalValue;
        private readonly IExpressionObject _target;
        private readonly AnimationDelayBehavior _delayBehavior;
        private readonly TimeSpan _delayTime;
        private readonly AnimationDirection _direction;
        private readonly TimeSpan _duration;
        private readonly AnimationIterationBehavior _iterationBehavior;
        private readonly int _iterationCount;
        private readonly AnimationStopBehavior _stopBehavior;
        private TimeSpan _startedAt;
        private T _startingValue;

        public KeyFrameAnimationInstance(
            IInterpolator<T> interpolator, ServerKeyFrame<T>[] keyFrames,
            PropertySetSnapshot snapshot, ExpressionVariant? finalValue,
            IExpressionObject target,
            AnimationDelayBehavior delayBehavior, TimeSpan delayTime,
            AnimationDirection direction, TimeSpan duration,
            AnimationIterationBehavior iterationBehavior,
            int iterationCount, AnimationStopBehavior stopBehavior)
        {
            _interpolator = interpolator;
            _keyFrames = keyFrames;
            _snapshot = snapshot;
            _finalValue = finalValue;
            _target = target;
            _delayBehavior = delayBehavior;
            _delayTime = delayTime;
            _direction = direction;
            _duration = duration;
            _iterationBehavior = iterationBehavior;
            _iterationCount = iterationCount;
            _stopBehavior = stopBehavior;
            if (_keyFrames.Length == 0)
                throw new InvalidOperationException("Animation has no key frames");
            if(_duration.Ticks <= 0)
                throw new InvalidOperationException("Invalid animation duration");
        }

        public ExpressionVariant Evaluate(TimeSpan now, ExpressionVariant currentValue)
        {
            var elapsed = now - _startedAt;
            var starting = ExpressionVariant.Create(_startingValue);
            var ctx = new ExpressionEvaluationContext
            {
                Parameters = _snapshot,
                Target = _target,
                CurrentValue = currentValue,
                FinalValue = _finalValue ??  starting,
                StartingValue = starting,
                ForeignFunctionInterface = BuiltInExpressionFfi.Instance
            };
            
            if (elapsed < _delayTime)
            {
                if (_delayBehavior == AnimationDelayBehavior.SetInitialValueBeforeDelay)
                    return ExpressionVariant.Create(GetKeyFrame(ref ctx, _keyFrames[0]));
                return currentValue;
            }

            elapsed -= _delayTime;
            var iterationNumber = elapsed.Ticks / _duration.Ticks;
            if (_iterationBehavior == AnimationIterationBehavior.Count
                && iterationNumber >= _iterationCount)
                return ExpressionVariant.Create(GetKeyFrame(ref ctx, _keyFrames[_keyFrames.Length - 1]));
            
            
            var evenIterationNumber = iterationNumber % 2 == 0;
            elapsed = TimeSpan.FromTicks(elapsed.Ticks % _duration.Ticks);

            var reverse =
                _direction == AnimationDirection.Alternate
                    ? !evenIterationNumber
                    : _direction == AnimationDirection.AlternateReverse
                        ? evenIterationNumber
                        : _direction == AnimationDirection.Reverse;

            var iterationProgress = elapsed.TotalSeconds / _duration.TotalSeconds;
            if (reverse)
                iterationProgress = 1 - iterationProgress;

            var left = new ServerKeyFrame<T>
            {
                Value = _startingValue
            };
            var right = _keyFrames[_keyFrames.Length - 1];
            for (var c = 0; c < _keyFrames.Length; c++)
            {
                var kf = _keyFrames[c];
                if (kf.Key < iterationProgress)
                {
                    // this is the last frame
                    if (c == _keyFrames.Length - 1)
                        return ExpressionVariant.Create(GetKeyFrame(ref ctx, kf));

                    left = kf;
                    right = _keyFrames[c + 1];
                    break;
                }
            }

            var keyProgress = Math.Max(0, Math.Min(1, (iterationProgress - left.Key) / (right.Key - left.Key)));

            var easedKeyProgress = right.EasingFunction.Ease((float) keyProgress);
            if (float.IsNaN(easedKeyProgress) || float.IsInfinity(easedKeyProgress))
                return currentValue;
            
            return ExpressionVariant.Create(_interpolator.Interpolate(
                GetKeyFrame(ref ctx, left),
                GetKeyFrame(ref ctx, right),
                easedKeyProgress
            ));
        }

        T GetKeyFrame(ref ExpressionEvaluationContext ctx, ServerKeyFrame<T> f)
        {
            if (f.Expression != null)
                return f.Expression.Evaluate(ref ctx).CastOrDefault<T>();
            else
                return f.Value;
        }

        public void Start(TimeSpan startedAt, ExpressionVariant startingValue)
        {
            _startedAt = startedAt;
            _startingValue = startingValue.CastOrDefault<T>();
        }
    }
}