using System.Collections.Generic;
using XamlX.TypeSystem;

namespace Avalonia.Generators.Common.Domain;

internal interface ICodeGenerator
{
    string GenerateCode(ResolvedView view, IEnumerable<ResolvedName> names);
}
