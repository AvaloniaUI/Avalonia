using System.Collections.Generic;
using XamlX.TypeSystem;

namespace Avalonia.NameGenerator.Domain;

internal interface ICodeGenerator
{
    string GenerateCode(string className, string nameSpace, IXamlType XamlType, IEnumerable<ResolvedName> names);
}