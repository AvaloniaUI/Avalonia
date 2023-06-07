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
    }
}
