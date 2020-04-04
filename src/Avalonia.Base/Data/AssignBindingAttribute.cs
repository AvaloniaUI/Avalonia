using System;

namespace Avalonia.Data
{
    /// <summary>
    /// Signifies that a binding can be assigned to a property.
    /// </summary>
    /// <remarks>
    /// Usually in markup, when a binding is set for a property that property will be bound. 
    /// Applying this attribute to a property indicates that the binding should be assigned to 
    /// the property rather than bound.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class AssignBindingAttribute : Attribute
    {
    }
}
