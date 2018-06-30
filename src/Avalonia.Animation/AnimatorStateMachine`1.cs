using System;
using System.Linq;
using Avalonia.Data;

namespace Avalonia.Animation
{
    /// <summary>
    /// Provides statefulness for an iteration of a keyframe animation.
    /// </summary>
    internal class AnimatorStateMachine<T> : IObservable<object>, IDisposable
    {
        object _lastInterpValue;
        object _firstKFValue;

        private ulong _delayTotalFrameCount;
        private ulong _durationTotalFrameCount;
        private ulong _delayFrameCount;
        private ulong _durationFrameCount;
        private ulong _repeatCount;
        private ulong _currentIteration;

        private bool _isLooping;
        private bool _isRepeating;
        private bool _isReversed;
        private bool _checkLoopAndRepeat;
        private bool _gotFirstKFValue;

        private FillMode _fillMode;
        private PlaybackDirection _animationDirection;
        private MachineStates _currentState;
        private MachineStates _savedState;
        private Animator<T> _parent;
        private Animation _targetAnimation;
        private Animatable _targetControl;
        private T _neutralValue;
        internal bool _unsubscribe = false;
        private IObserver<object> _targetObserver;

        [Flags]
        private enum MachineStates
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

        public void Initialize(Animation animation, Animatable control, Animator<T> keyframes)
        {
            _parent = keyframes;
            _targetAnimation = animation;
            _targetControl = control;
            _neutralValue = (T)_targetControl.GetValue(_parent.Property);

            _delayTotalFrameCount = (ulong)(animation.Delay.Ticks / Timing.FrameTick.Ticks);
            _durationTotalFrameCount = (ulong)(animation.Duration.Ticks / Timing.FrameTick.Ticks);

            switch (animation.RepeatCount.RepeatType)
            {
                case RepeatType.Loop:
                    _isLooping = true;
                    _checkLoopAndRepeat = true;
                    break;
                case RepeatType.Repeat:
                    _isRepeating = true;
                    _checkLoopAndRepeat = true;
                    _repeatCount = animation.RepeatCount.Value;
                    break;
            }

            _isReversed = (animation.PlaybackDirection & PlaybackDirection.Reverse) != 0;
            _animationDirection = _targetAnimation.PlaybackDirection;
            _fillMode = _targetAnimation.FillMode;

            if (_durationTotalFrameCount > 0)
                _currentState = MachineStates.DoDelay;
            else
                _currentState = MachineStates.DoRun;
        }

        public void Step(long time, Func<double, T, T> Interpolator)
        {
            try
            {
                InternalStep(time, Interpolator);
            }
            catch (Exception e)
            {
                _targetObserver?.OnError(e);
            }
        }

        private void InternalStep(long time, Func<double, T, T> Interpolator)
        {
            if (!_gotFirstKFValue)
            {
                _firstKFValue = _parent.First().Value;
                _gotFirstKFValue = true;
            }

            if (_currentState == MachineStates.Disposed)
                throw new InvalidProgramException("This KeyFrames Animation is already disposed.");

            var playState = Timing._globalPlayState;

            // Override the global play state when the target
            // play state changes
            if(_targetControl.PlayState != PlayState.Run)
                playState = _targetControl.PlayState;

            // Stop the animation.
            if (playState == PlayState.Stop
             || _targetControl.PlayState == PlayState.Stop)
                _currentState = MachineStates.Stop;

            // Save state and pause the machine
            if (playState == PlayState.Pause && _currentState != MachineStates.Pause)
            {
                _savedState = _currentState;
                _currentState = MachineStates.Pause;
            }

            // Resume the previous state
            if (playState != PlayState.Pause && _currentState == MachineStates.Pause)
                _currentState = _savedState;

            double _tempDuration = 0d, _easedTime;

        checkstate:
            switch (_currentState)
            {
                case MachineStates.DoDelay:

                    if (_fillMode == FillMode.Backward
                     || _fillMode == FillMode.Both)
                    {
                        if (_currentIteration == 0)
                        {
                            _targetObserver.OnNext(_firstKFValue);
                        }
                        else
                        {
                            _targetObserver.OnNext(_lastInterpValue);
                        }
                    }

                    if (_delayFrameCount > _delayTotalFrameCount)
                    {
                        _currentState = MachineStates.DoRun;
                        goto checkstate;
                    }
                    _delayFrameCount++;
                    break;

                case MachineStates.DoRun:

                    if (_isReversed)
                        _currentState = MachineStates.RunBackwards;
                    else
                        _currentState = MachineStates.RunForwards;

                    goto checkstate;

                case MachineStates.RunForwards:

                    if (_durationFrameCount > _durationTotalFrameCount)
                    {
                        _currentState = MachineStates.RunComplete;
                        goto checkstate;
                    }

                    _tempDuration = (double)_durationFrameCount / _durationTotalFrameCount;
                    _currentState = MachineStates.RunApplyValue;

                    goto checkstate;

                case MachineStates.RunBackwards:

                    if (_durationFrameCount > _durationTotalFrameCount)
                    {
                        _currentState = MachineStates.RunComplete;
                        goto checkstate;
                    }

                    _tempDuration = (double)(_durationTotalFrameCount - _durationFrameCount) / _durationTotalFrameCount;
                    _currentState = MachineStates.RunApplyValue;

                    goto checkstate;

                case MachineStates.RunApplyValue:

                    _easedTime = _targetAnimation.Easing.Ease(_tempDuration);

                    _durationFrameCount++;
                    _lastInterpValue = Interpolator(_easedTime, _neutralValue);
                    _targetObserver.OnNext(_lastInterpValue);
                    _currentState = MachineStates.DoRun;

                    break;

                case MachineStates.RunComplete:

                    if (_checkLoopAndRepeat)
                    {
                        _delayFrameCount = 0;
                        _durationFrameCount = 0;

                        if (_isLooping)
                        {
                            _currentState = MachineStates.DoRun;
                        }
                        else if (_isRepeating)
                        {
                            if (_currentIteration >= _repeatCount)
                            {
                                _currentState = MachineStates.Stop;
                            }
                            else
                            {
                                _currentState = MachineStates.DoRun;
                            }
                            _currentIteration++;
                        }

                        if (_animationDirection == PlaybackDirection.Alternate
                         || _animationDirection == PlaybackDirection.AlternateReverse)
                            _isReversed = !_isReversed;

                        break;
                    }

                    _currentState = MachineStates.Stop;
                    goto checkstate;

                case MachineStates.Stop:

                    if (_fillMode == FillMode.Forward
                     || _fillMode == FillMode.Both)
                    {
                        _targetControl.SetValue(_parent.Property, _lastInterpValue, BindingPriority.LocalValue);
                    }
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
            _currentState = MachineStates.Disposed;
        }
    }
}