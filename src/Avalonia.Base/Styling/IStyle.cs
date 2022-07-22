using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Metadata;

namespace Avalonia.Styling
{
    /// <summary>
    /// Defines the interface for styles.
    /// </summary>
    [NotClientImplementable]
    public interface IStyle : IResourceNode
    {
        /// <summary>
        /// Gets a collection of child styles.
        /// </summary>
        IReadOnlyList<IStyle> Children { get; }

        /// <summary>
        /// Attaches the style and any child styles to a control if the style's selector matches.
        /// </summary>
        /// <param name="target">The control to attach to.</param>
        /// <param name="host">The element that hosts the style.</param>
        /// <returns>
        /// A <see cref="SelectorMatchResult"/> describing how the style matches the control.
        /// </returns>
        SelectorMatchResult TryAttach(IStyleable target, object? host);
    }
}
