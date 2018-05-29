using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Data.Core
{
    interface ITransformNode
    {
        object Transform(object value);
    }
}
