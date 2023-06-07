using System;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Data.Core.Plugins
{
    /// <summary>
    /// Defines a plugin that handles the '^' stream binding operator.
    /// </summary>
    public interface IStreamPlugin
    {
        /// <summary>
        /// Checks whether this plugin handles the specified value.
        /// </summary>
        /// <param name="reference">A weak reference to the value.</param>
        /// <returns>True if the plugin can handle the value; otherwise false.</returns>
        [RequiresUnreferencedCode(TrimmingMessages.StreamPluginRequiresUnreferencedCodeMessage)]
        bool Match(WeakReference<object?> reference);

        /// <summary>
        /// Starts producing output based on the specified value.
        /// </summary>
        /// <param name="reference">A weak reference to the object.</param>
        /// <returns>
        /// An observable that produces the output for the value.
        /// </returns>
        [RequiresUnreferencedCode(TrimmingMessages.StreamPluginRequiresUnreferencedCodeMessage)]
        IObservable<object?> Start(WeakReference<object?> reference);
    }
}
