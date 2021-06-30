using System.Diagnostics.CodeAnalysis;
using Avalonia.Data;

#nullable enable

namespace Avalonia.PropertyStore
{
    internal class InheritanceFrame : ValueFrameBase, IValueFrame
    {
        public InheritanceFrame(ValueStore owner, InheritanceFrame? parent = null)
        {
            Owner = owner;
            Parent = parent;
        }

        public override bool IsActive => true;
        public ValueStore Owner { get; }
        public InheritanceFrame? Parent { get; private set; }
        public override BindingPriority Priority => BindingPriority.Inherited;

        public bool TryGetFromThisOrAncestor(AvaloniaProperty property, [NotNullWhen(true)] out IValueEntry? value)
        {
            var frame = this;

            while (frame is object)
            {
                if (frame.TryGet(property, out value))
                    return true;
                frame = frame.Parent;
            }

            value = default;
            return false;
        }

        public void SetParent(InheritanceFrame? value) => Parent = value;
        public void SetValue(IValueEntry value) => Set(value);
    }
}
