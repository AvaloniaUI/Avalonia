using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

// Special license applies <see href="https://raw.githubusercontent.com/AvaloniaUI/Avalonia/master/src/Avalonia.Base/Rendering/Composition/License.md">License.md</see>

namespace Avalonia.Rendering.Composition.Server
{
    /// <summary>
    /// Server-side <see cref="CompositionObject" /> counterpart.
    /// Is responsible for animation activation and invalidation
    /// </summary>
    internal abstract class ServerObject : IExpressionObject
    {
        public ServerCompositor Compositor { get; }

        public virtual long LastChangedBy => ItselfLastChangedBy;
        public long ItselfLastChangedBy { get; private set; }
        private uint _activationCount;
        public bool IsActive => _activationCount != 0;
        private InlineDictionary<CompositionProperty, ServerObjectSubscriptionStore> _subscriptions;
        private InlineDictionary<CompositionProperty, IAnimationInstance> _animations;
        
        private class ServerObjectSubscriptionStore
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
            
        public ServerObject(ServerCompositor compositor)
        {
            Compositor = compositor;
        }

        public virtual ExpressionVariant GetPropertyForAnimation(string name)
        {
            return default;
        }

        ExpressionVariant IExpressionObject.GetProperty(string name) => GetPropertyForAnimation(name);

        public void Activate()
        {
            _activationCount++;
            if (_activationCount == 1)
                Activated();
        }

        public void Deactivate()
        {
#if DEBUG
            if (_activationCount == 0)
                throw new InvalidOperationException();
#endif
            _activationCount--;
            if (_activationCount == 0)
                Deactivated();
        }

        protected void Activated()
        {
            foreach(var kp in _animations)
                kp.Value.Activate();
        }

        protected void Deactivated()
        {
            foreach(var kp in _animations)
                kp.Value.Deactivate();
        }

        void InvalidateSubscriptions(CompositionProperty property)
        {
            if(_subscriptions.TryGetValue(property, out var subs))
                subs.Invalidate();
        }

        protected void SetValue<T>(CompositionProperty prop, out T field, T value)
        {
            field = value;
            InvalidateSubscriptions(prop);
        }

        protected T GetValue<T>(CompositionProperty prop, ref T field)
        {
            if (_subscriptions.TryGetValue(prop, out var subs))
                subs.IsValid = true;
            return field;
        }

        protected void SetAnimatedValue<T>(CompositionProperty prop, ref T field,
            TimeSpan committedAt, IAnimationInstance animation) where T : struct
        {
            if (IsActive && _animations.TryGetValue(prop, out var oldAnimation))
                oldAnimation.Deactivate();
            _animations[prop] = animation;
            
            animation.Initialize(committedAt, ExpressionVariant.Create(field), prop);
            if(IsActive)
                animation.Activate();
            
            InvalidateSubscriptions(prop);
        }

        protected void SetAnimatedValue<T>(CompositionProperty property, out T field, T value)
        {
            if (_animations.TryGetAndRemoveValue(property, out var animation) && IsActive) 
                animation.Deactivate();
            field = value;
            InvalidateSubscriptions(property);
        }
        
        protected T GetAnimatedValue<T>(CompositionProperty property, ref T field) where T : struct
        {
            if (_subscriptions.TryGetValue(property, out var subscriptions))
                subscriptions.IsValid = true;

            if (_animations.TryGetValue(property, out var animation))
                field = animation.Evaluate(Compositor.ServerNow, ExpressionVariant.Create(field))
                .CastOrDefault<T>();

            return field;
        }
        
        public virtual void NotifyAnimatedValueChanged(CompositionProperty prop)
        {
            InvalidateSubscriptions(prop);
            ValuesInvalidated();
        }

        protected virtual void ValuesInvalidated()
        {
            
        }

        public void SubscribeToInvalidation(CompositionProperty member, IAnimationInstance animation)
        {
            if (!_subscriptions.TryGetValue(member, out var store))
                _subscriptions[member] = store = new ServerObjectSubscriptionStore();
            if (store.Subscribers == null)
                store.Subscribers = new();
            store.Subscribers.AddRef(animation);
        }

        public void UnsubscribeFromInvalidation(CompositionProperty member, IAnimationInstance animation)
        {
            if(_subscriptions.TryGetValue(member, out var store))
                store.Subscribers?.ReleaseRef(animation);
        }

        public virtual CompositionProperty? GetCompositionProperty(string fieldName) => null;

        protected virtual void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
        {
            if (this is IDisposable disp
                && reader.Read<byte>() == 1)
                disp.Dispose();
        }
        
        public void DeserializeChanges(BatchStreamReader reader, Batch batch)
        {
            DeserializeChangesCore(reader, batch.CommittedAt);
            ValuesInvalidated();
            ItselfLastChangedBy = batch.SequenceId;
        }
    }
}
