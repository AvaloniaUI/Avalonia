using System;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Animation.Utils;
using Avalonia.Data;
using Avalonia.Reactive;

namespace Avalonia.Animation
{
    /// <summary>
    /// Handles interpolatoin and time-related functions 
    /// for keyframe animations.
    /// </summary>
    internal class AnimationsEngine<T> : SingleSubscriberObservableBase<T>
    {
        private T _lastInterpValue;
        private T _firstKFValue;
        private long _repeatCount;
        private double _currentIteration;
        private bool _isLooping;
        private bool _gotFirstKFValue;
        private bool _gotFirstFrameCount;
        private bool _iterationDelay;
        private FillMode _fillMode;
        private PlaybackDirection _animationDirection;
        private Animator<T> _parent;
        private Animatable _targetControl;
        private T _neutralValue;
        private double _speedRatio;
        private TimeSpan _delay;
        private TimeSpan _duration;
        private TimeSpan _firstFrameCount;
        private TimeSpan _internalClock;
        private TimeSpan? _previousClock;
        private Easings.Easing _easeFunc;
        private Action _onCompleteAction;
        private Func<double, T, T> _interpolator;
        private IDisposable _timerSubscription;

        public AnimationsEngine(Animation animation, Animatable control, Animator<T> animator, Action OnComplete, Func<double, T, T> Interpolator)
        {
            if (animation.SpeedRatio <= 0)
                throw new InvalidOperationException("Speed ratio cannot be negative or zero.");

            if (animation.Duration.TotalSeconds <= 0)
                throw new InvalidOperationException("Duration cannot be negative or zero.");

            _parent = animator;
            _easeFunc = animation.Easing;
            _targetControl = control;
            _neutralValue = (T)_targetControl.GetValue(_parent.Property);

            _speedRatio = animation.SpeedRatio;

            _delay = animation.Delay;
            _duration = animation.Duration;
            _iterationDelay = animation.DelayBetweenIterations;

            switch (animation.RepeatCount.RepeatType)
            {
                case RepeatType.None:
                    _repeatCount = 1;
                    break;
                case RepeatType.Loop:
                    _isLooping = true;
                    break;
                case RepeatType.Repeat:
                    _repeatCount = (long)animation.RepeatCount.Value;
                    break;
            }

            _animationDirection = animation.PlaybackDirection;
            _fillMode = animation.FillMode;
            _onCompleteAction = OnComplete;
            _interpolator = Interpolator;
        }

        protected override void Unsubscribed()
        {
            _timerSubscription?.Dispose();
        }

        protected override void Subscribed()
        {
            _timerSubscription = Timing.AnimationsTimer
                                       .Subscribe(p => this.Step(p));
        }

        public void Step(TimeSpan frameTick)
        {
            try
            {
                InternalStep(frameTick);
            }
            catch (Exception e)
            {
                PublishError(e);
            }
        }

        private void DoComplete()
        {
            if (_fillMode == FillMode.Forward || _fillMode == FillMode.Both)
                _targetControl.SetValue(_parent.Property, _lastInterpValue, BindingPriority.LocalValue);

            _onCompleteAction?.Invoke();
            PublishCompleted();
        }

        private void DoDelay()
        {
            if (_fillMode == FillMode.Backward || _fillMode == FillMode.Both)
                if (_currentIteration == 0)
                    PublishNext(_firstKFValue);
                else
                    PublishNext(_lastInterpValue);
        }

        private void DoPlayStatesAndTime(TimeSpan systemTime)
        {
            if (Animation.GlobalPlayState == PlayState.Stop || _targetControl.PlayState == PlayState.Stop)
                DoComplete();

            if (!_previousClock.HasValue)
            {
                _previousClock = systemTime;
                _internalClock = TimeSpan.Zero;
            }
            else
            {
                if (Animation.GlobalPlayState == PlayState.Pause || _targetControl.PlayState == PlayState.Pause)
                {
                    _previousClock = systemTime;
                    return;
                }
                var delta = systemTime - _previousClock;
                _internalClock += delta.Value;
                _previousClock = systemTime;
            }

            if (!_gotFirstKFValue)
            {
                _firstKFValue = (T)_parent.First().Value;
                _gotFirstKFValue = true;
            }

            if (!_gotFirstFrameCount)
            {
                _firstFrameCount = _internalClock;
                _gotFirstFrameCount = true;
            }
        }

        private void InternalStep(TimeSpan systemTime)
        {
            DoPlayStatesAndTime(systemTime);
 
            var time = _internalClock - _firstFrameCount;
            var delayEndpoint = _delay;
            var iterationEndpoint = delayEndpoint + _duration;

            //determine if time is currently in the first iteration.
            if (time >= TimeSpan.Zero & time <= iterationEndpoint)
            {
                _currentIteration = 1;
            }
            else if (time > iterationEndpoint)
            {
                //Subtract first iteration to properly get the subsequent iteration time
                time -= iterationEndpoint;

                if (!_iterationDelay & delayEndpoint > TimeSpan.Zero)
                {
                    delayEndpoint = TimeSpan.Zero;
                    iterationEndpoint = _duration;
                }

                //Calculate the current iteration number
                _currentIteration = (int)Math.Floor((double)time.Ticks / iterationEndpoint.Ticks) + 2;
            }
            else
            {
                _previousClock = systemTime;
                return;
            }

            time = TimeSpan.FromTicks(time.Ticks % iterationEndpoint.Ticks);

            if (!_isLooping)
            {
                if (_currentIteration > _repeatCount)
                    DoComplete();

                if (time > iterationEndpoint)
                    DoComplete();
            }

            // Determine if the current iteration should have its normalized time inverted.
            bool isCurIterReverse = _animationDirection == PlaybackDirection.Normal ? false :
                                    _animationDirection == PlaybackDirection.Alternate ? (_currentIteration % 2 == 0) ? false : true :
                                    _animationDirection == PlaybackDirection.AlternateReverse ? (_currentIteration % 2 == 0) ? true : false :
                                    _animationDirection == PlaybackDirection.Reverse ? true : false;

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
                var easedTime = _easeFunc.Ease(interpVal);
                _lastInterpValue = _interpolator(easedTime, _neutralValue);

                PublishNext(_lastInterpValue);
            }
        }
    }
}