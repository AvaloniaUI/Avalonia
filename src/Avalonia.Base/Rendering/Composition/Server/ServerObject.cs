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

        protected virtual void Activated()
        {
            
        }

        protected virtual void Deactivated()
        {
            
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected int GetOffset(ref ServerObjectSubscriptionStore field)
        {
            return Unsafe.ByteOffset(ref _activationCount,
                    ref Unsafe.As<ServerObjectSubscriptionStore, uint>(ref field))
                .ToInt32();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected ref ServerObjectSubscriptionStore GetStoreFromOffset(int offset)
        {
#if DEBUG
            if (offset == 0)
                throw new InvalidOperationException();
#endif
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
            if (store.Subscribers == null)
                store.Subscribers = new();
            store.Subscribers.AddRef(animation);
        }

        public void UnsubscribeFromInvalidation(int member, IAnimationInstance animation)
        {
            ref var store = ref GetStoreFromOffset(member);
            store.Subscribers?.ReleaseRef(animation);
        }

        public virtual int? GetFieldOffset(string fieldName) => null;

        protected virtual void DeserializeChangesCore(BatchStreamReader reader, TimeSpan commitedAt)
        {
            if (this is IDisposable disp
                && reader.Read<byte>() == 1)
                disp.Dispose();
        }
        
        public void DeserializeChanges(BatchStreamReader reader, Batch batch)
        {
            DeserializeChangesCore(reader, batch.CommitedAt);
            ValuesInvalidated();
            ItselfLastChangedBy = batch.SequenceId;
        }
    }
}