using System;
using System.Linq;
using Avalonia.Animation.Utils;
using Avalonia.Data;

namespace Avalonia.Animation
{
    /// <summary>
    /// Handles interpolatoin and time-related functions 
    /// for keyframe animations.
    /// </summary>
    internal class AnimationsEngine<T> : IObservable<T>, IDisposable
    {
        T lastInterpValue;
        T firstKFValue;

        private long repeatCount;
        private double currentIteration;

        private bool isLooping;
        private bool gotFirstKFValue;
        private bool gotFirstFrameCount;
        private bool iterationDelay;

        private FillMode fillMode;
        private PlaybackDirection animationDirection;
        private Animator<T> parent;
        private Animatable targetControl;
        private T neutralValue;
        private double speedRatio;
        internal bool unsubscribe;
        private bool isDisposed;

        private TimeSpan delay;
        private TimeSpan duration;
        private TimeSpan firstFrameCount;
        private TimeSpan internalClock;
        private TimeSpan? previousClock;

        private Easings.Easing easeFunc;
        private IObserver<T> targetObserver;
        private readonly Action onCompleteAction;

        public AnimationsEngine(Animation animation, Animatable control, Animator<T> animator, Action OnComplete)
        {
            if (animation.SpeedRatio <= 0 || DoubleUtils.AboutEqual(animation.SpeedRatio, 0))
                throw new InvalidOperationException("Speed ratio cannot be negative or zero.");

            if (animation.Duration.TotalSeconds <= 0 || DoubleUtils.AboutEqual(animation.Duration.TotalSeconds, 0))
                throw new InvalidOperationException("Duration cannot be negative or zero.");
 
            parent = animator;
            easeFunc = animation.Easing;
            targetControl = control;
            neutralValue = (T)targetControl.GetValue(parent.Property);

            speedRatio = animation.SpeedRatio;

            delay = animation.Delay;
            duration = animation.Duration;
            iterationDelay = animation.DelayBetweenIterations;

            switch (animation.RepeatCount.RepeatType)
            {
                case RepeatType.None:
                    repeatCount = 1;
                    break;
                case RepeatType.Loop:
                    isLooping = true;
                    break;
                case RepeatType.Repeat:
                    repeatCount = (long)animation.RepeatCount.Value;
                    break;
            }

            animationDirection = animation.PlaybackDirection;
            fillMode = animation.FillMode;
            onCompleteAction = OnComplete;
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
            onCompleteAction?.Invoke();
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
            if (Animation.GlobalPlayState == PlayState.Stop || targetControl.PlayState == PlayState.Stop)
                DoComplete();

            if (!previousClock.HasValue)
            {
                previousClock = systemTime;
                internalClock = TimeSpan.Zero;
            }
            else
            {
                if (Animation.GlobalPlayState == PlayState.Pause || targetControl.PlayState == PlayState.Pause)
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

            var time = internalClock - firstFrameCount;
            var delayEndpoint = delay;
            var iterationEndpoint = delayEndpoint + duration;

            //determine if time is currently in the first iteration.
            if (time >= TimeSpan.Zero & time <= iterationEndpoint)
            {
                currentIteration = 1;
            }
            else if (time > iterationEndpoint)
            {
                //Subtract first iteration to properly get the subsequent iteration time
                time -= iterationEndpoint;

                if (!iterationDelay & delayEndpoint > TimeSpan.Zero)
                {
                    delayEndpoint = TimeSpan.Zero;
                    iterationEndpoint = duration;
                }

                //Calculate the current iteration number
                currentIteration = (int)Math.Floor((double)time.Ticks / iterationEndpoint.Ticks) + 2;
            }
            else
            {
                previousClock = systemTime;
                return;
            }

            time = TimeSpan.FromTicks(time.Ticks % iterationEndpoint.Ticks);

            if (!isLooping)
            {
                if (currentIteration > repeatCount)
                    DoComplete();

                if (time > iterationEndpoint)
                    DoComplete();
            }

            // Determine if the current iteration should have its normalized time inverted.
            bool isCurIterReverse = animationDirection == PlaybackDirection.Normal ? false :
                                    animationDirection == PlaybackDirection.Alternate ? (currentIteration % 2 == 0) ? false : true :
                                    animationDirection == PlaybackDirection.AlternateReverse ? (currentIteration % 2 == 0) ? true : false :
                                    animationDirection == PlaybackDirection.Reverse ? true : false;

            if (delayEndpoint > TimeSpan.Zero & time < delayEndpoint)
            {
                DoDelay();
            }
            else
            {
                // Offset the delay time            
                time -= delayEndpoint;
                iterationEndpoint -= delayEndpoint;

                // Normalize time
                var interpVal = (double)time.Ticks / iterationEndpoint.Ticks;

                if (isCurIterReverse)
                    interpVal = 1 - interpVal;

                // Ease and interpolate
                var easedTime = easeFunc.Ease(interpVal);
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