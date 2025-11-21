using System;

namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// Interface representing a template used to build hierarchical data.
    /// </summary>
    public interface ITreeDataTemplate : IDataTemplate
    {
        /// <summary>
        /// Binds the children of the specified item to a property on a target object.
        /// </summary>
        /// <param name="target">The target object.</param>
        /// <param name="targetProperty">The target property.</param>
        /// <param name="item">The item whose children should be bound.</param>
        /// <returns>
        /// An <see cref="IDisposable"/> that can be used to remove the binding.
        /// </returns>
        IDisposable BindChildren(AvaloniaObject target, AvaloniaProperty targetProperty, object item);
    }
}
