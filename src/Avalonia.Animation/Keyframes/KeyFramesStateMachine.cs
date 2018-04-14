using System;

namespace Avalonia.Animation.Keyframes
{
    /// <summary>
    /// Provides statefulness for an iteration of a keyframe group animation.
    /// </summary>
    internal class KeyFramesStateMachine
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
        KeyFramesStates _currentState;

        internal bool _unsubscribe = false;
        private Animation _parentAnimation;
        private Animatable _targetAnimatable;

        private enum KeyFramesStates
        {
            INITIALIZE,
            DO_DELAY,
            DO_RUN,
            RUN_FORWARDS,
            RUN_BACKWARDS,
            RUN_COMPLETE,
            STOP
        }

        public void Start(Animation animation, Animatable control)
        {
            this._parentAnimation = animation;
            this._targetAnimatable = control;

            // int _delayFrameCount,
            //     _durationFrameCount,
            //     _repeatCount,
            //     _iterationDirection,
            //     _currentIteration,
            //     _totalIteration;

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
            _animationDirection = animation.PlaybackDirection;
            
            switch (_animationDirection)
            {
                case PlaybackDirection.Reverse:
                case PlaybackDirection.AlternateReverse:
                    SetInitialPlaybackDirection(true);
                    break;
                default:
                    SetInitialPlaybackDirection(false);
                    break;
            }

            _currentState = KeyFramesStates.DO_RUN;
        }

        private void SetInitialPlaybackDirection(bool isReversed)
        {
            _isReversed = isReversed;
        }

        private bool GetPlaybackDirection()
        {
            _isReversed = !_isReversed;
            return _isReversed;
        }

        public double Step(PlayState _playState)
        {
            if (_playState == PlayState.Stop) _currentState = KeyFramesStates.STOP;

            switch (_currentState)
            {
                case KeyFramesStates.DO_DELAY:
                    if (_delayFrameCount >= _delayTotalFrameCount)
                    {
                        _currentState = KeyFramesStates.DO_RUN;
                    }
                    _delayFrameCount++;
                    return 0d;

                case KeyFramesStates.DO_RUN:

                    break;

                case KeyFramesStates.RUN_FORWARDS:
                // break;

                case KeyFramesStates.RUN_BACKWARDS:
                // break;

                case KeyFramesStates.RUN_COMPLETE:
                // break;

                case KeyFramesStates.STOP:
                    _unsubscribe = true;
                    break;
            }
            return 0;
        }

    }
}