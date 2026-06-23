using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Animation
{
    /// <summary>
    /// Manages the lifetime of animation instances as determined by its selector state.
    /// </summary>
    internal class DisposeAnimationInstanceSubject<T>(
        Animator<T> animator,
        Animation animation,
        Animatable control,
        IClock? clock,
        Action? onComplete,
        bool shouldPauseOnInvisible)
        : IObserver<bool>, IDisposable
    {
        private IDisposable? _lastInstance;
        private bool _lastMatch;

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
            _lastInstance = null;
        }

        void IObserver<bool>.OnNext(bool matchVal)
        {
            if (matchVal != _lastMatch)
            {
                _lastInstance?.Dispose();

                if (matchVal)
                {
                    _lastInstance = animator.Run(animation, control, clock, onComplete, shouldPauseOnInvisible);
                }
                else
                {
                    _lastInstance = null;
                }

                _lastMatch = matchVal;
            }
        }
    }
}
