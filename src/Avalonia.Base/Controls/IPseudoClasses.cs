using Avalonia.Metadata;

namespace Avalonia.Controls
{
    /// <summary>
    /// Exposes an interface for setting pseudoclasses on a <see cref="Classes"/> collection.
    /// </summary>
    [NotClientImplementable]
    public interface IPseudoClasses
    {
        /// <summary>
        /// Adds a pseudoclass to the collection.
        /// </summary>
        /// <param name="name">The pseudoclass name.</param>
        void Add(string name);

        /// <summary>
        /// Removes a pseudoclass from the collection.
        /// </summary>
        /// <param name="name">The pseudoclass name.</param>
        bool Remove(string name);

        /// <summary>
        /// Returns whether a pseudoclass is present in the collection.
        /// </summary>
        /// <param name="name">The pseudoclass name.</param>
        /// <returns>Whether the pseudoclass is present.</returns>
        bool Contains(string name);
    }
}
