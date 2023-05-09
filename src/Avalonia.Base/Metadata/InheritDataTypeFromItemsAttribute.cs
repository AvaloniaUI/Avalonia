using System;

namespace Avalonia.Metadata;

/// <summary>
/// Instructs the compiler to resolve the compiled bindings data type for the item-specific properties of collection-like controls. 
/// </summary>
/// <remarks>
/// A typical usage example is a ListBox control, where <see cref="InheritDataTypeFromItemsAttribute"/> is defined on the ItemTemplate property,
/// allowing the template to inherit the data type from the Items collection binding. 
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class InheritDataTypeFromItemsAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InheritDataTypeFromItemsAttribute"/> class.
    /// </summary>
    /// <param name="ancestorItemsProperty">The name of the property whose item type should be used on the target property.</param>
    public InheritDataTypeFromItemsAttribute(string ancestorItemsProperty)
    {
        AncestorItemsProperty = ancestorItemsProperty;
    }
    
    /// <summary>
    /// The name of the property whose item type should be used on the target property.
    /// </summary>
    public string AncestorItemsProperty { get; }

    /// <summary>
    /// The ancestor type to be used in a lookup for the <see cref="AncestorItemsProperty"/>.
    /// If null, the declaring type of the target property is used.
    /// </summary>
    public Type? AncestorType { get; set; }
}
