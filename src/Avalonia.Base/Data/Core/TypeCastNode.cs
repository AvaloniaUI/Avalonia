using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Data.Core
{
    internal class TypeCastNode : ExpressionNode
    {
        public override string Description => $"as {TargetType.FullName}";

        public Type TargetType { get; }

        public TypeCastNode(Type type)
        {
            TargetType = type;
        }

        protected virtual object? Cast(object? value)
        {
            return TargetType.IsInstanceOfType(value) ? value : null;
        }

        protected override void StartListeningCore(WeakReference<object?> reference)
        {
            if (reference.TryGetTarget(out var target))
            {
                target = Cast(target);
                reference = target == null ? NullReference : new WeakReference<object?>(target);
            }

            base.StartListeningCore(reference);
        }
    }
}
