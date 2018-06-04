using Avalonia.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Data.Core
{
    interface ISettableNode
    {
        bool SetTargetValue(object value, BindingPriority priority);
        Type PropertyType { get; }
    }
}
