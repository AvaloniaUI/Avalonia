using System;
using Avalonia.Data;

namespace Avalonia;

/// <summary>
/// Instructs the Avalonia property source generator to emit an <see cref="AttachedProperty{TValue}"/>
/// registration and accessor methods for the annotated partial <c>Get</c> method.
/// </summary>
/// <remarks>
/// <para>
/// Apply to a <c>static partial T Get{Name}(THost element)</c> method declared on a <c>partial</c> class.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class AttachedPropertyAttribute : Attribute
{
    /// <summary>
    /// Re-owns an existing attached property from the specified base type instead of registering a new property.
    /// The source property must be a static <c>{Name}Property</c> member of the given type.
    /// Metadata may still be overridden with <see cref="DefaultValue"/>, <see cref="DefaultBindingMode"/> and <see cref="CoerceMethodName"/>.
    /// </summary>
    public Type? AddOwnerFrom { get; set; }

    /// <summary>
    /// The default value of the property. Must be a compile-time constant.
    /// For non-constant defaults, call <c>{Name}Property.OverrideDefaultValue(...)</c> in a static constructor instead.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// The default binding mode for the property.
    /// </summary>
    public BindingMode DefaultBindingMode { get; set; }

    /// <summary>
    /// Whether the property inherits its value down the logical tree.
    /// </summary>
    /// <remarks>
    /// Cannot be used with <see cref="AddOwnerFrom"/>. 
    /// </remarks>
    public bool Inherits { get; set; }

    /// <summary>
    /// The name of the static change handler to wire up, with signature
    /// <c>static void {Method}(THost host, T oldValue, T newValue)</c>.
    /// The generator emits a <c>private static partial</c> declaration for the method.
    /// </summary>
    public string? ChangedMethodName { get; set; }

    /// <summary>
    /// The name of the validation method, with signature
    /// <c>static bool {Method}(T value)</c>. Pass via <c>nameof(...)</c>.
    /// The generator emits a <c>private static partial</c> declaration for the method.
    /// </summary>
    /// <remarks>
    /// Cannot be used with <see cref="AddOwnerFrom"/>. 
    /// </remarks>
    public string? ValidateMethodName { get; set; }

    /// <summary>
    /// The name of the coercion method, with signature
    /// <c>static T {Method}(AvaloniaObject sender, T value)</c>.
    /// The generator emits a <c>private static partial</c> declaration for the method.
    /// </summary>
    public string? CoerceMethodName { get; set; }
}
