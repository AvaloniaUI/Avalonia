using System;

namespace Avalonia.Metadata;

// This class was created to identify the attributes used in the designer
// and in the future be able to remove them from the assemblies
// during packaging and leave them only in the reference assemblies.

/// <summary>
/// Base class for all DesignerAttribute
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property,
    Inherited = true,
    AllowMultiple = false)]
public abstract class DesignerAttribute:Attribute
{
}
