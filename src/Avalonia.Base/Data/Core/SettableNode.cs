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
            if (PropertyType.IsValueType)
            {
                return LastValue?.Target != null && LastValue.Target.Equals(value);
            }
            return LastValue != null && Object.ReferenceEquals(LastValue?.Target, value);
        }

        protected abstract bool SetTargetValueCore(object value, BindingPriority priority);

        public abstract Type PropertyType { get; }
    }
}
