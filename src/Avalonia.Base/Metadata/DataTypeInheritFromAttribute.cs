using System;

namespace Avalonia.Metadata;

/// <summary>
/// Hints the compiler how to resolve the compiled bindings data type for the collection-like controls' item specific properties.  
/// </summary>
/// <remarks>
/// Typical example usage is a ListBox control, where DataTypeInheritFrom is defined on the ItemTemplate property,
/// so template can try to inherit data type from the Items collection binding. 
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class DataTypeInheritFromAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataTypeInheritFromAttribute"/> class.
    /// </summary>
    /// <param name="ancestorProperty">Defines property name which items' type should used on the target property</param>
    public DataTypeInheritFromAttribute(string ancestorProperty)
    {
        AncestorProperty = ancestorProperty;
    }
    
    /// <summary>
    /// Defines property name which items' type should used on the target property.
    /// </summary>
    public string AncestorProperty { get; }
    
    /// <summary>
    /// Defines ancestor type which should be used in a lookup for <see cref="AncestorProperty"/>.
    /// If null, declaring type of the target property is used.
    /// </summary>
    public Type? AncestorType { get; set; }
}
