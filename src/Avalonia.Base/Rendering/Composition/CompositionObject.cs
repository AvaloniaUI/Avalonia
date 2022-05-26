using System;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition
{
    public abstract class CompositionObject : IDisposable
    {
        public ImplicitAnimationCollection? ImplicitAnimations { get; set; }
        internal CompositionObject(Compositor compositor, ServerObject server)
        {
            Compositor = compositor;
            Server = server;
        }
        
        public Compositor Compositor { get; }
        internal ServerObject Server { get; }
        public bool IsDisposed { get; private set; }
        private bool _registeredForSerialization;

        private static void ThrowInvalidOperation() =>
            throw new InvalidOperationException("There is no server-side counterpart for this object");

        public void Dispose()
        {
            //Changes.Dispose = true;
            IsDisposed = true;
        }

        public void StartAnimation(string propertyName, CompositionAnimation animation)
            => StartAnimation(propertyName, animation, null);
        
        internal virtual void StartAnimation(string propertyName, CompositionAnimation animation, ExpressionVariant? finalValue = null)
        {
            throw new ArgumentException("Unknown property " + propertyName);
        }

        public void StartAnimationGroup(ICompositionAnimationBase grp)
        {
            if (grp is CompositionAnimation animation)
            {
                if(animation.Target == null)
                    throw new ArgumentException("Animation Target can't be null");
                StartAnimation(animation.Target, animation);
            }
            else if (grp is CompositionAnimationGroup group)
            {
                foreach (var a in group.Animations)
                {
                    if (a.Target == null)
                        throw new ArgumentException("Animation Target can't be null");
                    StartAnimation(a.Target, a);
                }
            }
        }

        bool StartAnimationGroupPart(CompositionAnimation animation, string target, ExpressionVariant finalValue)
        {
            if(animation.Target == null)
                throw new ArgumentException("Animation Target can't be null");
            if (animation.Target == target)
            {
                StartAnimation(animation.Target, animation, finalValue);
                return true;
            }
            else
            {
                StartAnimation(animation.Target, animation);
                return false;
            }
        }
        
        internal bool StartAnimationGroup(ICompositionAnimationBase grp, string target, ExpressionVariant finalValue)
        {
            if (grp is CompositionAnimation animation)
                return StartAnimationGroupPart(animation, target, finalValue);
            if (grp is CompositionAnimationGroup group)
            {
                var matched = false;
                foreach (var a in group.Animations)
                {
                    if (a.Target == null)
                        throw new ArgumentException("Animation Target can't be null");
                    if (StartAnimationGroupPart(a, target, finalValue))
                        matched = true;
                }

                return matched;
            }

            throw new ArgumentException();
        }

        protected void RegisterForSerialization()
        {
            if (Server == null)
                throw new InvalidOperationException("The object doesn't have an associated server counterpart");
            
            if(_registeredForSerialization)
                return;
            _registeredForSerialization = true;
            Compositor.RegisterForSerialization(this);
        }

        internal void SerializeChanges(BatchStreamWriter writer)
        {
            _registeredForSerialization = false;
            SerializeChangesCore(writer);
        }

        private protected virtual void SerializeChangesCore(BatchStreamWriter writer)
        {
            
        }
    }
}