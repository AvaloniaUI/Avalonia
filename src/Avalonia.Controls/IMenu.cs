using Avalonia.Controls.Platform;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents a <see cref="Menu"/> or <see cref="ContextMenu"/>.
    /// </summary>
    public interface IMenu : IMenuElement
    {
        /// <summary>
        /// Gets the menu interaction handler.
        /// </summary>
        IMenuInteractionHandler InteractionHandler { get; }

        /// <summary>
        /// Gets a value indicating whether the menu is open.
        /// </summary>
        bool IsOpen { get; }
    }
}
