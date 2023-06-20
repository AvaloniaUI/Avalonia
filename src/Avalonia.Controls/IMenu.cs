using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Metadata;
using Avalonia.Rendering;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents a <see cref="Menu"/> or <see cref="ContextMenu"/>.
    /// </summary>
    internal interface IMenu : IMenuElement, IInputElement
    {
        /// <summary>
        /// Gets the menu interaction handler.
        /// </summary>
        IMenuInteractionHandler InteractionHandler { get; }

        /// <summary>
        /// Gets a value indicating whether the menu is open.
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// Gets the root of the visual tree, if the control is attached to a visual tree.
        /// </summary>
        IRenderRoot? VisualRoot { get; }
    }
}
