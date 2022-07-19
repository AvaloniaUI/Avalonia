using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.PropertyStore
{
    internal class InheritanceFrame : Dictionary<AvaloniaProperty, EffectiveValue>
    {
        public InheritanceFrame(ValueStore owner, InheritanceFrame? parent = null)
        {
            Owner = owner;
            Parent = parent;
        }

        public ValueStore Owner { get; }
        public InheritanceFrame? Parent { get; private set; }

        public bool TryGetFromThisOrAncestor(AvaloniaProperty property, [NotNullWhen(true)] out EffectiveValue? value)
        {
            var frame = this;

            while (frame is object)
            {
                if (frame.TryGetValue(property, out value))
                    return true;
                frame = frame.Parent;
            }

            value = default;
            return false;
        }

        public void SetParent(InheritanceFrame? value) => Parent = value;
    }
}
