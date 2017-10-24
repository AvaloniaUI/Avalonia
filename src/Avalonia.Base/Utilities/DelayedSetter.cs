using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Text;

namespace Avalonia.Utilities
{
    /// <summary>
    /// A utility class to enable deferring assignment until after property-changed notifications are sent.
    /// </summary>
    /// <typeparam name="TProperty">The type of the object that represents the property.</typeparam>
    /// <typeparam name="TValue">The type of value with which to track the delayed assignment.</typeparam>
    class DelayedSetter<TProperty, TValue>
    {
        /// <summary>
        /// Information on current setting/notification status of a property.
        /// </summary>
        private class SettingStatus
        {
            public bool Notifying { get; set; }

            private Queue<TValue> pendingValues;
            
            public Queue<TValue> PendingValues
            {
                get
                {
                    return pendingValues ?? (pendingValues = new Queue<TValue>());
                }
            }
        }

        private readonly Dictionary<TProperty, SettingStatus> setRecords = new Dictionary<TProperty, SettingStatus>();

        /// <summary>
        /// Mark the property as currently notifying.
        /// </summary>
        /// <param name="property">The property to mark as notifying.</param>
        /// <returns>Returns a disposable that when disposed, marks the property as done notifying.</returns>
        internal IDisposable MarkNotifying(TProperty property)
        {
            Contract.Requires<InvalidOperationException>(!IsNotifying(property));

            if (!setRecords.ContainsKey(property))
            {
                setRecords[property] = new SettingStatus();
            }
            setRecords[property].Notifying = true;

            return Disposable.Create(() => setRecords[property].Notifying = false);
        }

        /// <summary>
        /// Check if the property is currently notifying listeners.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>If the property is currently notifying listeners.</returns>
        internal bool IsNotifying(TProperty property)
            => setRecords.TryGetValue(property, out var value) && value.Notifying;

        /// <summary>
        /// Add a pending assignment for the property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value to assign.</param>
        internal void AddPendingSet(TProperty property, TValue value)
        {
            Contract.Requires<InvalidOperationException>(IsNotifying(property));
            if (!setRecords.ContainsKey(property))
            {
                setRecords[property] = new SettingStatus();
            }
            setRecords[property].PendingValues.Enqueue(value);
        }

        /// <summary>
        /// Checks if there are any pending assignments for the property.
        /// </summary>
        /// <param name="property">The property to check.</param>
        /// <returns>If the property has any pending assignments.</returns>
        internal bool HasPendingSet(TProperty property)
        {
            return setRecords.ContainsKey(property) && setRecords[property].PendingValues.Count != 0;
        }

        /// <summary>
        /// Gets the first pending assignment for the property.
        /// </summary>
        /// <param name="property">The property to check.</param>
        /// <returns>The first pending assignment for the property.</returns>
        internal TValue GetFirstPendingSet(TProperty property)
        {
            return setRecords[property].PendingValues.Dequeue();
        }

        /// <summary>
        /// Set the property and notify listeners while ensuring we don't get into a stack overflow as happens with #855 and #824
        /// </summary>
        /// <param name="property">The property to set.</param>
        /// <param name="setterCallback">
        /// A callback that actually sets the property.
        /// The first parameter is the value to set, and the second is a wrapper that takes a callback that sends the property-changed notification.
        /// </param>
        /// <param name="value">The value to try to set.</param>
        /// <param name="pendingSetCondition">A predicate to filter what possible values should be added as pending sets (i.e. only values not equal to the current value).</param>
        public void SetAndNotify(TProperty property, Action<TValue, Action<Action>> setterCallback, TValue value, Predicate<TValue> pendingSetCondition)
        {
            Contract.Requires<ArgumentNullException>(setterCallback != null);
            if (!IsNotifying(property))
            {
                setterCallback(value, notification =>
                {
                    using (MarkNotifying(property))
                    {
                        notification();
                    }
                });
                while (HasPendingSet(property))
                {
                    setterCallback(GetFirstPendingSet(property), notification =>
                    {
                        using (MarkNotifying(property))
                        {
                            notification();
                        }
                    });
                }
            }
            else if(pendingSetCondition?.Invoke(value) ?? true)
            {
                AddPendingSet(property, value);
            }
        }
    }
}
