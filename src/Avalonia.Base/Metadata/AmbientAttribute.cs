using System;

namespace Avalonia.Metadata
{
    /// <summary>
    /// Defines the ambient class/property
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = true)]
    public sealed class AmbientAttribute : Attribute
    {
    }
}
