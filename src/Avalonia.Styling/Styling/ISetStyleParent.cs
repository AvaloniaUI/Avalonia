using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Controls;

namespace Avalonia.Styling
{
    /// <summary>
    /// Defines an interface through which a <see cref="Style"/>'s parent can be set.
    /// </summary>
    /// <remarks>
    /// You should not usually need to use this interface - it is for internal use only.
    /// </remarks>
    public interface ISetStyleParent : IStyle
    {
        /// <summary>
        /// Sets the style parent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        void SetParent(IResourceNode parent);

        /// <summary>
        /// Notifies the style that a change has been made to resources that apply to it.
        /// </summary>
        /// <param name="e">The event args.</param>
        /// <remarks>
        /// This method will be called automatically by the framework, you should not need to call
        /// this method yourself.
        /// </remarks>
        void NotifyResourcesChanged(ResourcesChangedEventArgs e);
    }
}
