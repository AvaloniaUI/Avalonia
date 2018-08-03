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
            delayBetweenIterations = animation.DelayBetweenRepeats;

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
            onComplete = onComplete;
        }

        public void Step(PlayState playState, long frameTick, Func<double, T, T> Interpolator)
        {
            try
            {
                InternalStep(playState, frameTick, Interpolator);
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

        private void InternalStep(PlayState playState, long frameTick, Func<double, T, T> Interpolator)
        {
            if (!gotFirstKFValue)
            {
                firstKFValue = (T)parent.First().Value;
                gotFirstKFValue = true;
            }

            if (!gotFirstFrameCount)
            {
                firstFrameCount = frameTick;
                gotFirstFrameCount = true;
            }

            if (isDisposed)
                throw new InvalidProgramException("This KeyFrames Animation is already disposed.");

            if (playState == PlayState.Stop)
                DoComplete();

            // Save state and pause the machine
            // if (playState == PlayState.Pause && currentState != KeyFramesStates.Pause)
            // {
            //     savedState = currentState;
            //     currentState = KeyFramesStates.Pause;
            // }

            // // Resume the previous state
            // if (playState != PlayState.Pause && currentState == KeyFramesStates.Pause)
            //     currentState = savedState;

            // get the time with the initial fc as point of origin.
            double t = (frameTick - firstFrameCount);

            // check if t is within the zeroth iteration
            if (t <= (delayFC + durationFC))
            {
                currentIteration = 0;
                t = t % (delayFC + durationFC);
            }
            else
            {
                var totalDur = (double)((delayBetweenIterations ? delayFC : 0) + durationFC + 1);
                currentIteration = Math.Floor(t / totalDur);
                t = t % totalDur;
            }

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

            double x1 = delayFC;
            double x2 = x1 + durationFC;

            if (delayFC > 0 & t >= 0 & t <= x1)
            {
                if (currentIteration == 0 && delayBetweenIterations)
                    DoDelay();

            }
            else if (t >= x1 & t <= x2)
            {
                var interpVal = t / durationFC;

                if (isCurIterReverse)
                    interpVal = 1 - interpVal;

                var easedTime = EaseFunc.Ease(interpVal);

                lastInterpValue = Interpolator(easedTime, neutralValue);
                targetObserver.OnNext(lastInterpValue);
            }
            else if (t > x2 & (currentIteration + 1 > repeatCount & !isLooping))
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