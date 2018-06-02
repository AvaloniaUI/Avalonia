using Avalonia.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Data.Core
{
    internal abstract class SettableNode : ExpressionNode
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
                return LastValue?.Target.Equals(value) ?? false;
            }
            return LastValue != null && Object.ReferenceEquals(LastValue?.Target, value);
        }

        protected abstract bool SetTargetValueCore(object value, BindingPriority priority);

        public abstract Type PropertyType { get; }
    }
}
