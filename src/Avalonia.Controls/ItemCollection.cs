using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Collections;

namespace Avalonia.Controls
{
    public class ItemCollection : ItemsSourceView, IList
    {
// Suppress "Avoid zero-length array allocations": This is a sentinel value and must be unique.
#pragma warning disable CA1825
        private static readonly object?[] s_uninitialized = new object?[0];
#pragma warning restore CA1825

        private Mode _mode;

        internal ItemCollection()
            : base(s_uninitialized)
        {
        }

        public new object? this[int index]
        {
            get => base[index];
            set => WritableSource[index] = value;
        }

        public bool IsReadOnly => _mode == Mode.ItemsSource;

        internal event EventHandler? SourceChanged;

        public int Add(object? value) => WritableSource.Add(value);
        public void Clear() => WritableSource.Clear();
        public void Insert(int index, object? value) => WritableSource.Insert(index, value);
        public void RemoveAt(int index) => WritableSource.RemoveAt(index);

        public bool Remove(object? value)
        {
            var c = Count;
            WritableSource.Remove(value);
            return Count < c;
        }

        int IList.Add(object? value) => Add(value);
        void IList.Clear() => Clear();
        void IList.Insert(int index, object? value) => Insert(index, value);
        void IList.RemoveAt(int index) => RemoveAt(index);

        private IList WritableSource
        {
            get
            {
                if (IsReadOnly)
                    ThrowIsItemsSource();
                if (Source == s_uninitialized)
                    SetSource(CreateDefaultCollection());
                return Source;
            }
        }

        internal IList? GetItemsPropertyValue()
        {
            if (_mode == Mode.ObsoleteItemsSetter)
                return Source == s_uninitialized ? null : Source;
            return this;
        }

        internal void SetItems(IList? items)
        {
            _mode = Mode.ObsoleteItemsSetter;
            SetSource(items ?? s_uninitialized);
        }

        internal void SetItemsSource(IEnumerable? value)
        {
            _mode = value is not null ? Mode.ItemsSource : Mode.Items;
            SetSource(value ?? CreateDefaultCollection());
        }

        private new void SetSource(IEnumerable source)
        {
            var oldSource = Source;

            base.SetSource(source);

            if (oldSource.Count > 0)
                RaiseCollectionChanged(new(NotifyCollectionChangedAction.Remove, oldSource, 0));
            if (Source.Count > 0)
                RaiseCollectionChanged(new(NotifyCollectionChangedAction.Add, Source, 0));
            SourceChanged?.Invoke(this, EventArgs.Empty);
        }

        private static AvaloniaList<object?> CreateDefaultCollection()
        {
            return new() { ResetBehavior = ResetBehavior.Remove };
        }

        [DoesNotReturn]
        private static void ThrowIsItemsSource()
        {
            throw new InvalidOperationException(
                "Operation is not valid while ItemsSource is in use." +
                "Access and modify elements with ItemsControl.ItemsSource instead.");
        }

        private enum Mode
        {
            Items,
            ItemsSource,
            ObsoleteItemsSetter,
        }
    }
}
