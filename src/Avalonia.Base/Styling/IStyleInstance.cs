using Avalonia.Metadata;

namespace Avalonia.Styling
{
    /// <summary>
    /// Represents a <see cref="Style"/> that has been instanced on a control.
    /// </summary>
    internal interface IStyleInstance
    {
        /// <summary>
        /// Gets the source style.
        /// </summary>
        IStyle Source { get; }

        /// <summary>
        /// Gets a value indicating whether this style instance has an activator.
        /// </summary>
        /// <remarks>
        /// A style instance without an activator will always be active.
        /// </remarks>
        bool HasActivator { get; }

        /// <summary>
        /// Gets a value indicating whether this style is active.
        /// </summary>
        bool IsActive { get; }
    }
}
