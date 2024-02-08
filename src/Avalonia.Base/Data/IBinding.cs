using System;
using Avalonia.Diagnostics;
using Avalonia.Metadata;

namespace Avalonia.Data
{
    /// <summary>
    /// Holds a binding that can be applied to a property on an object.
    /// </summary>
    [NotClientImplementable]
    public interface IBinding
    {
        /// <summary>
        /// Initiates the binding on a target object.
        /// </summary>
        /// <param name="target">The target instance.</param>
        /// <param name="targetProperty">The target property. May be null.</param>
        /// <param name="anchor">
        /// An optional anchor from which to locate required context. When binding to objects that
        /// are not in the logical tree, certain types of binding need an anchor into the tree in 
        /// order to locate named controls or resources. The <paramref name="anchor"/> parameter 
        /// can be used to provide this context.
        /// </param>
        /// <param name="enableDataValidation">Whether data validation should be enabled.</param>
        /// <returns>
        /// A <see cref="InstancedBinding"/> or null if the binding could not be resolved.
        /// </returns>
        [Obsolete(ObsoletionMessages.MayBeRemovedInAvalonia12)]
        InstancedBinding? Initiate(
            AvaloniaObject target, 
            AvaloniaProperty? targetProperty,
            object? anchor = null,
            bool enableDataValidation = false);
    }
}
