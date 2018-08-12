using System;
using System.Linq;
using Avalonia.Animation.Utils;
using Avalonia.Data;

namespace Avalonia.Animation
{
    /// <summary>
    /// Provides statefulness for an iteration of a keyframe animation.
    /// </summary>
    internal class AnimatorStateMachine<T> : IObservable<T>, IDisposable
    {
        T lastInterpValue;
        T firstKFValue;

        private long repeatCount;
        private double currentIteration;

        private bool isLooping;
        private bool isRepeating;
        private bool gotFirstKFValue;
        private bool gotFirstFrameCount;
        private bool delayBetweenIterations;

        private FillMode fillMode;
        private PlaybackDirection animationDirection;
        private Animator<T> parent;
        private Animatable targetControl;
        private T neutralValue;
        private double speedRatio;
        internal bool unsubscribe;
        private bool isDisposed;

        private TimeSpan delayFC;
        private TimeSpan durationFC;
        private TimeSpan firstFrameCount;
        private TimeSpan internalClock;
        private TimeSpan? previousClock;

        private Easings.Easing EaseFunc;
        private IObserver<T> targetObserver;
        private readonly Action onComplete;

        public AnimatorStateMachine(Animation animation, Animatable control, Animator<T> animator, Action onComplete)
        {

            if (animation.SpeedRatio <= 0 || DoubleUtils.AboutEqual(animation.SpeedRatio, 0))
                throw new InvalidOperationException("Speed ratio cannot be negative or zero.");

            if (animation.Duration.TotalSeconds <= 0 || DoubleUtils.AboutEqual(animation.Duration.TotalSeconds, 0))
                throw new InvalidOperationException("Animation duration cannot be negative or zero.");

            parent = animator;
            EaseFunc = animation.Easing;
            targetControl = control;
            neutralValue = (T)targetControl.GetValue(parent.Property);

            speedRatio = animation.SpeedRatio;

            delayFC = animation.Delay;
            durationFC = animation.Duration;

            delayBetweenIterations = animation.DelayBetweenIterations;

            switch (animation.RepeatCount.RepeatType)
            {
                case RepeatType.None:
                    repeatCount = 1;
                    break;
                case RepeatType.Loop:
                    isLooping = true;
                    break;
                case RepeatType.Repeat:
                    isRepeating = true;
                    repeatCount = (long)animation.RepeatCount.Value;
                    break;
            }

            animationDirection = animation.PlaybackDirection;
            fillMode = animation.FillMode;
            this.onComplete = onComplete;
        }

        public void Step(TimeSpan frameTick, Func<double, T, T> Interpolator)
        {
            try
            {
                InternalStep(frameTick, Interpolator);
            }
            catch (Exception e)
            {
                targetObserver?.OnError(e);
            }
        }

        private void DoComplete()
        {
            if (fillMode == FillMode.Forward || fillMode == FillMode.Both)
                targetControl.SetValue(parent.Property, lastInterpValue, BindingPriority.LocalValue);

            targetObserver.OnCompleted();
            onComplete?.Invoke();
            Dispose();
        }

        private void DoDelay()
        {
            if (fillMode == FillMode.Backward || fillMode == FillMode.Both)
                if (currentIteration == 0)
                    targetObserver.OnNext(firstKFValue);
                else
                    targetObserver.OnNext(lastInterpValue);
        }

        private void DoPlayStatesAndTime(TimeSpan systemTime)
        {
            if (Timing.GlobalPlayState == PlayState.Stop || targetControl.PlayState == PlayState.Stop)
                DoComplete();

            if (!previousClock.HasValue)
            {
                previousClock = systemTime;
                internalClock = TimeSpan.Zero;
            }
            else
            {
                if (Timing.GlobalPlayState == PlayState.Pause || targetControl.PlayState == PlayState.Pause)
                {
                    previousClock = systemTime;
                    return;
                }
                var delta = systemTime - previousClock;
                internalClock += delta.Value;
                previousClock = systemTime;
            }

            if (!gotFirstKFValue)
            {
                firstKFValue = (T)parent.First().Value;
                gotFirstKFValue = true;
            }

            if (!gotFirstFrameCount)
            {
                firstFrameCount = internalClock;
                gotFirstFrameCount = true;
            }
        }

        private void InternalStep(TimeSpan systemTime, Func<double, T, T> Interpolator)
        {
            DoPlayStatesAndTime(systemTime);

            if (isDisposed)
                throw new InvalidProgramException("This KeyFrames Animation is already disposed.");

            var t = internalClock - firstFrameCount;

            var delayEndpoint = delayFC;
            var iterationEndpoint = delayEndpoint + durationFC;

            currentIteration = (int)Math.Floor((double)t.Ticks / iterationEndpoint.Ticks);
            t = TimeSpan.FromTicks(t.Ticks % iterationEndpoint.Ticks);

            if (currentIteration > (repeatCount - 1) && !isLooping)
                DoComplete();

            if (t > iterationEndpoint & !isLooping)
                DoComplete();
            
            bool isCurIterReverse = animationDirection == PlaybackDirection.Normal ? false :
                                    animationDirection == PlaybackDirection.Alternate ? (currentIteration % 2 == 0) ? false : true :
                                    animationDirection == PlaybackDirection.AlternateReverse ? (currentIteration % 2 == 0) ? true : false :
                                    animationDirection == PlaybackDirection.Reverse ? true : false;

            if (delayFC > TimeSpan.Zero & t < delayEndpoint)
            {
                if (currentIteration == 0)
                    DoDelay();
            }
            else if (t >= delayEndpoint & t <= iterationEndpoint)
            {
                var k = t - delayFC;
                var interpVal = (double)k.Ticks / durationFC.Ticks;

                if (isCurIterReverse)
                    interpVal = 1 - interpVal;

                var easedTime = EaseFunc.Ease(interpVal);

                lastInterpValue = Interpolator(easedTime, neutralValue);
                targetObserver.OnNext(lastInterpValue);
            }
            
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            targetObserver = observer;
            return this;
        }
        
        public void Dispose()
        {
            unsubscribe = true;
            isDisposed = true;
        }
    }
}