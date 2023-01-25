using System;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Data.Core.Plugins
{
    /// <summary>
    /// Defines how a member is read, written and observed by an
    /// <see cref="ExpressionObserver"/>.
    /// </summary>
    public interface IPropertyAccessorPlugin
    {
        /// <summary>
        /// Checks whether this plugin can handle accessing the properties of the specified object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>True if the plugin can handle the property on the object; otherwise false.</returns>
        [RequiresUnreferencedCode(TrimmingMessages.PropertyAccessorsRequiresUnreferencedCodeMessage)]
        bool Match(object obj, string propertyName);

        /// <summary>
        /// Starts monitoring the value of a property on an object.
        /// </summary>
        /// <param name="reference">A weak reference to the object.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>
        /// An <see cref="IPropertyAccessor"/> interface through which future interactions with the 
        /// property will be made.
        /// </returns>
        [RequiresUnreferencedCode(TrimmingMessages.PropertyAccessorsRequiresUnreferencedCodeMessage)]
        IPropertyAccessor? Start(WeakReference<object?> reference,
            string propertyName);
    }
}
