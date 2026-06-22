using System.Collections.Generic;

namespace Avalonia.Generators.Common.Domain;

internal interface ICodeGenerator
{
    string GenerateCode(string className, string nameSpace, IEnumerable<ResolvedName> names);
}
