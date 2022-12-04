using System.Linq;
using XamlX.TypeSystem;

namespace Avalonia.NameGenerator.Generator;

internal static class ResolverExtensions
{
	public static bool IsAvaloniaControl(this IXamlType clrType)
	{
		return clrType.HasControlBaseType() || clrType.HasIControlInterface();
	}

	private static bool HasControlBaseType(this IXamlType clrType)
	{
		// Check for the base type since IControl interface is removed.
		// https://github.com/AvaloniaUI/Avalonia/pull/9553
		if (clrType.FullName == "Avalonia.Controls.Control")
			return true;

		if (clrType.BaseType != null)
			return IsAvaloniaControl(clrType.BaseType);

		return false;
	}

	private static bool HasIControlInterface(this IXamlType clrType)
	{
		return clrType
			.Interfaces
			.Any(abstraction => abstraction.IsInterface &&
			                    abstraction.FullName == "Avalonia.Controls.IControl");
	}
}