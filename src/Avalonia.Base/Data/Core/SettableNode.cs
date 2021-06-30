using System;

namespace Avalonia.Data.Core
{
    public abstract class SettableNode : ExpressionNode
    {
        public bool SetTargetValue(object value)
        {
            if (ShouldNotSet(value))
            {
                return true;
            }
            return SetTargetValueCore(value);
        }

        private bool ShouldNotSet(object value)
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

            bool isLastValueAlive = LastValue.TryGetTarget(out object lastValue);

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
                return lastValue.Equals(value);
            }

            return ReferenceEquals(lastValue, value);
        }

        protected abstract bool SetTargetValueCore(object value);

        public abstract Type PropertyType { get; }
    }
}
