using Avalonia.Data;

namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// Interface representing a template used to build hierarchical data.
    /// </summary>
    public interface ITreeDataTemplate : IDataTemplate
    {
        /// <summary>
        /// Selects the child items of an item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>
        /// An <see cref="InstancedBinding"/> holding the items, or an observable that tracks the
        /// items. May return null if no child items.
        /// </returns>
        InstancedBinding? ItemsSelector(object item);
    }
}
