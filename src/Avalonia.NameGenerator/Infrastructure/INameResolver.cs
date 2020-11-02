using System.Collections.Generic;

namespace Avalonia.NameGenerator.Infrastructure
{
    internal interface INameResolver
    {
        IReadOnlyList<(string TypeName, string Name)> ResolveNames(string xaml);
    }
}