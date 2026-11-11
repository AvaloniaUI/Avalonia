using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Controls
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SceneAppAttribute<T> : Attribute where T : Application
    {
    }
}
