using System.Collections.Generic;

namespace XamlNameReferenceGenerator.Infrastructure
{
    internal interface INameResolver
    {
        IReadOnlyList<(string TypeName, string Name)> ResolveNames(string xaml);
    }
}