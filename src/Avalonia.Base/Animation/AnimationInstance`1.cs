using System;
using System.Linq;
using Avalonia.Reactive;
using Avalonia.Animation.Animators;
using Avalonia.Data;

namespace Avalonia.Animation
{
    /// <summary>
    /// Handles interpolation and time-related functions 
    /// for keyframe animations.
    /// </summary>
    internal class AnimationInstance<T> : SingleSubscriberObservableBase<T>
    {
        private readonly Animator<T> _animator;
        private readonly Animation _animation;
        private readonly Animatable _targetControl;
        private readonly Action? _onCompleteAction;
        private IDisposable? _timerSub;
        private EventHandler<AvaloniaPropertyChangedEventArgs>? _propertyChangedDelegate;

        private readonly IClock _baseClock;
        private IClock? _clock;

        private Easings.Easing? _easeFunc;
        private readonly Func<double, T, T> _interpolator;

        private T _neutralValue;
        private FillMode _fillMode;

        private bool _isFirstFrame;
        private bool _isInFirstInitialDelay;
        private T _lastInterpValue;
        private T _initialKFValue;
        private long? _iterationCount;
        private TimeSpan _initialDelay;
        private TimeSpan _iterationDelay;
        private TimeSpan _duration;

        private TimeSpan _timePrev;
        private long _animTimePrev;

        private PlaybackDirection _playbackDirection;
        private PlaybackDirection _playbackDirectionPrev;
        private double _speedRatio;
        private double _speedRatioPrev;
        private bool _timeMovesBackwards;

        private TimeSpan _timeOfLastChange;
        private long _animTimeOfLastChange;

        /// <summary>
        /// Animation's smallest unit of time in terms of TimeSpan Ticks. This can be used
        /// to increase possible runtime of animation before reaching over/under-flow.
        /// </summary>
        /// <remarks>
        /// Value to use here can be found with <code>TimeSpan.FromMilliseconds(x).Ticks</code>
        /// </remarks>
        private const long PRECISION_IN_TICKS = 10_000;

        public AnimationInstance(Animation animation, Animatable control, Animator<T> animator, IClock baseClock, Action? OnComplete, Func<double, T, T> Interpolator)
        {
            _lastInterpValue = default!;
            _animator = animator;
            _animation = animation;
            _targetControl = control;
            _onCompleteAction = OnComplete;
            _interpolator = Interpolator;
            _baseClock = baseClock;
            _initialKFValue = default!;
            _neutralValue = default!;
            _isFirstFrame = true;
            _isInFirstInitialDelay = true;
            _speedRatio = 1;
            FetchProperties();
        }

        private void FetchProperties()
        {
            if (_animation.SpeedRatio < 0d)
                throw new InvalidOperationException("SpeedRatio value should not be negative.");

            if (_animation.Duration < TimeSpan.Zero)
                throw new InvalidOperationException("Duration value cannot be negative.");

            if (_animation.Delay < TimeSpan.Zero)
                throw new InvalidOperationException("Delay value cannot be negative.");

            _easeFunc = _animation.Easing;

            _speedRatioPrev = _speedRatio;
            _speedRatio = _animation.SpeedRatio;

            _initialDelay = _animation.Delay;
            _duration = _animation.Duration;
            _iterationDelay = _animation.DelayBetweenIterations;

            if (_animation.IterationCount.RepeatType == IterationType.Many)
            {
                if (_animation.IterationCount.Value > long.MaxValue)
                    throw new InvalidOperationException("IterationCount value cannot be larger than long.MaxValue.");
                _iterationCount = (long)_animation.IterationCount.Value;
            }
            else
            {
                _iterationCount = null;
            }

            _playbackDirectionPrev = _playbackDirection;
            _playbackDirection = _animation.PlaybackDirection;
            _fillMode = _animation.FillMode;
        }

        protected override void Unsubscribed()
        {
            // Animation may have been stopped before it has finished.
            if (CanApplyFinalFill())
                ApplyFinalFill(_lastInterpValue);

            _targetControl.PropertyChanged -= _propertyChangedDelegate;
            _timerSub?.Dispose();
            _clock!.PlayState = PlayState.Stop;
        }

        protected override void Subscribed()
        {
            _clock = new Clock(_baseClock);
            _timerSub = _clock.Subscribe(Step);
            _propertyChangedDelegate ??= ControlPropertyChanged;
            _targetControl.PropertyChanged += _propertyChangedDelegate;
            UpdateNeutralValue();
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

        private bool CanApplyFinalFill()
        {
            return _fillMode is FillMode.Forward or FillMode.Both;
        }

        private void ApplyFinalFill(T interpolatedValue)
        {
            if (_animator.Property is null)
                throw new InvalidOperationException("Animator has no property specified.");
            _targetControl.SetValue(_animator.Property, interpolatedValue);
        }

        private void DoComplete(bool stopAsIs)
        {
            if (!stopAsIs)
            {
                // Update _lastInterpValue because PublishCompleted might perform final fill
                // and we should ensure that filled value is snapped to last value that animation would
                // reach if it was left running in current state.
                var isIterationReversed = IsIterationReversed(IsAlternatingPlaybackDirection(), _iterationCount + 1);
                _lastInterpValue = FindEdgeValue(IsAnimTimeGoingBackwards(), isIterationReversed);
            }
            _onCompleteAction?.Invoke();
            PublishCompleted();
        }

        private void DoInitialDelay()
        {
            if (_isInFirstInitialDelay)
            {
                if (_fillMode is not (FillMode.Backward or FillMode.Both))
                    return;
            }

            PublishNext(_initialKFValue);
        }

        private void DoIterationDelay(bool isIterationReversed)
        {
            _lastInterpValue = FindEdgeValue(false, isIterationReversed);
            PublishNext(_lastInterpValue);
        }

        private void DoPlayStates()
        {
            if (_clock!.PlayState == PlayState.Stop || _baseClock.PlayState == PlayState.Stop)
                DoComplete(true);

            if (_isFirstFrame)
            {
                // In first frame of animation we determine the expected direction of time.
                _timeMovesBackwards =
                    _animation.PlaybackDirection == PlaybackDirection.Reverse ||
                    _animation.PlaybackDirection == PlaybackDirection.AlternateReverse;

                if (_timeMovesBackwards)
                {
                    if (_animator.Last().Value is T last)
                        _initialKFValue = last;
                }
                else
                {
                    if (_animator.First().Value is T first)
                        _initialKFValue = first;
                }

                _isFirstFrame = false;
            }
        }

        private static bool IsIterationReversed(bool isAlternatingPlaybackDirection, long? iterationIndex)
        {
            if (isAlternatingPlaybackDirection)
                return (iterationIndex & 1) != 0;
            else
                return false;
        }

        private T FindEdgeValue(bool isAnimTimeGoingBackwards, bool isIterationReversed)
        {
            double finalEaseValue = isAnimTimeGoingBackwards ^ isIterationReversed ? 0.0 : 1.0;

            var easedTime = _easeFunc!.Ease(finalEaseValue);
            return _interpolator(easedTime, _neutralValue);
        }

        private bool IsAnimTimeGoingBackwards()
        {
            return
                _playbackDirection == PlaybackDirection.Reverse ||
                _playbackDirection == PlaybackDirection.AlternateReverse;
        }

        private bool IsAlternatingPlaybackDirection()
        {
            return
                _playbackDirection == PlaybackDirection.Alternate ||
                _playbackDirection == PlaybackDirection.AlternateReverse;
        }

        private bool ApplyInitialDelay(ref long animTime)
        {
            // Handle all possible cases of applying initial delay and clamping.
            long delay = _initialDelay.Ticks / PRECISION_IN_TICKS;
            if (_iterationCount.HasValue)
            {
                animTime -= delay;
                if (animTime <= 0)
                {
                    if (animTime < -delay)
                        animTime = -delay;
                    return true;
                }
            }
            else
            {
                // Determine the interval of initial delay.
                long low = -delay;
                long high = 0;

                // Handle all 3 location cases based on above interval.
                if (animTime < low)
                {
                    animTime += delay;
                    if (animTime > 0)
                    {
                        animTime = 0;
                        return true;
                    }
                }
                else if (animTime > high)
                {
                    animTime -= delay;
                    if (animTime < 0)
                    {
                        animTime = 0;
                        return true;
                    }
                }
                else // if (animTime >= low && animTime <= high)
                {
                    animTime = 0;
                    return true;
                }
            }
            return false;
        }

        private void ApplyLimitedClamp(ref long animTime)
        {
            if (_timeMovesBackwards)
            {
                if (animTime > 0)
                    animTime = 0;
            }
            else
            {
                if (animTime < 0)
                    animTime = 0;
            }
        }

        private void InternalStep(TimeSpan time)
        {
            DoPlayStates();
            FetchProperties();

            if (_speedRatio != _speedRatioPrev || _playbackDirection != _playbackDirectionPrev)
            {
                // Remember the time when speed changed.
                // All we can know is that it changed some time between current and prev frame.
                // We assume that this happened exactly at prev frame.
                _timeOfLastChange = _timePrev;
                _animTimeOfLastChange = _animTimePrev;
            }

            bool isAnimTimeGoingBackwards = IsAnimTimeGoingBackwards();

            // Combine SpeedRatio and PlaybackDirection into a single signed speed ratio.
            double speedRatio = isAnimTimeGoingBackwards ? -_speedRatio : _speedRatio;

            // Calculate animation time. That's time that has passed inside
            // the animation since its beginning.
            var timeSinceLastChange = time - _timeOfLastChange;
            var animTimeSinceLastChange = (long)(timeSinceLastChange.Ticks / PRECISION_IN_TICKS * speedRatio);
            var animTime = _animTimeOfLastChange + animTimeSinceLastChange;

            if (_iterationCount.HasValue)
            {
                // Make sure animation time is inside a valid interval.
                ApplyLimitedClamp(ref animTime);
            }

            _timePrev = time;
            _animTimePrev = animTime;

            // Get animation time oriented in the direction of time. Animation running in
            // same direction as it was on first frame will always increase this variable.
            long animTimePositive = _timeMovesBackwards ? -animTime : animTime;

            if (_initialDelay > TimeSpan.Zero)
            {
                bool isCurrentlyInsideDelay = ApplyInitialDelay(ref animTimePositive);
                if (isCurrentlyInsideDelay)
                {
                    DoInitialDelay();
                    return;
                }

                _isInFirstInitialDelay = false;
            }

            var iterDuration = _duration.Ticks / PRECISION_IN_TICKS;
            var iterDelay = _iterationDelay.Ticks / PRECISION_IN_TICKS;
            var iterDurationTotal = iterDuration + iterDelay;

            if (iterDurationTotal <= 0)
            {
                DoComplete(false);
                return;
            }

            // Calculate current iteration info.
            var iterIndex = animTimePositive / iterDurationTotal;
            var iterTime = animTimePositive % iterDurationTotal;

            bool playbackReversed = animTimePositive < 0;
            if (playbackReversed)
            {
                // Animation time is behind the starting point of animation.

                // First negative iteration has index -1, first positive iteration has index 0.
                iterIndex--;

                // Move iteration delay to the front of iteration, which is (when moving
                // backwards through animation time) effectively at the end of iteration.
                iterTime = -iterTime;
            }
            playbackReversed ^= _timeMovesBackwards ^ IsIterationReversed(IsAlternatingPlaybackDirection(), iterIndex);

            var itersUntilEnd = _iterationCount - iterIndex;

            // End animation when limit is reached.
            if (itersUntilEnd <= 0)
            {
                DoComplete(false);
                return;
            }

            var timeUntilIterEnd = iterDurationTotal - iterTime;

            if (iterTime > iterDuration && iterTime <= iterDurationTotal && iterDelay > 0)
            {
                // The last iteration's trailing delay should be skipped.
                if (_iterationCount.HasValue && itersUntilEnd <= 1)
                {
                    DoComplete(false);
                    return;
                }

                DoIterationDelay(playbackReversed);
            }
            else if (iterTime <= iterDuration)
            {
                // Ease and interpolate.
                var normalized = iterTime / (double)iterDuration;
                if (playbackReversed)
                    normalized = 1 - normalized;

                var easedTime = _easeFunc!.Ease(normalized);
                _lastInterpValue = _interpolator(easedTime, _neutralValue);

                PublishNext(_lastInterpValue);
            }
        }

        private void UpdateNeutralValue()
        {
            var property = _animator.Property ?? throw new InvalidOperationException("Animator has no property specified.");
            var baseValue = _targetControl.GetBaseValue(property);

            _neutralValue = baseValue != AvaloniaProperty.UnsetValue ?
                (T)baseValue! : (T)_targetControl.GetValue(property)!;
        }

        private void ControlPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == _animator.Property && e.Priority > BindingPriority.Animation)
            {
                UpdateNeutralValue();
            }
        }
    }
}
