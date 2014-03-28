// -----------------------------------------------------------------------
// <copyright file="PriorityValue.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Disposables;

    /// <summary>
    /// Maintains a list of prioritised bindings together with a current value.
    /// </summary>
    public class PriorityValue : IObservable<Tuple<object, object>>
    {
        /// <summary>
        /// The currently registered binding entries.
        /// </summary>
        private LinkedList<BindingEntry> bindings = new LinkedList<BindingEntry>();

        /// <summary>
        /// The current observers.
        /// </summary>
        private List<IObserver<Tuple<object, object>>> observers = 
            new List<IObserver<Tuple<object, object>>>();

        /// <summary>
        /// The current value.
        /// </summary>
        private object value;

        /// <summary>
        /// The priority of the binding that is currently active.
        /// </summary>
        private int valuePriority = int.MaxValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityValue"/> class.
        /// </summary>
        public PriorityValue()
        {
            this.value = PerspexProperty.UnsetValue;
        }

        /// <summary>
        /// Gets the currently active bindings on this object.
        /// </summary>
        /// <returns>An enumerable collection of bindings.</returns>
        public IEnumerable<BindingEntry> GetBindings()
        {
            return this.bindings;
        }

        /// <summary>
        /// Gets the current value.
        /// </summary>
        public object Value
        {
            get { return this.value; }
        }

        /// <summary>
        /// Adds a new binding.
        /// </summary>
        /// <param name="binding">The binding.</param>
        /// <param name="priority">The binding priority.</param>
        /// <returns>
        /// A disposable that will remove the binding.
        /// </returns>
        public IDisposable Add(IObservable<object> binding, int priority)
        {
            BindingEntry entry = new BindingEntry();
            LinkedListNode<BindingEntry> insert = this.bindings.First;

            while (insert != null && insert.Value.Priority < priority)
            {
                insert = insert.Next;
            }

            if (insert == null)
            {
                this.bindings.AddLast(entry);
            }
            else
            {
                this.bindings.AddBefore(insert, entry);
            }

            entry.Start(binding, priority, this.EntryChanged, this.EntryCompleted);

            return Disposable.Create(() =>
            {
                entry.Dispose();
                this.bindings.Remove(entry);
                this.UpdateValue();
            });
        }

        /// <summary>
        /// Notifies the provider that an observer is to receive notifications.
        /// </summary>
        /// <param name="observer">The object that is to receive notifications.</param>
        /// <returns>
        /// A reference to an interface that allows observers to stop receiving notifications 
        /// before the provider has finished sending them.
        /// </returns>
        public IDisposable Subscribe(IObserver<Tuple<object, object>> observer)
        {
            this.observers.Add(observer);
            return Disposable.Create(() => this.observers.Remove(observer));
        }

        /// <summary>
        /// Called when an binding's value changes.
        /// </summary>
        /// <param name="changed">The changed entry.</param>
        private void EntryChanged(BindingEntry changed)
        {
            if (changed.Priority <= this.valuePriority)
            {
                this.UpdateValue();
            }
        }

        /// <summary>
        /// Called when an binding completes.
        /// </summary>
        /// <param name="changed">The completed entry.</param>
        private void EntryCompleted(BindingEntry entry)
        {
            entry.Dispose();
            this.bindings.Remove(entry);
            this.UpdateValue();
        }

        /// <summary>
        /// Notifies all observers of a change in value.
        /// </summary>
        /// <param name="value">The old and new values.</param>
        private void OnNext(Tuple<object, object> value)
        {
            foreach (var observer in this.observers)
            {
                observer.OnNext(value);
            }
        }

        /// <summary>
        /// Sets the current value and notifies all observers.
        /// </summary>
        /// <param name="value">The new value.</param>
        /// <param name="priority">The priority of the binding which produced the value.</param>
        private void SetValue(object value, int priority)
        {
            object old = this.value;

            this.valuePriority = priority;

            if (!EqualityComparer<object>.Default.Equals(old, value))
            {
                this.value = value;
                this.OnNext(Tuple.Create(old, value));
            }
        }

        /// <summary>
        /// Updates the current value.
        /// </summary>
        private void UpdateValue()
        {
            foreach (BindingEntry entry in this.bindings)
            {
                if (entry.Value != PerspexProperty.UnsetValue)
                {
                    this.SetValue(entry.Value, entry.Priority);
                    return;
                }
            }
        }

        /// <summary>
        /// A registered binding.
        /// </summary>
        public class BindingEntry : IDisposable
        {
            /// <summary>
            /// The binding subscription.
            /// </summary>
            private IDisposable subscription;

            /// <summary>
            /// The priority of the binding.
            /// </summary>
            public int Priority
            {
                get;
                private set;
            }

            /// <summary>
            /// The current value of the binding.
            /// </summary>
            public object Value
            {
                get;
                private set;
            }

            /// <summary>
            /// Starts listening to the specified binding.
            /// </summary>
            /// <param name="binding">The binding.</param>
            /// <param name="priority">The binding priority.</param>
            /// <param name="changed">Called when the binding changes.</param>
            /// <param name="completed">Called when the binding completes.</param>
            public void Start(
                IObservable<object> binding,
                int priority,
                Action<BindingEntry> changed,
                Action<BindingEntry> completed)
            {
                Contract.Requires<ArgumentNullException>(binding != null);
                Contract.Requires<ArgumentNullException>(changed != null);
                Contract.Requires<ArgumentNullException>(completed != null);

                if (this.subscription != null)
                {
                    throw new Exception("PriorityValue.Entry.Start() called more than once.");
                }

                this.Priority = priority;
                this.Value = PerspexProperty.UnsetValue;

                if (binding is IObservableDescription)
                {
                    this.Description = ((IObservableDescription)binding).Description;
                }

                this.subscription = binding.Subscribe(
                    value =>
                    {
                        this.Value = value;
                        changed(this);
                    },
                    () => completed(this));
            }

            /// <summary>
            /// Gets a description of the binding.
            /// </summary>
            public string Description
            {
                get;
                private set;
            }

            /// <summary>
            /// Ends the binding subscription.
            /// </summary>
            public void Dispose()
            {
                if (this.subscription != null)
                {
                    this.subscription.Dispose();
                }
            }
        }
    }
}
