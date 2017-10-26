using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Markup.Data
{
    interface ITransformNode
    {
        object Transform(object value);
    }
}
