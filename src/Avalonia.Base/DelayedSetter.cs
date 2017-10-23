using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia
{
    class DelayedSetter<T>
    {
        private class SettingStatus
        {
            public bool Notifying { get; set; }

            private Queue<object> pendingValues;
            
            public Queue<object> PendingValues
            {
                get
                {
                    return pendingValues ?? (pendingValues = new Queue<object>());
                }
            }
        }

        private readonly Dictionary<T, SettingStatus> setRecords = new Dictionary<T, SettingStatus>();

        public void SetNotifying(T property, bool notifying)
        {
            if (!setRecords.ContainsKey(property))
            {
                setRecords[property] = new SettingStatus();
            }
            setRecords[property].Notifying = notifying;
        }

        public bool IsNotifying(T property) => setRecords.TryGetValue(property, out var value) && value.Notifying;

        public void RecordPendingSet(T property, object value)
        {
            if (!setRecords.ContainsKey(property))
            {
                setRecords[property] = new SettingStatus();
            }
            setRecords[property].PendingValues.Enqueue(value);
        }

        public bool HasPendingSet(T property)
        {
            return setRecords.ContainsKey(property) && setRecords[property].PendingValues.Count != 0;
        }

        public object GetFirstPendingSet(T property)
        {
            return setRecords[property].PendingValues.Dequeue();
        }
    }
}
