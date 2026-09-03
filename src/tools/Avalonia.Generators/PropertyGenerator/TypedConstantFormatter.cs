using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Avalonia.Generators.PropertyGenerator;

internal static class TypedConstantFormatter
{
    /// <summary>
    /// Formats constant in C# code expression, including type casts if necessary.
    /// </summary>
    public static string FormatAsExpression(TypedConstant constant, ITypeSymbol targetType)
    {
        var expression = RenderValue(constant);

        if (constant is { IsNull: false, Type: { } constantType } &&
            !SymbolEqualityComparer.Default.Equals(constantType, targetType))
        {
            var targetRef = targetType.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));
            return $"({targetRef})({expression})";
        }

        return expression;
    }

    // Ideally, we want to use TypedConstant.ToCSharpString, but it has several issues:
    // See https://github.com/dotnet/roslyn/issues/76061
    // And https://github.com/dotnet/roslyn/issues/58705
    private static string RenderValue(TypedConstant constant)
    {
        if (constant.IsNull)
        {
            return "null";
        }

        switch (constant.Kind)
        {
            case TypedConstantKind.Enum:
                var enumType = constant.Type!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                return $"unchecked(({enumType})({FormatPrimitive(constant.Value!)}))";

            case TypedConstantKind.Type:
                var type = ((ITypeSymbol)constant.Value!).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                return $"typeof({type})";

            case TypedConstantKind.Array:
                var arrayType = (IArrayTypeSymbol)constant.Type!;
                var elementType = arrayType.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var elements = string.Join(", ", constant.Values.Select(RenderValue));
                return $"new {elementType}[] {{ {elements} }}";

            default:
                return FormatPrimitive(constant.Value!);
        }

        static string FormatPrimitive(object value) => value switch
        {
            // Special floating point values have no literal form.
            double.NaN => "double.NaN",
            double.PositiveInfinity => "double.PositiveInfinity",
            double.NegativeInfinity => "double.NegativeInfinity",
            float.NaN => "float.NaN",
            float.PositiveInfinity => "float.PositiveInfinity",
            float.NegativeInfinity => "float.NegativeInfinity",
            // Force a decimal point so the expression round-trips as a real literal even for whole values.
            double d => d == (long)d ? $"{(long)d}.0" : SymbolDisplay.FormatPrimitive(d, quoteStrings: false, useHexadecimalNumbers: false),
            float f => f == (long)f ? $"{(long)f}f" : SymbolDisplay.FormatPrimitive(f, quoteStrings: false, useHexadecimalNumbers: false) + "f",
            uint ui => ui + "u",
            long l => l + "L",
            ulong ul => ul + "UL",
            _ => SymbolDisplay.FormatPrimitive(value, quoteStrings: true, useHexadecimalNumbers: false),
        };
    }
}
