using System;

namespace Avalonia.Animation.Keyframes
{
    /// <summary>
    /// Provides statefulness for an iteration of a keyframe group animation.
    /// </summary>
    internal class KeyFramesStateMachine<T> : IObservable<object>, IDisposable
    {
        
        T _lastInterpValue = default(T);

        private ulong _delayTotalFrameCount,
            _durationTotalFrameCount,
            _delayFrameCount,
            _durationFrameCount,
            _repeatCount,
            _currentIteration;

        private bool _isLooping, _isRepeating, _isReversed, _checkLoopAndRepeat;
        private PlaybackDirection _animationDirection;
        KeyFramesStates _currentState, _savedState;
        private Animation _parentAnimation;
        private Animatable _targetControl;
        internal bool _unsubscribe = false;
        double _outputTime = 0d;
        private IObserver<object> _targetObserver;

        private enum KeyFramesStates
        {
            INITIALIZE,
            DO_DELAY,
            DO_RUN,
            RUN_FORWARDS,
            RUN_BACKWARDS,
            RUN_APPLYVALUE,
            RUN_COMPLETE,
            PAUSE,
            STOP,
            DISPOSED
        }

        public void Initialize(Animation animation, Animatable control)
        {
            _parentAnimation = animation;
            _targetControl = control;

            _delayTotalFrameCount = (ulong)(animation.Delay.Ticks / Timing.FrameTick.Ticks);
            _durationTotalFrameCount = (ulong)(animation.Duration.Ticks / Timing.FrameTick.Ticks);

            switch (animation.RepeatBehavior)
            {
                case RepeatBehavior.Loop:
                    _isLooping = true;
                    _checkLoopAndRepeat = true;
                    break;
                case RepeatBehavior.Repeat:
                    if (animation.RepeatCount != null)
                    {
                        if (animation.RepeatCount == 0)
                        {
                            throw new InvalidOperationException
                                ($"RepeatCount should be greater than zero when RepeatBehavior is set to Repeat.");
                        }
                        _isRepeating = true;
                        _checkLoopAndRepeat = true;
                        _repeatCount = (ulong)animation.RepeatCount;
                    }
                    else
                    {
                        throw new InvalidOperationException
                            ($"RepeatCount should be defined when RepeatBehavior is set to Repeat.");
                    }
                    break;
            }

            switch (animation.PlaybackDirection)
            {
                case PlaybackDirection.Reverse:
                case PlaybackDirection.AlternateReverse:
                    _isReversed = true;
                    break;
                default:
                    _isReversed = false;
                    break;
            }

            _animationDirection = _parentAnimation.PlaybackDirection;

            if (_durationTotalFrameCount > 0)
                _currentState = KeyFramesStates.DO_DELAY;
            else
                _currentState = KeyFramesStates.DO_RUN;

             
        }

        public void Step(PlayState _playState, Func<double, T> Interpolator)
        {
            try
            {
                InternalStep(_playState, Interpolator);
            }
            catch (Exception e)
            {
                _targetObserver?.OnError(e);
            }
        }

        private void InternalStep(PlayState _playState, Func<double, T> Interpolator)
        {
            if (_currentState == KeyFramesStates.DISPOSED)
                throw new InvalidProgramException("This KeyFrames Animation is already disposed.");

            if (_playState == PlayState.Stop)
                _currentState = KeyFramesStates.STOP;

            // Save state and pause the machine
            if (_playState == PlayState.Pause && _currentState != KeyFramesStates.PAUSE)
            {
                _savedState = _currentState;
                _currentState = KeyFramesStates.PAUSE;
            }

            // Resume the previous state
            if (_playState != PlayState.Pause && _currentState == KeyFramesStates.PAUSE)
                _currentState = _savedState;

            double _tempDuration = 0d, _easedTime;

        checkstate:
            switch (_currentState)
            {
                case KeyFramesStates.DO_DELAY:
                    if (_delayFrameCount > _delayTotalFrameCount)
                    {
                        _currentState = KeyFramesStates.DO_RUN;
                        goto checkstate;
                    }
                    _delayFrameCount++;
                    break;

                case KeyFramesStates.DO_RUN:
                    if (_isReversed)
                        _currentState = KeyFramesStates.RUN_BACKWARDS;
                    else
                        _currentState = KeyFramesStates.RUN_FORWARDS;
                    goto checkstate;

                case KeyFramesStates.RUN_FORWARDS:
                    if (_durationFrameCount > _durationTotalFrameCount)
                    {
                        _currentState = KeyFramesStates.RUN_COMPLETE;
                        goto checkstate;
                    }

                    _tempDuration = (double)_durationFrameCount / _durationTotalFrameCount;
                    _currentState = KeyFramesStates.RUN_APPLYVALUE;

                    goto checkstate;

                case KeyFramesStates.RUN_BACKWARDS:
                    if (_durationFrameCount > _durationTotalFrameCount)
                    {
                        _currentState = KeyFramesStates.RUN_COMPLETE;
                        goto checkstate;
                    }

                    _tempDuration = (double)(_durationTotalFrameCount - _durationFrameCount) / _durationTotalFrameCount;
                    _currentState = KeyFramesStates.RUN_APPLYVALUE;

                    goto checkstate;

                case KeyFramesStates.RUN_APPLYVALUE:

                    _easedTime = _parentAnimation.Easing.Ease(_tempDuration);

                    _durationFrameCount++;
                    _lastInterpValue = Interpolator(_easedTime);
                    _targetObserver.OnNext(_lastInterpValue);
                    _currentState = KeyFramesStates.DO_RUN;

                    break;

                case KeyFramesStates.RUN_COMPLETE:

                    if (_checkLoopAndRepeat)
                    {
                        _delayFrameCount = 0;
                        _durationFrameCount = 0;

                        if (_isLooping)
                        {
                            _currentState = KeyFramesStates.DO_RUN;
                        }
                        else if (_isRepeating)
                        {
                            if (_currentIteration >= _repeatCount)
                            {
                                _currentState = KeyFramesStates.STOP;
                            }
                            else
                            {
                                _currentState = KeyFramesStates.DO_RUN;
                            }
                            _currentIteration++;
                        }

                        if (_animationDirection == PlaybackDirection.Alternate
                         || _animationDirection == PlaybackDirection.AlternateReverse)
                            _isReversed = !_isReversed;

                        break;
                    }

                    _currentState = KeyFramesStates.STOP;
                    goto checkstate;

                case KeyFramesStates.STOP:
                    _targetObserver.OnCompleted();
                    break;
            }

        }

        public IDisposable Subscribe(IObserver<object> observer)
        {
            _targetObserver = observer;
            return this;
        }
        public void Dispose()
        {
            _unsubscribe = true;
            _currentState = KeyFramesStates.DISPOSED;
        }
    }
}