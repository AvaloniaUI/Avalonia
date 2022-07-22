using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data;
using Avalonia.Utilities;

namespace Avalonia.PropertyStore
{
    internal abstract class ValueFrame
    {
        private readonly List<IValueEntry> _entries = new();
        private AvaloniaPropertyDictionary<IValueEntry> _index;

        public int EntryCount => _entries.Count;
        public abstract bool IsActive { get; }
        public ValueStore? Owner { get; private set; }
        public BindingPriority Priority { get; protected set; }

        public bool Contains(AvaloniaProperty property) => _index.ContainsKey(property);

        public IValueEntry GetEntry(int index) => _entries[index];

        public void SetOwner(ValueStore? owner) => Owner = owner;

        public bool TryGetEntry(AvaloniaProperty property, [NotNullWhen(true)] out IValueEntry? entry)
        {
            return _index.TryGetValue(property, out entry);
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
            _entries.Add(value);
            _index.Add(value.Property, value);
        }

        protected void Remove(AvaloniaProperty property)
        {
            Debug.Assert(!property.IsDirect);

            var count = _entries.Count;

            for (var i = 0; i < count; ++i)
            {
                if (_entries[i].Property == property)
                {
                    _entries.RemoveAt(i);
                    break;
                }
            }

            _index.Remove(property);
        }
    }
}
