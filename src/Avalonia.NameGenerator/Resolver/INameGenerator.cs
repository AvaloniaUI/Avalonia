using System.Collections.Generic;

namespace Avalonia.NameGenerator.Resolver
{
    internal interface INameGenerator
    {
        string GenerateNames(string className, string nameSpace, IEnumerable<ResolvedName> names);
    }
}