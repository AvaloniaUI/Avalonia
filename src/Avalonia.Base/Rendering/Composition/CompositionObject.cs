using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition
{
    /// <summary>
    /// Base class of the composition API representing a node in the visual tree structure.
    /// Composition objects are the visual tree structure on which all other features of the composition API use and build on.
    /// The API allows developers to define and create one or many <see cref="CompositionVisual" /> objects each representing a single node in a Visual tree.
    /// </summary>
    public abstract class CompositionObject : ICompositorSerializable
    {
        /// <summary>
        /// The collection of implicit animations attached to this object.
        /// </summary>
        public ImplicitAnimationCollection? ImplicitAnimations { get; set; }

        private protected InlineDictionary<CompositionProperty, IAnimationInstance> PendingAnimations;
        internal CompositionObject(Compositor compositor, SimpleServerObject? server)
        {
            Compositor = compositor;
            Server = server;
        }
        
        /// <summary>
        /// The associated Compositor
        /// </summary>
        public Compositor Compositor { get; }

        SimpleServerObject ICompositorSerializable.TryGetServer(Compositor c)
        {
            Debug.Assert(c == Compositor);
            return Server ?? ThrowInvalidOperation();
        }

        internal SimpleServerObject? Server { get; }
        public bool IsDisposed { get; private set; }
        private bool _registeredForSerialization;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static SimpleServerObject ThrowInvalidOperation() =>
            throw new InvalidOperationException("There is no server-side counterpart for this object");

        protected internal void Dispose()
        {
            if (!IsDisposed && Server != null)
                Compositor.DisposeOnNextBatch(Server);
            IsDisposed = true;
        }

        /// <summary>
        /// Connects an animation with the specified property of the object and starts the animation.
        /// </summary>
        public void StartAnimation(string propertyName, CompositionAnimation animation)
            => StartAnimation(propertyName, animation, null);
        
        internal virtual void StartAnimation(string propertyName, CompositionAnimation animation, ExpressionVariant? finalValue)
        {
            throw new ArgumentException("Unknown property " + propertyName);
        }

        /// <summary>Disconnects an animation from the specified property and stops the animation.</summary>
        /// <param name="propertyName">The name of the property to disconnect the animation from.</param>
        public void StopAnimation(string propertyName)
        {
            if (propertyName is null)
                throw new ArgumentNullException(nameof(propertyName));
            if (Server is not ServerObject srv)
                return;
            var prop = srv.GetCompositionProperty(propertyName) ?? throw new ArgumentException("Unknown property " + propertyName);
            srv.Animations?.RemoveAnimationForProperty(prop);
        }

        /// <summary>
        /// Starts an animation group.
        /// The StartAnimationGroup method on CompositionObject lets you start CompositionAnimationGroup.
        /// All the animations in the group will be started at the same time on the object.
        /// </summary>
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

        /// <summary>Stops an animation group.</summary>
        /// <param name="grp">The animation group to stop.</param>
        public void StopAnimationGroup(ICompositionAnimationBase grp)
        {
            if (grp is CompositionAnimation animation)
            {
                if (animation.Target == null)
                    throw new ArgumentException("Animation Target can't be null");
                StopAnimation(animation.Target);
            }
            else if (grp is CompositionAnimationGroup group)
            {
                foreach (var a in group.Animations)
                {
                    if (a.Target == null)
                        throw new ArgumentException("Animation Target can't be null");
                    StopAnimation(a.Target);
                }
            }
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

        void ICompositorSerializable.SerializeChanges(Compositor c, BatchStreamWriter writer)
        {
            Debug.Assert(c == Compositor);
            _registeredForSerialization = false;
            SerializeChangesCore(writer);
        }

        private protected virtual void SerializeChangesCore(BatchStreamWriter writer)
        {
        }
    }
}
