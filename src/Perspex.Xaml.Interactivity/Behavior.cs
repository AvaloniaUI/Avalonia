// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;

namespace Perspex.Xaml.Interactivity
{
    /// <summary>
    /// A base class for behaviors, implementing the basic plumbing of IBehavior
    /// </summary>
    public abstract class Behavior : PerspexObject, IBehavior
    {
        /// <summary>
        /// Gets the <see cref="PerspexObject"/> to which the behavior is attached.
        /// </summary>
        public PerspexObject AssociatedObject { get; private set; }

        /// <summary>
        /// Attaches the behavior to the specified <see cref="PerspexObject"/>.
        /// </summary>
        /// <param name="associatedObject">The <see cref="PerspexObject"/> to which to attach.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="associatedObject"/> is null.</exception>
        public void Attach(PerspexObject associatedObject)
        {
            if (associatedObject == this.AssociatedObject)
            {
                return;
            }

            if (this.AssociatedObject != null)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    "An instance of a behavior cannot be attached to more than one object at a time.",
                    associatedObject,
                    this.AssociatedObject));
            }

            Debug.Assert(associatedObject != null, "Cannot attach the behavior to a null object.");

            if (associatedObject == null) throw new ArgumentNullException(nameof(associatedObject));

            AssociatedObject = associatedObject;
            OnAttached();
        }

        /// <summary>
        /// Detaches the behaviors from the <see cref="Behavior.AssociatedObject"/>.
        /// </summary>
        public void Detach()
        {
            OnDetaching();
            AssociatedObject = null;
        }

        /// <summary>
        /// Called after the behavior is attached to the <see cref="Behavior.AssociatedObject"/>.
        /// </summary>
        /// <remarks>
        /// Override this to hook up functionality to the <see cref="Behavior.AssociatedObject"/>
        /// </remarks>
        protected virtual void OnAttached()
        {
        }

        /// <summary>
        /// Called when the behavior is being detached from its <see cref="Behavior.AssociatedObject"/>.
        /// </summary>
        /// <remarks>
        /// Override this to unhook functionality from the <see cref="Behavior.AssociatedObject"/>
        /// </remarks>
        protected virtual void OnDetaching()
        {
        }
    }
}
