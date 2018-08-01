using System;
using System.Linq;
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

        private ulong delayTotalFrameCount;
        private ulong durationTotalFrameCount;
        private ulong delayFrameCount;
        private ulong durationFrameCount;
        private ulong repeatCount;
        private ulong currentIteration;

        private bool isLooping;
        private bool isRepeating;
        private bool isReversed;
        private bool checkLoopAndRepeat;
        private bool gotFirstKFValue;

        private FillMode fillMode;
        private PlaybackDirection animationDirection;
        private KeyFramesStates currentState;
        private KeyFramesStates savedState;
        private Animator<T> parent;
        private Animation targetAnimation;
        private Animatable targetControl;
        private T neutralValue;
        internal bool unsubscribe = false;
        private IObserver<T> targetObserver;

        [Flags]
        private enum KeyFramesStates
        {
            Initialize,
            DoDelay,
            DoRun,
            RunForwards,
            RunBackwards,
            RunApplyValue,
            RunComplete,
            Pause,
            Stop,
            Disposed
        }

        public void Initialize(Animation animation, Animatable control, Animator<T> animator)
        {
            parent = animator;
            targetAnimation = animation;
            targetControl = control;
            neutralValue = (T)targetControl.GetValue(parent.Property);

            delayTotalFrameCount = (ulong)(animation.Delay.Ticks / Timing.FrameTick.Ticks);
            durationTotalFrameCount = (ulong)(animation.Duration.Ticks / Timing.FrameTick.Ticks);

            switch (animation.RepeatCount.RepeatType)
            {
                case RepeatType.Loop:
                    isLooping = true;
                    checkLoopAndRepeat = true;
                    break;
                case RepeatType.Repeat:
                    isRepeating = true;
                    checkLoopAndRepeat = true;
                    repeatCount = animation.RepeatCount.Value;
                    break;
            }

            isReversed = (animation.PlaybackDirection & PlaybackDirection.Reverse) != 0;
            animationDirection = targetAnimation.PlaybackDirection;
            fillMode = targetAnimation.FillMode;

            if (durationTotalFrameCount > 0)
                currentState = KeyFramesStates.DoDelay;
            else
                currentState = KeyFramesStates.DoRun;
        }

        public void Step(PlayState playState, Func<double, T, T> Interpolator)
        {
            try
            {
                InternalStep(playState, Interpolator);
            }
            catch (Exception e)
            {
                targetObserver?.OnError(e);
            }
        }

        private void InternalStep(PlayState playState, Func<double, T, T> Interpolator)
        {
            if (!gotFirstKFValue)
            {
                firstKFValue = (T)parent.First().Value;
                gotFirstKFValue = true;
            }

            if (currentState == KeyFramesStates.Disposed)
                throw new InvalidProgramException("This KeyFrames Animation is already disposed.");

            if (playState == PlayState.Stop)
                currentState = KeyFramesStates.Stop;

            // // Save state and pause the machine
            // if (playState == PlayState.Pause && currentState != KeyFramesStates.Pause)
            // {
            //     savedState = currentState;
            //     currentState = KeyFramesStates.Pause;
            // }

            // // Resume the previous state
            // if (playState != PlayState.Pause && currentState == KeyFramesStates.Pause)
            //     currentState = savedState;

            double tempDuration = 0d, easedTime;

            bool handled = false;

            while (!handled)
            {
                switch (currentState)
                {
                    case KeyFramesStates.DoDelay:

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

                        if (delayFrameCount > delayTotalFrameCount)
                        {
                            currentState = KeyFramesStates.DoRun;
                        }
                        else
                        {
                            handled = true;
                            delayFrameCount++;
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

                        durationFrameCount++;
                        lastInterpValue = Interpolator(easedTime, neutralValue);
                        targetObserver.OnNext(lastInterpValue);
                        currentState = KeyFramesStates.DoRun;
                        handled = true;
                        break;

                    case KeyFramesStates.RunComplete:

                        if (checkLoopAndRepeat)
                        {
                            delayFrameCount = 0;
                            durationFrameCount = 0;

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
                        targetObserver.OnCompleted();
                        handled = true;
                        break;
                    default:
                        handled = true;
                        break;
                }
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
            currentState = KeyFramesStates.Disposed;
        }
    }
}
