using System;

#nullable enable

namespace Avalonia.PropertyStore
{
    internal class DirectBindingBase<T> : IDisposable
    {
        public DirectBindingBase(AvaloniaObject owner, DirectPropertyBase<T> property)
        {
            Owner = owner;
            Property = AvaloniaPropertyRegistry.Instance.GetRegisteredDirect(owner, property);
            Owner.AddDirectBinding(this);
        }

        public AvaloniaObject Owner { get; }
        public DirectPropertyBase<T> Property { get; }

        public virtual void Dispose() => Owner.RemoveDirectBinding(this);
        public void OnCompleted() => Dispose();
        public void OnError(Exception _) => Dispose();
    }
}
