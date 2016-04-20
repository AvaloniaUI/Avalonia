using System;

namespace Perspex.Data
{
    [Flags]
    public enum ValidationMethods
    {
        None = 0,
        Exceptions = 1,
        INotifyDataErrorInfo = 2,
        All = -1
    }
}