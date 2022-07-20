using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data;
using Avalonia.Utilities;

namespace Avalonia.PropertyStore
{
    internal abstract class ValueFrame
    {
        private readonly AvaloniaPropertyValueStore<IValueEntry> _entries = new();

        public int EntryCount => _entries.Count;
        public abstract bool IsActive { get; }
        public ValueStore? Owner { get; private set; }
        public abstract BindingPriority Priority { get; }

        public bool Contains(AvaloniaProperty property) => _entries.Contains(property);

        public IValueEntry GetEntry(int index) => _entries[index];

        public void SetOwner(ValueStore? owner) => Owner = owner;

        public bool TryGet(AvaloniaProperty property, [NotNullWhen(true)] out IValueEntry? value)
        {
            return _entries.TryGetValue(property, out value);
        }

        public bool TryGetEntry(AvaloniaProperty property, [NotNullWhen(true)] out IValueEntry? entry)
        {
            return _entries.TryGetValue(property, out entry);
        }

        public void OnBindingCompleted(IValueEntry binding)
        {
            Remove(binding.Property);
            Owner?.OnBindingCompleted(binding.Property, this);
        }

        public virtual void Dispose()
        {
            for (var i = 0; i < _entries.Count; ++i)
                _entries[i].Unsubscribe();
        }

        protected void Add(IValueEntry value)
        {
            Debug.Assert(!value.Property.IsDirect);
            _entries.AddValue(value.Property, value);
        }

        protected void Remove(AvaloniaProperty property) => _entries.Remove(property);
        protected void Set(IValueEntry value) => _entries.SetValue(value.Property, value);
    }
}
