using System;
using System.Runtime.InteropServices;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Animations
{
    /// <summary>
    /// This is the first element of both animated and non-animated value stores.
    /// It's used to propagate property invalidation to subscribers 
    /// </summary>
    
    internal struct ServerObjectSubscriptionStore
    {
        public bool IsValid;
        public RefTrackingDictionary<IAnimationInstance>? Subscribers;

        public void Invalidate()
        {
            if (IsValid)
                return;
            IsValid = false;
            if (Subscribers != null)
                foreach (var sub in Subscribers)
                    sub.Key.Invalidate();
        }
    }

    /// <summary>
    /// The value store for non-animated values that can still be referenced by animations.
    /// Simply stores the value and notifies subscribers
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ServerValueStore<T>
    {
        // HAS TO BE THE FIRST FIELD, accessed by field offset from ServerObject
        private ServerObjectSubscriptionStore Subscriptions;
        private T _value;
        public T Value
        {
            set
            {
                _value = value;
                Subscriptions.Invalidate();
            }
            get
            {
                Subscriptions.IsValid = true;
                return _value;
            }
        }
    }
    
    /// <summary>
    /// Value store for potentially animated values. Can hold both direct value and animation instance.
    /// Is also responsible for activating/deactivating the animation when container object is activated/deactivated 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ServerAnimatedValueStore<T> where T : struct
    {
        // HAS TO BE THE FIRST FIELD, accessed by field offset from ServerObject
        private ServerObjectSubscriptionStore Subscriptions;
        private IAnimationInstance? _animation;
        private T _direct;
        private T? _lastAnimated;

        public T GetAnimated(ServerCompositor compositor)
        {
            Subscriptions.IsValid = true;
            if (_animation == null)
                return _direct;
            var v = _animation.Evaluate(compositor.ServerNow, ExpressionVariant.Create(_direct))
                .CastOrDefault<T>();
            _lastAnimated = v;
            return v;
        }

        public void Activate(ServerObject parent)
        {
            if (_animation != null)
                _animation.Activate();
        }

        public void Deactivate(ServerObject parent)
        {
            if (_animation != null)
                _animation.Deactivate();
        }

        private T LastAnimated => _animation != null ? _lastAnimated ?? _direct : _direct;

        public bool IsAnimation => _animation != null;

        public void SetAnimation(ServerObject target, TimeSpan commitedAt, IAnimationInstance animation, int storeOffset)
        {
            _direct = default;
            if (_animation != null)
            {
                if (target.IsActive)
                    _animation.Deactivate();
            }

            _animation = animation;
            _animation.Initialize(commitedAt, ExpressionVariant.Create(LastAnimated), storeOffset);
            if (target.IsActive)
                _animation.Activate();
            
            Subscriptions.Invalidate();
        }

        public void SetValue(ServerObject target, T value)
        {
            if (_animation != null)
            {
                if (target.IsActive)
                    _animation.Deactivate();
            }

            _animation = null;
            _direct = value;
            Subscriptions.Invalidate();
        }
    }
}