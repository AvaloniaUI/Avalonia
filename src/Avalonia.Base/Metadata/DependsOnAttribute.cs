using System;

namespace Avalonia.Metadata
{
    /// <summary>
    /// Indicates that the property depends on the value of another property in markup.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public sealed class DependsOnAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DependsOnAttribute"/> class.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property that this property depends on.
        /// </param>
        public DependsOnAttribute(string propertyName)
        {
            Name = propertyName;
        }

        /// <summary>
        /// Gets the name of the property that this property depends on.
        /// </summary>
        public string Name { get; }
    }
}
