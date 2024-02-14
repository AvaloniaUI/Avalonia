using System;

namespace Avalonia.Metadata;

public enum InheritDataTypeFromScopeKind
{
    Style = 1,
    ControlTemplate,
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class InheritDataTypeFromAttribute : Attribute
{
    public InheritDataTypeFromAttribute(InheritDataTypeFromScopeKind scopeKind)
    {
        ScopeKind = scopeKind;
    }
    
    public InheritDataTypeFromScopeKind ScopeKind { get; }
}
