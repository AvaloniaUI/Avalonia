using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Generator;

static class Helpers
{
    public static StringBuilder Pad(this StringBuilder sb, int count) => sb.Append(' ', count * 4);

    public static string GetFullyQualifiedName(this ISymbol symbol)
    {
        return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }
    
    public static bool HasFullyQualifiedName(this ISymbol symbol, string name)
    {
        return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == name;
    }

    public static bool HasAttributeWithFullyQualifiedName(this ISymbol symbol, string name)
    {
        ImmutableArray<AttributeData> attributes = symbol.GetAttributes();

        foreach (AttributeData attribute in attributes)
            if (attribute.AttributeClass?.HasFullyQualifiedName(name) == true)
                return true;

        return false;
    }
}