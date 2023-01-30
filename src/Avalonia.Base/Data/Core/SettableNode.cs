using System;

namespace Avalonia.Data.Core
{
    internal abstract class SettableNode : ExpressionNode
    {
        public bool SetTargetValue(object? value, BindingPriority priority)
        {
            if (ShouldNotSet(value))
            {
                return true;
            }
            return SetTargetValueCore(value, priority);
        }

        private bool ShouldNotSet(object? value)
        {
            var propertyType = PropertyType;
            if (propertyType == null)
            {
                return false;
            }

            if (LastValue == null)
            {
                return false;
            }

            bool isLastValueAlive = LastValue.TryGetTarget(out var lastValue);

            if (!isLastValueAlive)
            {
                if (value == null && LastValue == NullReference)
                {
                    return true;
                }

                return false;
            }

            if (propertyType.IsValueType)
            {
                return Equals(lastValue, value);
            }

            return ReferenceEquals(lastValue, value);
        }

        protected abstract bool SetTargetValueCore(object? value, BindingPriority priority);

        public abstract Type? PropertyType { get; }
    }
}
