using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data;

#nullable enable

namespace Avalonia.PropertyStore
{
    internal abstract class ValueFrameBase : IValueFrame
    {
        private readonly SortedList<int, IValueEntry> _values = new();

        public abstract bool IsActive { get; }
        public abstract BindingPriority Priority { get; }
        public IList<IValueEntry> Values => _values.Values;

        public virtual void SetOwner(ValueStore? owner)
        {
        }

        public bool TryGet(AvaloniaProperty property, [NotNullWhen(true)] out IValueEntry? value)
        {
            return _values.TryGetValue(property.Id, out value);
        }

        protected void Add(IValueEntry value)
        {
            Debug.Assert(!value.Property.IsDirect);
            _values[value.Property.Id] = value;
        }

        protected bool Remove(AvaloniaProperty property) => _values.Remove(property.Id);
        protected void Set(IValueEntry value) => _values[value.Property.Id] = value;
    }
}
