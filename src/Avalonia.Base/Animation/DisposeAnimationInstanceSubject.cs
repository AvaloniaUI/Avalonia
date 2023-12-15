using System;
using System.Threading;
using Avalonia.Animation.Animators;
using Avalonia.Reactive;

namespace Avalonia.Animation
{
    /// <summary>
    /// Manages the lifetime of animation instances as determined by its selector state.
    /// </summary>
    internal class DisposeAnimationInstanceSubject<T> : IObserver<bool>, IDisposable
    {
        private IDisposable? _lastInstance;
        private bool _lastMatch, _lastGate;
        private readonly Animator<T> _animator;
        private readonly Animation _animation;
        private readonly Animatable _control;
        private readonly Action? _onComplete;
        private readonly IClock? _clock;
        private readonly object _lock = new object();

        public DisposeAnimationInstanceSubject(Animator<T> animator, 
            Animation animation, Animatable control, IClock? clock, Action? onComplete, IObservable<bool> state, CompositeDisposable disposable)
        {
            this._animator = animator;
            this._animation = animation;
            this._control = control;
            this._onComplete = onComplete;
            this._clock = clock;
            
            disposable.Add(state.Subscribe(AnimationStateChange));
        }
        
        public void Dispose()
        {
            lock (_lock)
            {
                _lastInstance?.Dispose();
            }
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
            lock (_lock)
            {
                _lastInstance?.Dispose();
                _lastInstance = null;
            }
        }

        private void AnimationStateChange(bool gateState)
        {
            lock (_lock)
            {
                if(Volatile.Read(ref _lastGate) == gateState) return;
                Volatile.Write(ref _lastGate, gateState);
                StateChange();
            }
        }

        public void OnNext(bool matchVal)
        {
            lock (_lock)
            {
                if (Volatile.Read(ref _lastMatch) == matchVal) return;
                Volatile.Write(ref _lastMatch, matchVal);
                StateChange();
            }
        }
        
        void StateChange()
        {
            bool match = Volatile.Read(ref _lastMatch);
            bool gate = Volatile.Read(ref _lastGate);
            
            _lastInstance?.Dispose();

            if (match & gate)
            {
                _lastInstance = _animator.Run(_animation, _control, _clock, _onComplete);
            }
            else
            {
                _lastInstance = null;
            }
        }
        
    }
}
