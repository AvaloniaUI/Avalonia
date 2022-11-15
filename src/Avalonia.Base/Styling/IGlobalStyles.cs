using System;
using System.Collections.Generic;
using Avalonia.Metadata;

namespace Avalonia.Styling
{
    /// <summary>
    /// Defines the style host that provides styles global to the application.
    /// </summary>
    [NotClientImplementable]
    public interface IGlobalStyles : IStyleHost
    {
        /// <summary>
        /// Raised when styles are added to <see cref="Styles"/> or a nested styles collection.
        /// </summary>
        public event Action<IReadOnlyList<IStyle>>? GlobalStylesAdded;

        /// <summary>
        /// Raised when styles are removed from <see cref="Styles"/> or a nested styles collection.
        /// </summary>
        public event Action<IReadOnlyList<IStyle>>? GlobalStylesRemoved;
    }
}
