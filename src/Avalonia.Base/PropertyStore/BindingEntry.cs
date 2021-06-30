using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Data;

#nullable enable

namespace Avalonia.PropertyStore
{
    internal class BindingEntry : BindingValueEntryBase,
        IValueFrame,
        IList<IValueEntry>,
        IDisposable
    {
        private ValueStore? _owner;

        public BindingEntry(
            AvaloniaProperty property,
            IObservable<object?> source,
            BindingPriority priority)
            : base(property, source)
        {
            Priority = priority;
        }

        public BindingPriority Priority { get; }
        public IList<IValueEntry> Values => this;
        int ICollection<IValueEntry>.Count => 1;
        bool ICollection<IValueEntry>.IsReadOnly => true;
        
        IValueEntry IList<IValueEntry>.this[int index] 
        { 
            get => this;
            set => throw new NotImplementedException(); 
        }

        public void SetOwner(ValueStore? owner) => _owner = owner;
        protected override void ValueChanged(object? oldValue) => _owner!.ValueChanged(this, this, oldValue);
        protected override void Completed(object? oldValue) => _owner!.RemoveBindingEntry(this, oldValue);

        int IList<IValueEntry>.IndexOf(IValueEntry item) => throw new NotImplementedException();
        void IList<IValueEntry>.Insert(int index, IValueEntry item) => throw new NotImplementedException();
        void IList<IValueEntry>.RemoveAt(int index) => throw new NotImplementedException();
        void ICollection<IValueEntry>.Add(IValueEntry item) => throw new NotImplementedException();
        void ICollection<IValueEntry>.Clear() => throw new NotImplementedException();
        bool ICollection<IValueEntry>.Contains(IValueEntry item) => throw new NotImplementedException();
        void ICollection<IValueEntry>.CopyTo(IValueEntry[] array, int arrayIndex) => throw new NotImplementedException();
        bool ICollection<IValueEntry>.Remove(IValueEntry item) => throw new NotImplementedException();
        IEnumerator<IValueEntry> IEnumerable<IValueEntry>.GetEnumerator() => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        protected override AvaloniaObject GetOwner() => _owner!.Owner;
    }
}
