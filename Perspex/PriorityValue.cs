// -----------------------------------------------------------------------
// <copyright file="PriorityValue.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Disposables;
    using System.Reactive.Subjects;
    using System.Reflection;

    /// <summary>
    /// Maintains a list of prioritised bindings together with a current value.
    /// </summary>
    /// <remarks>
    /// Bindings, in the form of <see cref="IObservable<object>"/>s are added to the object using
    /// the <see cref="Add"/> method. With the observable is passed a priority, where lower values
    /// represent higher priorites. The current <see cref="Value"/> is selected from the highest
    /// priority binding that doesn't return <see cref="PerspexProperty.UnsetValue"/>. Where there
    /// are multiple bindings registered with the same priority, the most recently added binding
    /// has a higher priority. Each time the value changes to a distinct new value, the
    /// <see cref="Changed"/> observable is fired with the old and new values.
    /// </remarks>
    public class PriorityValue
    {
        /// <summary>
        /// The name of the property.
        /// </summary>
        private string name;

        /// <summary>
        /// The value type.
        /// </summary>
        private Type valueType;

        /// <summary>
        /// The currently registered binding entries.
        /// </summary>
        private LinkedList<BindingEntry> bindings = new LinkedList<BindingEntry>();

        /// <summary>
        /// The changed observable.
        /// </summary>
        private Subject<Tuple<object, object>> changed = new Subject<Tuple<object, object>>();

        /// <summary>
        /// The current value.
        /// </summary>
        private object value;

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityValue"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="valueType">The value type.</param>
        public PriorityValue(string name, Type valueType)
        {
            this.name = name;
            this.valueType = valueType;
            this.value = PerspexProperty.UnsetValue;
            this.ValuePriority = int.MaxValue;
        }

        /// <summary>
        /// Fired whenever the current <see cref="Value"/> changes to a new distinct value.
        /// </summary>
        public IObservable<Tuple<object, object>> Changed
        {
            get { return this.changed; }
        }

        /// <summary>
        /// Gets the current value.
        /// </summary>
        public object Value
        {
            get { return this.value; }
        }

        /// <summary>
        /// Gets the priority of the binding that is currently active.
        /// </summary>
        public int ValuePriority
        {
            get;
            private set;
        }

        /// <summary>
        /// Checks whether a value is valid for a type.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="propertyType"></param>
        /// <returns></returns>
        public static bool IsValidValue(object value, Type propertyType)
        {
            TypeInfo type = propertyType.GetTypeInfo();

            if (value == PerspexProperty.UnsetValue)
            {
                return true;
            }
            else if (value == null)
            {
                if (type.IsValueType && 
                    (!type.IsGenericType || !(type.GetGenericTypeDefinition() == typeof(Nullable<>))))
                {
                    return false;
                }
            }
            else
            {
                if (!type.IsAssignableFrom(value.GetType().GetTypeInfo()))
                {
                    return false;
                }
            }

            return true;
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
                this.Remove(entry);
            });
        }

        /// <summary>
        /// Adds a new binding, replacing all those of the same priority.
        /// </summary>
        /// <param name="binding">The binding.</param>
        /// <param name="priority">The binding priority.</param>
        /// <returns>
        /// A disposable that will remove the binding.
        /// </returns>
        public IDisposable Replace(IObservable<object> binding, int priority)
        {
            BindingEntry entry = new BindingEntry();
            LinkedListNode<BindingEntry> insert = this.bindings.First;

            while (insert != null && insert.Value.Priority < priority)
            {
                insert = insert.Next;
            }

            while (insert != null && insert.Value.Priority == priority)
            {
                LinkedListNode<BindingEntry> next = insert.Next;
                insert.Value.Dispose();
                this.bindings.Remove(insert);
                insert = next;
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
                this.Remove(entry);
            });
        }

        /// <summary>
        /// Removes all bindings with the specified priority.
        /// </summary>
        /// <param name="priority">The priority.</param>
        public void Clear(int priority)
        {
            LinkedListNode<BindingEntry> item = this.bindings.First;
            bool removed = false;

            while (item != null && item.Value.Priority <= priority)
            {
                LinkedListNode<BindingEntry> next = item.Next;

                if (item.Value.Priority == priority)
                {
                    item.Value.Dispose();
                    this.bindings.Remove(item);
                    removed = true;
                }

                item = next;
            }

            if (removed && priority <= this.ValuePriority)
            {
                this.UpdateValue();
            }
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
        /// Called when an binding's value changes.
        /// </summary>
        /// <param name="changed">The changed entry.</param>
        private void EntryChanged(BindingEntry changed)
        {
            if (changed.Priority <= this.ValuePriority)
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
            this.Remove(entry);
        }

        /// <summary>
        /// Sets the current value and notifies all observers.
        /// </summary>
        /// <param name="value">The new value.</param>
        /// <param name="priority">The priority of the binding which produced the value.</param>
        private void SetValue(object value, int priority)
        {
            if (!IsValidValue(value, this.valueType))
            {
                throw new InvalidOperationException(string.Format(
                    "Invalid value for Property '{0}': {1} ({2})",
                    this.name,
                    value,
                    value.GetType().FullName));
            }

            object old = this.value;

            this.ValuePriority = priority;

            if (!EqualityComparer<object>.Default.Equals(old, value))
            {
                this.value = value;
                this.changed.OnNext(Tuple.Create(old, value));
            }
        }

        /// <summary>
        /// Removes the specified binding entry and updates the current value.
        /// </summary>
        /// <param name="entry">The binding entry to remove.</param>
        private void Remove(BindingEntry entry)
        {
            entry.Dispose();
            this.bindings.Remove(entry);
            this.UpdateValue();
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

            this.SetValue(PerspexProperty.UnsetValue, int.MaxValue);
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
            /// Gets a description of the binding.
            /// </summary>
            public string Description
            {
                get;
                private set;
            }

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
