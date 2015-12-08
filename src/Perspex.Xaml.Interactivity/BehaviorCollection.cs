// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Perspex.Xaml.Interactivity
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using Perspex.Collections;

    /// <summary>
    /// Represents a collection of IBehaviors with a shared <see cref="BehaviorCollection.AssociatedObject"/>.
    /// </summary>
    public sealed class BehaviorCollection : PerspexList<PerspexObject>
    {
        // After a VectorChanged event we need to compare the current state of the collection
        // with the old collection so that we can call Detach on all removed items.
        private readonly List<IBehavior> oldCollection = new List<IBehavior>();

        /// <summary>
        /// Initializes a new instance of the <see cref="BehaviorCollection"/> class.
        /// </summary>
        public BehaviorCollection()
        {
            this.CollectionChanged += BehaviorCollection_CollectionChanged;
        }

        /// <summary>
        /// Gets the <see cref="PerspexObject"/> to which the <see cref="BehaviorCollection"/> is attached.
        /// </summary>
        public PerspexObject AssociatedObject
        {
            get;
            private set;
        }

        /// <summary>
        /// Attaches the collection of behaviors to the specified <see cref="PerspexObject"/>.
        /// </summary>
        /// <param name="associatedObject">The <see cref="PerspexObject"/> to which to attach.</param>
        /// <exception cref="InvalidOperationException">The <see cref="BehaviorCollection"/> is already attached to a different <see cref="PerspexObject"/>.</exception>
        public void Attach(PerspexObject associatedObject)
        {
            if (associatedObject == this.AssociatedObject)
            {
                return;
            }

            // TODO: Check for design mode
            //if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            //{
            //    return;
            //}

            if (this.AssociatedObject != null)
            {
                throw new InvalidOperationException("An instance of a behavior cannot be attached to more than one object at a time.");
            }

            Debug.Assert(associatedObject != null, "The previous checks should keep us from ever setting null here.");
            this.AssociatedObject = associatedObject;

            foreach (PerspexObject item in this)
            {
                IBehavior behavior = (IBehavior)item;
                behavior.Attach(this.AssociatedObject);
            }
        }

        /// <summary>
        /// Detaches the collection of behaviors from the <see cref="Microsoft.Xaml.Interactivity.BehaviorCollection.AssociatedObject"/>.
        /// </summary>
        public void Detach()
        {
            foreach (PerspexObject item in this)
            {
                IBehavior behaviorItem = (IBehavior)item;
                if (behaviorItem.AssociatedObject != null)
                {
                    behaviorItem.Detach();
                }
            }

            this.AssociatedObject = null;
            this.oldCollection.Clear();
        }

        private void BehaviorCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs eventArgs)
        {
            if (eventArgs.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (IBehavior behavior in this.oldCollection)
                {
                    if (behavior.AssociatedObject != null)
                    {
                        behavior.Detach();
                    }
                }

                this.oldCollection.Clear();

                foreach (PerspexObject newItem in this)
                {
                    this.oldCollection.Add(this.VerifiedAttach(newItem));
                }

#if DEBUG
                this.VerifyOldCollectionIntegrity();
#endif
                return;
            }

            switch (eventArgs.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        int eventIndex = eventArgs.NewStartingIndex;
                        PerspexObject changedItem = (PerspexObject)eventArgs.NewItems[0];
                        this.oldCollection.Insert(eventIndex, this.VerifiedAttach(changedItem));
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    {
                        int eventIndex = eventArgs.OldStartingIndex;
                        eventIndex = eventIndex == -1 ? 0 : eventIndex;

                        PerspexObject changedItem = (PerspexObject)eventArgs.NewItems[0];

                        IBehavior oldItem = this.oldCollection[eventIndex];
                        if (oldItem.AssociatedObject != null)
                        {
                            oldItem.Detach();
                        }

                        this.oldCollection[eventIndex] = this.VerifiedAttach(changedItem);
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    {
                        int eventIndex = eventArgs.OldStartingIndex;
                        PerspexObject changedItem = (PerspexObject)eventArgs.OldItems[0];

                        IBehavior oldItem = this.oldCollection[eventIndex];
                        if (oldItem.AssociatedObject != null)
                        {
                            oldItem.Detach();
                        }

                        this.oldCollection.RemoveAt(eventIndex);
                    }
                    break;

                default:
                    Debug.Assert(false, "Unsupported collection operation attempted.");
                    break;
            }

#if DEBUG
            this.VerifyOldCollectionIntegrity();
#endif
        }

        private IBehavior VerifiedAttach(PerspexObject item)
        {
            IBehavior behavior = item as IBehavior;
            if (behavior == null)
            {
                throw new InvalidOperationException("Only IBehavior types are supported in a BehaviorCollection.");
            }

            if (this.oldCollection.Contains(behavior))
            {
                throw new InvalidOperationException("Cannot add an instance of a behavior to a BehaviorCollection more than once.");
            }

            if (this.AssociatedObject != null)
            {
                behavior.Attach(this.AssociatedObject);
            }

            return behavior;
        }

        [Conditional("DEBUG")]
        private void VerifyOldCollectionIntegrity()
        {
            bool isValid = (this.Count == this.oldCollection.Count);
            if (isValid)
            {
                for (int i = 0; i < this.Count; i++)
                {
                    if (this[i] != this.oldCollection[i])
                    {
                        isValid = false;
                        break;
                    }
                }
            }

            Debug.Assert(isValid, "Referential integrity of the collection has been compromised.");
        }
    }
}
