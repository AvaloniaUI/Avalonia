using System;

namespace Avalonia.Metadata;

/// <summary>
/// Represents the kind of scope from which a data type can be inherited. Used in resolving target for AvaloniaProperty.
/// </summary>
public enum InheritDataTypeFromScopeKind
{
    /// <summary>
    /// Indicates that the data type should be inherited from a style.
    /// </summary>
    Style = 1,

    /// <summary>
    /// Indicates that the data type should be inherited from a control template.
    /// </summary>
    ControlTemplate,
}

/// <summary>
/// Attribute that instructs the compiler to resolve the data type using specific scope hints, such as Style or ControlTemplate.
/// </summary>
/// <remarks>
/// This attribute is used to configure markup extensions like TemplateBinding to properly parse AvaloniaProperty values,
/// targeting a specific scope data type.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class InheritDataTypeFromAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InheritDataTypeFromAttribute"/> class with the specified scope kind.
    /// </summary>
    /// <param name="scopeKind">The kind of scope from which to inherit the data type.</param>
    public InheritDataTypeFromAttribute(InheritDataTypeFromScopeKind scopeKind)
    {
        ScopeKind = scopeKind;
    }

    /// <summary>
    /// Gets the kind of scope from which the data type should be inherited.
    /// </summary>
    public InheritDataTypeFromScopeKind ScopeKind { get; }
}
