using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia
{
    interface IAliasedPropertyAccessor
    {
        AvaloniaProperty ResolveAlias();
    }
}
