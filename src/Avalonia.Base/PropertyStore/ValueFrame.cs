using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Utilities;

namespace Avalonia.PropertyStore
{
    internal enum FrameType
    {
        Style,
        TemplatedParentTheme,
        Theme,
    }

    internal abstract class ValueFrame
    {
        private List<IValueEntry>? _entries;
        private AvaloniaPropertyDictionary<IValueEntry> _index;
        private ValueStore? _owner;
        private bool _isShared;

        protected ValueFrame(BindingPriority priority, FrameType type)
        {
            Priority = priority;
            FramePriority = priority.ToFramePriority(type);
        }

        public int EntryCount => _index.Count;
        public bool IsActive() => GetIsActive(out _);
        public ValueStore? Owner => !_isShared ? _owner : 
            throw new AvaloniaInternalException("Cannot get owner for shared ValueFrame");
        public BindingPriority Priority { get; }
        public FramePriority FramePriority { get; }

        public bool Contains(AvaloniaProperty property) => _index.ContainsKey(property);

        public IValueEntry GetEntry(int index) => _entries?[index] ?? _index[0];

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
            if (_index.TryGetValue(property, out entry))
                return GetIsActive(out activeChanged);
            activeChanged = false;
            return false;
        }

        public void OnBindingCompleted(IValueEntry binding)
        {
            var property = binding.Property;
            Remove(property);
            Owner?.OnValueEntryRemoved(this, property);
        }

        public virtual void Dispose()
        {
            for (var i = 0; i < _index.Count; ++i)
                _index[i].Unsubscribe();
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

            if (_entries is null && _index.Count == 1)
            {
                _entries = new();
                _entries.Add(_index[0]);
            }

            _index.Add(value.Property, value);
            _entries?.Add(value);
        }

        protected void Remove(AvaloniaProperty property)
        {
            Debug.Assert(!property.IsDirect);

            if (_entries is not null)
            {
                var count = _entries.Count;

                for (var i = 0; i < count; ++i)
                {
                    if (_entries[i].Property == property)
                    {
                        _entries.RemoveAt(i);
                        break;
                    }
                }
            }

            _index.Remove(property);
        }
    }
}
