// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Animation.Animators;
using Avalonia.Animation.Utils;
using Avalonia.Collections;
using Avalonia.Data;
using Avalonia.Reactive;

namespace Avalonia.Animation
{
    /// <summary>
    /// Manages the lifetime of animation instances as determined by its selector state.
    /// </summary>
    internal class DisposeAnimationInstanceSubject<T> : IObserver<bool>, IDisposable
    {
        private IDisposable _lastInstance;
        private bool _lastMatch;
        private Animator<T> _animator;
        private Animation _animation;
        private Animatable _control;
        private Action _onComplete;
        private IClock _clock;

        public DisposeAnimationInstanceSubject(Animator<T> animator, Animation animation, Animatable control, IClock clock, Action onComplete)
        {
            this._animator = animator;
            this._animation = animation;
            this._control = control;
            this._onComplete = onComplete;
            this._clock = clock;
        }
        public void Dispose()
        {
            _lastInstance?.Dispose();
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
            _lastInstance?.Dispose();
        }

        void IObserver<bool>.OnNext(bool matchVal)
        {
            if (matchVal != _lastMatch)
            {
                _lastInstance?.Dispose();
                if (matchVal)
                {
                    _lastInstance = _animator.Run(_animation, _control, _clock, _onComplete);
                }
                _lastMatch = matchVal;
            }
        }
    }
}