using System;
using Avalonia.Data;

namespace Avalonia.PropertyStore
{
    /// <summary>
    /// Holds values in a <see cref="ValueStore"/> set by one of the SetValue or AddBinding
    /// overloads with non-LocalValue priority.
    /// </summary>
    internal class ImmediateValueFrame : ValueFrame
    {
        public ImmediateValueFrame(BindingPriority priority)
        {
            Priority = priority;
        }

        public override bool IsActive => true;
        public override BindingPriority Priority { get; }

        public BindingEntry<T> AddBinding<T>(
            StyledPropertyBase<T> property,
            IObservable<BindingValue<T>> source)
        {
            var e = new BindingEntry<T>(this, property, source);
            Add(e);
            return e;
        }

        public BindingEntry<T> AddBinding<T>(
            StyledPropertyBase<T> property,
            IObservable<T> source)
        {
            var e = new BindingEntry<T>(this, property, source);
            Add(e);
            return e;
        }

        public UntypedBindingEntry<T> AddBinding<T>(
            StyledPropertyBase<T> property,
            IObservable<object?> source)
        {
            var e = new UntypedBindingEntry<T>(this, property, source);
            Add(e);
            return e;
        }

        public IDisposable AddValue<T>(StyledPropertyBase<T> property, T value)
        {
            var e = new ImmediateValueEntry<T>(this, property, value);
            Add(e);
            return e;
        }

        public void OnEntryDisposed(IValueEntry value)
        {
            Remove(value.Property);
            Owner?.OnValueEntryRemoved(this, value.Property);
        }
    }
}
