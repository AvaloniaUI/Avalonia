using System;

namespace Avalonia.Animation.Animators
{
    internal class DisposeCompositionAnimationInstanceSubject<T>(
        CompositionAnimator<T> animator,
        Animation animation,
        Visual control) : IObserver<bool>, IDisposable
    {
        private IDisposable? _lastInstance;
        private bool _lastMatch;

        public void Dispose()
        {
            _lastInstance?.Dispose();
            _lastInstance = null;
        }

        public void OnCompleted()
        {

        }

        public void OnError(Exception error)
        {
            _lastInstance?.Dispose();
            _lastInstance = null;
        }

        public void OnNext(bool matchVal)
        {
            if (matchVal != _lastMatch)
            {
                _lastInstance?.Dispose();

                if (matchVal)
                {
                    _lastInstance = animator.AttachAnimation(control, animation);
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
