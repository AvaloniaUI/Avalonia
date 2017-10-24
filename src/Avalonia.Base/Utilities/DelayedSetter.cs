using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Text;

namespace Avalonia.Utilities
{
    public class DelayedSetter<T, TValue>
    {
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

        private readonly Dictionary<T, SettingStatus> setRecords = new Dictionary<T, SettingStatus>();

        public IDisposable MarkNotifying(T property)
        {
            Contract.Requires<InvalidOperationException>(!IsNotifying(property));

            if (!setRecords.ContainsKey(property))
            {
                setRecords[property] = new SettingStatus();
            }
            setRecords[property].Notifying = true;

            return Disposable.Create(() => setRecords[property].Notifying = false);
        }

        public bool IsNotifying(T property) => setRecords.TryGetValue(property, out var value) && value.Notifying;

        public void AddPendingSet(T property, TValue value)
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

        public TValue GetFirstPendingSet(T property)
        {
            return setRecords[property].PendingValues.Dequeue();
        }
    }
}
