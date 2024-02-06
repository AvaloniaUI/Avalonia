using System;
using Avalonia.Data;
using Avalonia.Data.Core;

namespace Avalonia.PropertyStore
{
    /// <summary>
    /// Holds values in a <see cref="ValueStore"/> set by one of the SetValue or AddBinding
    /// overloads with non-LocalValue priority.
    /// </summary>
    internal sealed class ImmediateValueFrame : ValueFrame
    {
        public ImmediateValueFrame(BindingPriority priority)
            : base(priority, FrameType.Style)
        {
        }

        public IValueEntry AddBinding(UntypedBindingExpressionBase source)
        {
            Add(source);
            return source;
        }

        public TypedBindingEntry<T> AddBinding<T>(
            StyledProperty<T> property,
            IObservable<BindingValue<T>> source)
        {
            var e = new TypedBindingEntry<T>(Owner!.Owner, this, property, source);
            Add(e);
            return e;
        }

        public TypedBindingEntry<T> AddBinding<T>(
            StyledProperty<T> property,
            IObservable<T> source)
        {
            var e = new TypedBindingEntry<T>(Owner!.Owner, this, property, source);
            Add(e);
            return e;
        }

        public SourceUntypedBindingEntry<T> AddBinding<T>(
            StyledProperty<T> property,
            IObservable<object?> source)
        {
            var e = new SourceUntypedBindingEntry<T>(Owner!.Owner, this, property, source);
            Add(e);
            return e;
        }

        public ImmediateValueEntry<T> AddValue<T>(StyledProperty<T> property, T value)
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

        protected override bool GetIsActive(out bool hasChanged)
        {
            hasChanged = false;
            return true;
        }
    }
}
