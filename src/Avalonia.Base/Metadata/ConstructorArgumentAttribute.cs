using System;

namespace Avalonia.Metadata;

/// <summary>
/// Indicates that a property corresponds to a named parameter in the constructor.
/// </summary>
/// <param name="name">The name of the parameter in the constructor.</param>
/// <remarks>This attribute is used for XAML.</remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ConstructorArgumentAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the name of the parameter in the constructor.
    /// </summary>
    public string Name { get; } = name;
}
