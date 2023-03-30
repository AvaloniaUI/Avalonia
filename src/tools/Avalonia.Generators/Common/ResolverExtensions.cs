using System.Linq;
using XamlX.TypeSystem;

namespace Avalonia.Generators.Common;

internal static class ResolverExtensions
{
    public static bool IsAvaloniaStyledElement(this IXamlType clrType) =>
        clrType.HasStyledElementBaseType() ||
        clrType.HasIStyledElementInterface();

    private static bool HasStyledElementBaseType(this IXamlType clrType)
    {
        // Check for the base type since IStyledElement interface is removed.
        // https://github.com/AvaloniaUI/Avalonia/pull/9553
        if (clrType.FullName == "Avalonia.StyledElement")
            return true;
        return clrType.BaseType != null && IsAvaloniaStyledElement(clrType.BaseType);
    }

    private static bool HasIStyledElementInterface(this IXamlType clrType) =>
        clrType.Interfaces.Any(abstraction =>
            abstraction.IsInterface &&
            abstraction.FullName == "Avalonia.IStyledElement");
}
