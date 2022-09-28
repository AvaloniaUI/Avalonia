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
        private ValueStore? _owner;
        private bool _isShared;

        public int EntryCount => _entries.Count;
        public bool IsActive => GetIsActive(out _);
        public ValueStore? Owner => !_isShared ? _owner : 
            throw new AvaloniaInternalException("Cannot get owner for shared ValueFrame");
        public BindingPriority Priority { get; protected set; }

        public bool Contains(AvaloniaProperty property) => _index.ContainsKey(property);

        public IValueEntry GetEntry(int index) => _entries[index];

        public void SetOwner(ValueStore? owner)
        {
            if (_owner is not null && owner is not null)
                throw new AvaloniaInternalException("ValueFrame already has an owner.");
            if (!_isShared)
                _owner = owner;
        }

        public bool TryGetEntryIfActive(
            AvaloniaProperty property,
            [NotNullWhen(true)] out IValueEntry? entry,
            out bool activeChanged)
        {
            if (_index.TryGetValue(property, out entry) && 
                GetIsActive(out activeChanged))
                return true;
            activeChanged = false;
            return false;
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

        protected abstract bool GetIsActive(out bool hasChanged);

        protected void MakeShared()
        {
            _isShared = true;
            _owner = null;
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
