using System;

namespace Avalonia.Metadata;

/// <summary>
/// Instructs the compiler to resolve the compiled bindings data type for the item-specific properties of collection-like controls. 
/// </summary>
/// <remarks>
/// A typical usage example is a ListBox control, where DataTypeInheritFrom is defined on the ItemTemplate property,
/// allowing the template to inherit the data type from the Items collection binding. 
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class DataTypeInheritFromAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataTypeInheritFromAttribute"/> class.
    /// </summary>
    /// <param name="ancestorProperty">The name of the property whose item type should be used on the target property.</param>
    public DataTypeInheritFromAttribute(string ancestorProperty)
    {
        AncestorProperty = ancestorProperty;
    }
    
    /// <summary>
    /// The name of the property whose item type should be used on the target property.
    /// </summary>
    public string AncestorProperty { get; }
    
    /// <summary>
    /// The ancestor type to be used in a lookup for the <see cref="AncestorProperty"/>.
    /// If null, the declaring type of the target property is used.
    /// </summary>
    public Type? AncestorType { get; set; }
}
