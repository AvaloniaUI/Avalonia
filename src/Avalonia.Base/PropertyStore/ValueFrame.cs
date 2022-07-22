using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data;
using Avalonia.Utilities;

namespace Avalonia.PropertyStore
{
    internal abstract class ValueFrame
    {
        private AvaloniaPropertyDictionary<IValueEntry> _entries = new();

        public int EntryCount => _entries.Count;
        public abstract bool IsActive { get; }
        public ValueStore? Owner { get; private set; }
        public BindingPriority Priority { get; protected set; }

        public bool Contains(AvaloniaProperty property) => _entries.ContainsKey(property);

        public IValueEntry GetEntry(int index) => _entries[index];

        public void SetOwner(ValueStore? owner) => Owner = owner;

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
            _entries.Add(value.Property, value);
        }

        protected void Remove(AvaloniaProperty property) => _entries.Remove(property);
        protected void Set(IValueEntry value) => _entries[value.Property] = value;
    }
}
