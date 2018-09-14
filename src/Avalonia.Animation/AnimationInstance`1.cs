using System;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Animation.Utils;
using Avalonia.Data;
using Avalonia.Reactive;

namespace Avalonia.Animation
{
    /// <summary>
    /// Handles interpolation and time-related functions 
    /// for keyframe animations.
    /// </summary>
    internal class AnimationInstance<T> : SingleSubscriberObservableBase<T>
    {
        private T _lastInterpValue;
        private T _firstKFValue;
        private long _repeatCount;
        private double _currentIteration;
        private bool _isLooping;
        private bool _gotFirstKFValue;
        private bool _iterationDelay;
        private FillMode _fillMode;
        private PlaybackDirection _animationDirection;
        private Animator<T> _parent;
        private Animatable _targetControl;
        private T _neutralValue;
        private double _speedRatio;
        private TimeSpan _delay;
        private TimeSpan _duration;
        private Easings.Easing _easeFunc;
        private Action _onCompleteAction;
        private Func<double, T, T> _interpolator;
        private IDisposable _timerSubscription;
        private readonly IClock _baseClock;
        private IClock _clock;

        public AnimationInstance(Animation animation, Animatable control, Animator<T> animator, IClock baseClock, Action OnComplete, Func<double, T, T> Interpolator)
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
            _baseClock = baseClock;
        }

        protected override void Unsubscribed()
        {
            _timerSubscription?.Dispose();
            _clock.PlayState = PlayState.Stop;
        }

        protected override void Subscribed()
        {
            _clock = new Clock(_baseClock);
            _timerSubscription = _clock.Subscribe(Step);
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

        private void DoPlayStates()
        {
            if (_clock.PlayState == PlayState.Stop || _baseClock.PlayState == PlayState.Stop)
                DoComplete();

            if (!_gotFirstKFValue)
            {
                _firstKFValue = (T)_parent.First().Value;
                _gotFirstKFValue = true;
            }
        }

        private void InternalStep(TimeSpan time)
        {
            DoPlayStates();

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
                _currentIteration = (int)Math.Floor((double)((double)time.Ticks / iterationEndpoint.Ticks)) + 2;
            }
            else
            {
                return;
            }

            time = TimeSpan.FromTicks((long)(time.Ticks % iterationEndpoint.Ticks));

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
