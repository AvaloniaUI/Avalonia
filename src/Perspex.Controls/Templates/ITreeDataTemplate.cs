





namespace Perspex.Controls.Templates
{
    using System.Collections;

    /// <summary>
    /// Interface representing a template used to build hierachical data.
    /// </summary>
    public interface ITreeDataTemplate : IDataTemplate
    {
        /// <summary>
        /// Checks to see if the item should be initially expanded.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>True if the item should be initially expanded, otherwise false.</returns>
        bool IsExpanded(object item);

        /// <summary>
        /// Selects the child items of an item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The child items, or null if no child items.</returns>
        IEnumerable ItemsSelector(object item);
    }
}