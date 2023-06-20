using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Indicates that the property resolves an element by Name or x:Name.
    /// When applying this to attached properties, ensure to put on both
    /// the Getter and Setter methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class ResolveByNameAttribute : Attribute
    {
    }
}
