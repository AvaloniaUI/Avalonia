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

        private double delayFC;
        private double durationFC;
        private long repeatCount;
        private double currentIteration;
        private long firstFrameCount;

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

        private long? internalClock;

        private long? previousClock = null;
        private long currentDiscreteTime;

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
            delayFC = ((animation.Delay.Ticks / Timing.FrameTick.Ticks) * speedRatio);
            durationFC = ((animation.Duration.Ticks / Timing.FrameTick.Ticks) * speedRatio);
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

        public void Step(long frameTick, Func<double, T, T> Interpolator)
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

        private void InternalStep(long time, Func<double, T, T> Interpolator)
        {
            if (Timing.GlobalPlayState == PlayState.Stop || targetControl.PlayState == PlayState.Stop)
                DoComplete();

            if (!previousClock.HasValue)
            {
                previousClock = time;
                internalClock = 0;
            }
            else
            {
                if (Timing.GlobalPlayState == PlayState.Pause || targetControl.PlayState == PlayState.Pause)
                {
                    previousClock = time;
                    return;
                }
                var delta = time - previousClock;
                internalClock += delta;
                previousClock = time;
            }

            // currentDiscreteTime = internalClock.Value;
            currentDiscreteTime++;

            if (!gotFirstKFValue)
            {
                firstKFValue = (T)parent.First().Value;
                gotFirstKFValue = true;
            }

            if (!gotFirstFrameCount)
            {
                firstFrameCount = currentDiscreteTime;
                gotFirstFrameCount = true;
            }

            if (isDisposed)
                throw new InvalidProgramException("This KeyFrames Animation is already disposed.");

            // get the time with the initial fc as point of origin.
            double t = (currentDiscreteTime - firstFrameCount);

            // check if t is within the zeroth iteration

            double delayEndpoint = delayFC;
            double iterationEndpoint = delayEndpoint + durationFC;

            currentIteration = Math.Floor(t / iterationEndpoint);
            t = t % iterationEndpoint;

            // check if it's over the repeat count
            if (currentIteration > (repeatCount - 1) && !isLooping)
            {
                DoComplete();
            }

            // check if the current iteration should be reversed or not.
            bool isCurIterReverse = animationDirection == PlaybackDirection.Normal ? false :
                                    animationDirection == PlaybackDirection.Alternate ? (currentIteration % 2 == 0) ? false : true :
                                    animationDirection == PlaybackDirection.AlternateReverse ? (currentIteration % 2 == 0) ? true : false :
                                    animationDirection == PlaybackDirection.Reverse ? true : false;


            if (delayFC > 0 & t <= delayEndpoint)
            {
                if (currentIteration == 0)
                    DoDelay();
            }
            else if (t > delayEndpoint & t < iterationEndpoint)
            {
                double k = t - delayFC;
                var interpVal = k / (double)durationFC;
 
                if (isCurIterReverse)
                    interpVal = 1 - interpVal;

                var easedTime = EaseFunc.Ease(interpVal);

                lastInterpValue = Interpolator(easedTime, neutralValue);
                targetObserver.OnNext(lastInterpValue);
            }
            else if (t > iterationEndpoint && !isLooping)
            {
                DoComplete();
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