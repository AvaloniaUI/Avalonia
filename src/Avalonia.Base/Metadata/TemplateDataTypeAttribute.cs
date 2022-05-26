using System;

namespace Avalonia.Metadata;

/// <summary>
/// Defines the property that contains type of the data passed to the <see cref="IDataTemplate"/> implementation.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class TemplateDataTypeAttribute : Attribute
{
    
}
