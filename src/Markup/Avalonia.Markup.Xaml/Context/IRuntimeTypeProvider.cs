using System;
using System.Collections.Generic;
using System.Reflection;

namespace Avalonia.Markup.Xaml.Context
{
    public interface IRuntimeTypeProvider
    {
        Type FindType(string xamlNamespace, string name, IEnumerable<Type> typeArguments);

        IEnumerable<Assembly> ReferencedAssemblies { get; }
    }
}
