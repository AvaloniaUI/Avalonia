using System.Collections.Generic;
using XamlX.TypeSystem;

namespace Avalonia.Generators.Common.Domain;

internal interface ICodeGenerator
{
    string GenerateCode(string className, string nameSpace, IXamlType xamlType, IEnumerable<ResolvedName> names);
}
