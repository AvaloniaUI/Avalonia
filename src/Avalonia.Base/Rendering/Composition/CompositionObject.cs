using System;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition
{
    public abstract class CompositionObject : IDisposable, IExpressionObject
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
        private ChangeSet? _changes;

        private static void ThrowInvalidOperation() =>
            throw new InvalidOperationException("There is no server-side counterpart for this object");
        
        private protected ChangeSet Changes
        {
            get
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (Server == null) ThrowInvalidOperation();
                var currentBatch = Compositor.CurrentBatch;
                if (_changes != null && _changes.Batch != currentBatch)
                    _changes = null;
                if (_changes == null)
                {
                    _changes = ChangeSetPool.Get(Server!, currentBatch);
                    currentBatch.Changes!.Add(_changes);
                    Compositor.QueueImplicitBatchCommit();
                }

                return _changes;
            }
        }

        private protected abstract IChangeSetPool ChangeSetPool { get; }

        public void Dispose()
        {
            Changes.Dispose = true;
            IsDisposed = true;
        }

        internal virtual ExpressionVariant GetPropertyForAnimation(string name)
        {
            return default;
        }

        ExpressionVariant IExpressionObject.GetProperty(string name) => GetPropertyForAnimation(name);

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
    }
}