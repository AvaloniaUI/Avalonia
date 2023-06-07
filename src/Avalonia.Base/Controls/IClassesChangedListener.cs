namespace Avalonia.Controls
{
    /// <summary>
    /// Internal interface for listening to changes in <see cref="Classes"/> in a more
    /// performant manner than subscribing to CollectionChanged.
    /// </summary>
    internal interface IClassesChangedListener
    {
        /// <summary>
        /// Notifies the listener that the <see cref="Classes"/> collection has changed.
        /// </summary>
        void Changed();
    }
}
