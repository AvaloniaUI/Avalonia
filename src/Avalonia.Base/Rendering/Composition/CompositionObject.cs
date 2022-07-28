using System;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

// Special license applies, see //file: src/Avalonia.Base/Rendering/Composition/License.md

namespace Avalonia.Rendering.Composition
{
    /// <summary>
    /// Base class of the composition API representing a node in the visual tree structure.
    /// Composition objects are the visual tree structure on which all other features of the composition API use and build on.
    /// The API allows developers to define and create one or many <see cref="CompositionVisual" /> objects each representing a single node in a Visual tree.
    /// </summary>
    public abstract class CompositionObject : IDisposable
    {
        /// <summary>
        /// The collection of implicit animations attached to this object.
        /// </summary>
        public ImplicitAnimationCollection? ImplicitAnimations { get; set; }

        private protected InlineDictionary<CompositionProperty, IAnimationInstance> PendingAnimations;
        internal CompositionObject(Compositor compositor, ServerObject server)
        {
            Compositor = compositor;
            Server = server;
        }
        
        /// <summary>
        /// The associated Compositor
        /// </summary>
        public Compositor Compositor { get; }
        internal ServerObject Server { get; }
        public bool IsDisposed { get; private set; }
        private bool _registeredForSerialization;

        private static void ThrowInvalidOperation() =>
            throw new InvalidOperationException("There is no server-side counterpart for this object");

        public void Dispose()
        {
            RegisterForSerialization();
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
            if (Server is IDisposable)
                writer.Write((byte)(IsDisposed ? 1 : 0));
        }
    }
}