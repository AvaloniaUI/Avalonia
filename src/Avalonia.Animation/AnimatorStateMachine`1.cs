using System;
using System.Linq;
using Avalonia.Animation.Utils;
using Avalonia.Data;

namespace Avalonia.Animation
{
    /// <summary>
    /// Provides statefulness for an iteration of a keyframe animation.
    /// </summary>
    internal class AnimatorStateMachine<T> : IObservable<object>, IDisposable
    {
        T lastInterpValue;
        object firstKFValue;

        private long delayFC;
        private long durationFC;
        private long repeatCount;
        private long currentIteration;
        private long firstFrameCount;

        private bool isLooping;
        private bool isRepeating;
        private bool isReversed;
        private bool checkLoopAndRepeat;
        private bool gotFirstKFValue;
        private bool gotFirstFrameCount;

        private bool delayBetweenIterations;

        private FillMode fillMode;
        private PlaybackDirection animationDirection;
        // private KeyFramesStates currentState;
        // private KeyFramesStates savedState;
        private Animator<T> parent;
        private Animatable targetControl;
        private T neutralValue;
        private double speedRatio;
        internal bool unsubscribe;
        private bool isDisposed;

        private Easings.Easing EaseFunc;
        private IObserver<object> targetObserver;

        public void Initialize(Animation animation, Animatable control, Animator<T> animator)
        {

            if (animation.SpeedRatio <= 0 || DoubleUtils.AboutEqual(animation.SpeedRatio, 0))
                throw new InvalidOperationException("Speed ratio cannot be negative or zero.");

            if (animation.Duration.TotalSeconds <= 0 || DoubleUtils.AboutEqual(animation.Duration.TotalSeconds, 0))
                throw new InvalidOperationException("Animation duration cannot be negative or zero.");

            parent = animator;
            EaseFunc = animation.Easing;
            //targetAnimation = animation;
            targetControl = control;
            neutralValue = (T)targetControl.GetValue(parent.Property);

            speedRatio = animation.SpeedRatio;
            delayFC = (long)((animation.Delay.Ticks / Timing.FrameTick.Ticks) * speedRatio);
            durationFC = (long)((animation.Duration.Ticks / Timing.FrameTick.Ticks) * speedRatio);


            switch (animation.RepeatCount.RepeatType)
            {
                case RepeatType.None:
                    repeatCount = 1;
                    break;
                case RepeatType.Loop:
                    isLooping = true;
                    checkLoopAndRepeat = true;
                    break;
                case RepeatType.Repeat:
                    isRepeating = true;
                    checkLoopAndRepeat = true;
                    repeatCount = (long)animation.RepeatCount.Value;
                    break;
            }

            isReversed = (animation.PlaybackDirection & PlaybackDirection.Reverse) != 0;
            animationDirection = animation.PlaybackDirection;
            fillMode = animation.FillMode;

            // if (durationFC > 0)
            //     currentState = KeyFramesStates.DoDelay;
            // else
            //     currentState = KeyFramesStates.DoRun;
        }

        public void Step(PlayState _playState, ulong frameTick, Func<double, T, T> Interpolator)
        {
            try
            {
                InternalStep(_playState, (long)frameTick, Interpolator);
            }
            catch (Exception e)
            {
                targetObserver?.OnError(e);
            }
        }


        private void DoComplete()
        {
            if (fillMode == FillMode.Forward
             || fillMode == FillMode.Both)
            {
                targetControl.SetValue(parent.Property, lastInterpValue, BindingPriority.LocalValue);
            }
            targetObserver.OnCompleted();
        }
        private void DoDelay()
        {
            if (fillMode == FillMode.Backward
             || fillMode == FillMode.Both)
            {
                if (currentIteration == 0)
                {
                    targetObserver.OnNext(firstKFValue);
                }
                else
                {
                    targetObserver.OnNext(lastInterpValue);
                }
            }
        }


        private void InternalStep(PlayState playState, long t, Func<double, T, T> Interpolator)
        {
            if (!gotFirstKFValue)
            {
                firstKFValue = parent.First().Value;
                gotFirstKFValue = true;
            }

            if (!gotFirstFrameCount)
            {
                firstFrameCount = t;
                gotFirstFrameCount = true;
            }

            if (isDisposed)
                throw new InvalidProgramException("This KeyFrames Animation is already disposed.");

            // if (playState == PlayState.Stop)
            //     currentState = KeyFramesStates.Stop;

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
            var curTime = (t - firstFrameCount);

            if (curTime <= (delayFC + durationFC))
            {
                currentIteration = 0;
                curTime = curTime % (delayFC + durationFC);
            }
            else
            {
                var totalDur = (double)((delayBetweenIterations ? delayFC : 0) + durationFC + 1);
                currentIteration = (long)Math.Floor((double)curTime / totalDur);
                curTime = curTime % (long)totalDur;
            }

            // check if it's over the repeat count
            if (currentIteration + 1 > (long)repeatCount && !isLooping)
            {
                DoComplete();
            }

            // check if the current iteration should be reversed or not.
            var isCurIterReverse = animationDirection == PlaybackDirection.Normal ? false :
                                   animationDirection == PlaybackDirection.Alternate ? (currentIteration % 2 == 0) ? false : true :
                                   animationDirection == PlaybackDirection.AlternateReverse ? (currentIteration % 2 == 0) ? true : false :
                                   animationDirection == PlaybackDirection.Reverse ? true : false;


            var x1 = delayFC;
            var x2 = x1 + durationFC;

            if (delayFC > 0 && curTime >= 0 && curTime <= x1)
            {
                if (currentIteration == 0)
                    DoDelay();
            }
            else if (curTime >= x1 && curTime <= x2)
            {
                var interpVal = curTime / (double)durationFC;

                if (isCurIterReverse)
                    interpVal = 1 - interpVal;

                var easedTime = EaseFunc.Ease(interpVal);

                lastInterpValue = Interpolator(easedTime, neutralValue);
                targetObserver.OnNext(lastInterpValue);
            }
            else if (curTime > x2 && (currentIteration + 1 > repeatCount && !isLooping))
            {
                DoComplete();
            }



            /*
            double tempDuration = 0d, easedTime;

            bool handled = false;

            while (!handled)
            {
                ulong delayFrameCount = frameTick - firstFrameCount;
                ulong durationFrameCount = frameTick - firstFrameCount - delayTotalFrameCount;

                switch (currentState)
                {
                    case KeyFramesStates.DoDelay:

                        if (fillMode == FillMode.Backward
                         || fillMode == FillMode.Both)
                        {
                            if (currentIteration == 0)
                            {
                                _targetObserver.OnNext(firstKFValue);
                            }
                            else
                            {
                                _targetObserver.OnNext(lastInterpValue);
                            }
                        }

                        if (delayFrameCount > delayTotalFrameCount)
                        {
                            currentState = KeyFramesStates.DoRun;
                        }
                        else
                        {
                            handled = true;
                        }
                        break;

                    case KeyFramesStates.DoRun:

                        if (isReversed)
                            currentState = KeyFramesStates.RunBackwards;
                        else
                            currentState = KeyFramesStates.RunForwards;

                        break;

                    case KeyFramesStates.RunForwards:

                        if (durationFrameCount > durationTotalFrameCount)
                        {
                            currentState = KeyFramesStates.RunComplete;
                        }
                        else
                        {
                            tempDuration = (double)durationFrameCount / durationTotalFrameCount;
                            currentState = KeyFramesStates.RunApplyValue;

                        }
                        break;

                    case KeyFramesStates.RunBackwards:

                        if (durationFrameCount > durationTotalFrameCount)
                        {
                            currentState = KeyFramesStates.RunComplete;
                        }
                        else
                        {
                            tempDuration = (double)(durationTotalFrameCount - durationFrameCount) / durationTotalFrameCount;
                            currentState = KeyFramesStates.RunApplyValue;
                        }
                        break;

                    case KeyFramesStates.RunApplyValue:

                        easedTime = targetAnimation.Easing.Ease(tempDuration);
                        lastInterpValue = Interpolator(easedTime, neutralValue);
                        _targetObserver.OnNext(lastInterpValue);
                        currentState = KeyFramesStates.DoRun;
                        handled = true;
                        break;

                    case KeyFramesStates.RunComplete:

                        if (checkLoopAndRepeat)
                        {
                            firstFrameCount = frameTick;

                            if (isLooping)
                            {
                                currentState = KeyFramesStates.DoRun;
                            }
                            else if (isRepeating)
                            {
                                if (currentIteration >= repeatCount)
                                {
                                    currentState = KeyFramesStates.Stop;
                                }
                                else
                                {
                                    currentState = KeyFramesStates.DoRun;
                                }
                                currentIteration++;
                            }

                            if (animationDirection == PlaybackDirection.Alternate
                             || animationDirection == PlaybackDirection.AlternateReverse)
                                isReversed = !isReversed;

                            break;
                        }

                        currentState = KeyFramesStates.Stop;
                        break;

                    case KeyFramesStates.Stop:

                        if (fillMode == FillMode.Forward
                         || fillMode == FillMode.Both)
                        {
                            targetControl.SetValue(parent.Property, lastInterpValue, BindingPriority.LocalValue);
                        }
                        _targetObserver.OnCompleted();
                        handled = true;
                        break;
                    default:
                        handled = true;
                        break;
                }
            }
            */
        }

        public IDisposable Subscribe(IObserver<object> observer)
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
