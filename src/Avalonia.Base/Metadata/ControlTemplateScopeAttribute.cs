using System;

namespace Avalonia.Metadata;

/// <summary>
/// Indicates that a type acts as a control template scope (for example, TemplateBindings are expected to work).
/// Types annotated with this attribute may provide a TargetType property.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct,
    AllowMultiple = false,
    Inherited = true)]
public sealed class ControlTemplateScopeAttribute : Attribute;
