using System;

namespace Avalonia.Data.Core
{
    public abstract class SettableNode : ExpressionNode
    {
        public bool SetTargetValue(object value, BindingPriority priority)
        {
            if (ShouldNotSet(value))
            {
                return true;
            }
            return SetTargetValueCore(value, priority);
        }

        private bool ShouldNotSet(object value)
        {
            if (PropertyType == null)
            {
                return false;
            }

            if (LastValue == null)
            {
                return false;
            }

            bool isLastValueAlive = LastValue.TryGetTarget(out object lastValue);

            if (!isLastValueAlive)
            {
                if (value == null && LastValue == NullReference)
                {
                    return true;
                }

                return false;
            }

            if (PropertyType.IsValueType)
            {
                return lastValue.Equals(value);
            }

            return ReferenceEquals(lastValue, value);
        }

        protected abstract bool SetTargetValueCore(object value, BindingPriority priority);

        public abstract Type PropertyType { get; }
    }
}
