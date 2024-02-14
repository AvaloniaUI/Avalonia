using System;

namespace Avalonia.Metadata;

public enum InheritDataTypeFromScopeKind
{
    Style = 1,
    ControlTemplate,
}

/// <summary>
/// Instructs the compiler to resolve the data type using hints such as scope kind - Style or ControlTemplate. 
/// </summary>
/// <remarks>
/// Currently used to configure markup extensions like TemplateBinding to properly parse AvaloniaProperty values, targeting specific scope data type.  
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class InheritDataTypeFromAttribute : Attribute
{
    public InheritDataTypeFromAttribute(InheritDataTypeFromScopeKind scopeKind)
    {
        ScopeKind = scopeKind;
    }

    public InheritDataTypeFromScopeKind ScopeKind { get; }
}
