using System;
using System.Collections.Generic;

namespace Avalonia.Utilities
{
    /// <summary>
    /// A utility class to enable deferring assignment until after property-changed notifications are sent.
    /// Used to fix #855.
    /// </summary>
    /// <typeparam name="TSetRecord">The type of value with which to track the delayed assignment.</typeparam>
    class DeferredSetter<TSetRecord>
    {
        private struct NotifyDisposable : IDisposable
        {
            private readonly SettingStatus status;

            internal NotifyDisposable(SettingStatus status)
            {
                this.status = status;
                status.Notifying = true;
            }

            public void Dispose()
            {
                status.Notifying = false;
            }
        }

        /// <summary>
        /// Information on current setting/notification status of a property.
        /// </summary>
        private class SettingStatus
        {
            public bool Notifying { get; set; }

            private SingleOrQueue<TSetRecord> pendingValues;
            
            public SingleOrQueue<TSetRecord> PendingValues
            {
                get
                {
                    return pendingValues ?? (pendingValues = new SingleOrQueue<TSetRecord>());
                }
            }
        }

        private Dictionary<AvaloniaProperty, SettingStatus> _setRecords;
        private Dictionary<AvaloniaProperty, SettingStatus> SetRecords
            => _setRecords ?? (_setRecords = new Dictionary<AvaloniaProperty, SettingStatus>());

        private SettingStatus GetOrCreateStatus(AvaloniaProperty property)
        {
            if (!SetRecords.TryGetValue(property, out var status))
            {
                status = new SettingStatus();
                SetRecords.Add(property, status);
            }

            return status;
        }

        /// <summary>
        /// Mark the property as currently notifying.
        /// </summary>
        /// <param name="property">The property to mark as notifying.</param>
        /// <returns>Returns a disposable that when disposed, marks the property as done notifying.</returns>
        private NotifyDisposable MarkNotifying(AvaloniaProperty property)
        {
            Contract.Requires<InvalidOperationException>(!IsNotifying(property));

            SettingStatus status = GetOrCreateStatus(property);

            return new NotifyDisposable(status);
        }

        /// <summary>
        /// Check if the property is currently notifying listeners.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>If the property is currently notifying listeners.</returns>
        private bool IsNotifying(AvaloniaProperty property)
            => SetRecords.TryGetValue(property, out var value) && value.Notifying;

        /// <summary>
        /// Add a pending assignment for the property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value to assign.</param>
        private void AddPendingSet(AvaloniaProperty property, TSetRecord value)
        {
            Contract.Requires<InvalidOperationException>(IsNotifying(property));

            GetOrCreateStatus(property).PendingValues.Enqueue(value);
        }

        /// <summary>
        /// Checks if there are any pending assignments for the property.
        /// </summary>
        /// <param name="property">The property to check.</param>
        /// <returns>If the property has any pending assignments.</returns>
        private bool HasPendingSet(AvaloniaProperty property)
        {
            return SetRecords.TryGetValue(property, out var status) && !status.PendingValues.Empty;
        }

        /// <summary>
        /// Gets the first pending assignment for the property.
        /// </summary>
        /// <param name="property">The property to check.</param>
        /// <returns>The first pending assignment for the property.</returns>
        private TSetRecord GetFirstPendingSet(AvaloniaProperty property)
        {
            return GetOrCreateStatus(property).PendingValues.Dequeue();
        }

        public delegate bool SetterDelegate<TValue>(TSetRecord record, ref TValue backing, Action<Action> notifyCallback);

        /// <summary>
        /// Set the property and notify listeners while ensuring we don't get into a stack overflow as happens with #855 and #824
        /// </summary>
        /// <param name="property">The property to set.</param>
        /// <param name="backing">The backing field for the property</param>
        /// <param name="setterCallback">
        /// A callback that actually sets the property.
        /// The first parameter is the value to set, and the second is a wrapper that takes a callback that sends the property-changed notification.
        /// </param>
        /// <param name="value">The value to try to set.</param>
        public bool SetAndNotify<TValue>(
            AvaloniaProperty property,
            ref TValue backing,
            SetterDelegate<TValue> setterCallback,
            TSetRecord value)
        {
            Contract.Requires<ArgumentNullException>(setterCallback != null);
            if (!IsNotifying(property))
            {
                bool updated = false;
                if (!object.Equals(value, backing))
                {
                    updated = setterCallback(value, ref backing, notification =>
                    {
                        using (MarkNotifying(property))
                        {
                            notification();
                        }
                    });
                }
                while (HasPendingSet(property))
                {
                    updated |= setterCallback(GetFirstPendingSet(property), ref backing, notification =>
                    {
                        using (MarkNotifying(property))
                        {
                            notification();
                        }
                    });
                }

                return updated;
            }
            else if(!object.Equals(value, backing))
            {
                AddPendingSet(property, value);
            }
            return false;
        }
    }
}
