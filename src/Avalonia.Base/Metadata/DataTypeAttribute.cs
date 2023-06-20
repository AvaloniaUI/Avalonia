using System;

namespace Avalonia.Metadata;

/// <summary>
/// Defines the property that contains type that should be used as a type information for compiled bindings.
/// </summary>
/// <remarks>
/// Used on DataTemplate.DataType property so it can be inherited in compiled bindings inside of the template.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DataTypeAttribute : Attribute
{

}
