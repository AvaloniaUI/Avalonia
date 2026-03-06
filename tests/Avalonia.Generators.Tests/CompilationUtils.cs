using System.Collections.Generic;
using System.Linq;
using Avalonia.Generators.Common;
using Avalonia.Generators.Common.Domain;
using Avalonia.Generators.Compiler;
using Microsoft.CodeAnalysis;

namespace Avalonia.Generators.Tests;

internal static class CompilationUtils
{
    internal static IEnumerable<ResolvedName> ResolveNames(this IEnumerable<ResolvedXmlName> names, Compilation compilation, XamlXNameResolver nameResolver)
    {
        var compiler = MiniCompiler.CreateRoslyn(new RoslynTypeSystem(compilation), MiniCompiler.AvaloniaXmlnsDefinitionAttribute);
        return names
            .Select(xmlName =>
            {
                var clrType = compiler.ResolveXamlType(xmlName.XmlType);
                return (clrType, nameResolver.ResolveName(clrType, xmlName.Name, xmlName.FieldModifier));
            })
            .Where(t => t.clrType.IsAvaloniaStyledElement())
            .Select(t => t.Item2);
    }
}
