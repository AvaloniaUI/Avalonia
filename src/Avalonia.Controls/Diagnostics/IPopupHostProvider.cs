using System;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;

namespace Avalonia.Controls.Diagnostics
{
    /// <summary>
    /// Diagnostics interface to retrieve an associated <see cref="IPopupHost"/>.
    /// </summary>
    [NotClientImplementable]
    public interface IPopupHostProvider
    {
        /// <summary>
        /// The popup host.
        /// </summary>
        IPopupHost? PopupHost { get; }

        /// <summary>
        /// Raised when the popup host changes.
        /// </summary>
        event Action<IPopupHost?>? PopupHostChanged;
    }
}
