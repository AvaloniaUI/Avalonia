using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server
{
    internal abstract class ServerObject : IExpressionObject
    {
        public ServerCompositor Compositor { get; }

        public virtual long LastChangedBy => ItselfLastChangedBy;
        public long ItselfLastChangedBy { get; private set; }
        private uint _activationCount;
        public bool IsActive => _activationCount != 0;

        public ServerObject(ServerCompositor compositor)
        {
            Compositor = compositor;
        }

        protected virtual void ApplyCore(ChangeSet changes)
        {
            
        }

        public void Apply(ChangeSet changes)
        {
            ApplyCore(changes);
            ValuesInvalidated();
            ItselfLastChangedBy = changes.Batch!.SequenceId;
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

        protected virtual void Activated()
        {
            
        }

        protected virtual void Deactivated()
        {
            
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected int GetOffset<T>(ref T field) where T : struct
        {
            return Unsafe.ByteOffset(ref Unsafe.As<uint, object>(ref _activationCount),
                    ref Unsafe.As<T, object>(ref field))
                .ToInt32();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref ServerObjectSubscriptionStore GetStoreFromOffset(int offset)
        {
            return ref Unsafe.As<uint, ServerObjectSubscriptionStore>(ref Unsafe.AddByteOffset(ref _activationCount,
                new IntPtr(offset)));
        }

        public void NotifyAnimatedValueChanged(int offset)
        {
            ref var store = ref GetStoreFromOffset(offset);
            store.Invalidate();
            ValuesInvalidated();
        }

        protected virtual void ValuesInvalidated()
        {
            
        }

        public void SubscribeToInvalidation(int member, IAnimationInstance animation)
        {
            ref var store = ref GetStoreFromOffset(member);
            if (store.Subscribers.AddRef(animation))
                Activate();
        }

        public void UnsubscribeFromInvalidation(int member, IAnimationInstance animation)
        {
            ref var store = ref GetStoreFromOffset(member);
            if (store.Subscribers.ReleaseRef(animation))
                Deactivate();
        }

        public virtual int? GetFieldOffset(string fieldName) => null;
    }
}