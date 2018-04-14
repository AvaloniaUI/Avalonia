using System;

namespace Avalonia.Animation.Keyframes
{
    /// <summary>
    /// Provides statefulness for an iteration of a keyframe group animation.
    /// </summary>
    internal class KeyFramesStateMachine<T> : IObservable<object>, IDisposable
    {
        ulong _delayTotalFrameCount,
            _durationTotalFrameCount,
            _delayFrameCount,
            _durationFrameCount,
            _repeatCount,
            _currentIteration,
            _totalIteration;

        bool _isLooping, _isRepeating, _isReversed;
        private PlaybackDirection _animationDirection;
        KeyFramesStates _currentState, _savedState;
        private Animation parentAnimation;
        internal bool _unsubscribe = false;
        double _outputTime = 0d;
        private IObserver<object> targetObserver;

        private enum KeyFramesStates
        {
            INITIALIZE,
            DO_DELAY,
            DO_RUN,
            RUN_FORWARDS,
            RUN_BACKWARDS,
            RUN_COMPLETE,
            PAUSE,
            STOP,
            DISPOSED
        }

        public void Start(Animation animation)
        {
            parentAnimation = animation;
            _delayTotalFrameCount = (ulong)(animation.Delay.Ticks / Timing.FrameTick.Ticks);
            _durationTotalFrameCount = (ulong)(animation.Duration.Ticks / Timing.FrameTick.Ticks);

            if (_delayTotalFrameCount > 0)
            {
                _currentState = KeyFramesStates.DO_DELAY;
            }

            switch (animation.RepeatBehavior)
            {
                case RepeatBehavior.Loop:
                    _isLooping = true;
                    break;
                case RepeatBehavior.Repeat:
                    _isRepeating = true;
                    if (animation.RepeatCount != null)
                    {
                        if (animation.RepeatCount == 0)
                        {
                            throw new InvalidOperationException
                                ($"RepeatCount should be greater than zero when RepeatBehavior is set to Repeat.");
                        }
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

            if (_durationTotalFrameCount > 0)
                _currentState = KeyFramesStates.DO_DELAY;
            else
                _currentState = KeyFramesStates.DO_RUN;
        }

        public double Step(PlayState _playState, Func<double, T> Interpolator)
        {
            if (_currentState == KeyFramesStates.DISPOSED) throw new InvalidProgramException("This KeyFrames Animation is already disposed.");

            if (_playState == PlayState.Stop) _currentState = KeyFramesStates.STOP;

            // Save state and pause the machine
            if (_playState == PlayState.Pause && _currentState != KeyFramesStates.PAUSE)
            {
                _savedState = _currentState;
                _currentState = KeyFramesStates.PAUSE;
            }

            // Resume the previous state
            if (_playState != PlayState.Pause && _currentState == KeyFramesStates.PAUSE)
                _currentState = _savedState;

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
                    return 0d;

                case KeyFramesStates.DO_RUN:
                    // temporary stuff.
                    _currentState = KeyFramesStates.RUN_FORWARDS;

                    goto checkstate;


                case KeyFramesStates.RUN_FORWARDS:
                    // temporary stuff.
                    if (_durationFrameCount > _durationTotalFrameCount)
                        _currentState = KeyFramesStates.RUN_COMPLETE;

                    var tmp1 = (double)_durationFrameCount / _durationTotalFrameCount;
                    var easedTime = parentAnimation.Easing.Ease(tmp1);
                    _durationFrameCount++;
                    targetObserver.OnNext(Interpolator(easedTime));
                    break;

                case KeyFramesStates.RUN_BACKWARDS:
                // break;

                case KeyFramesStates.RUN_COMPLETE:
                // break;

                case KeyFramesStates.STOP:
                    targetObserver.OnCompleted();
                    _unsubscribe = true;
                    break;
            }
            return 0;
        }

        public IDisposable Subscribe(IObserver<object> observer)
        {
            this.targetObserver = observer;
            return this;
        }

        public void Dispose()
        {
            _unsubscribe = true;
            _currentState = KeyFramesStates.DISPOSED;
            targetObserver = null;
        }
    }
}