using System;
namespace Avalonia.Metadata;

/// <summary>
/// Allows AvaloniaVS to identify the base element type of ItemsControl and derivatives
/// </summary>
[AttributeUsage(AttributeTargets.Class,
    AllowMultiple = false,
    Inherited = true)]
public sealed class ItemTypeAttribute: DesignerAttribute
{
    readonly Type _baseType;
    public ItemTypeAttribute(Type baseType)
    {
        _baseType = baseType;
    }

    public Type BaseType => _baseType;
}
