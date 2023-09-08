using System.Collections.Generic;
using Avalonia.Metadata;
using Avalonia.Rendering;

namespace Avalonia.Input
{
    /// <summary>
    /// Manages focus for the application.
    /// </summary>
    [NotClientImplementable]
    public interface IFocusManager
    {
        /// <summary>
        /// Gets the currently focused <see cref="IInputElement"/>.
        /// </summary>
        IInputElement? GetFocusedElement();

        /// <summary>
        /// Clears currently focused element.
        /// </summary>
        [Unstable("This API might be removed in 11.x minor updates. Please consider focusing another element instead of removing focus at all for better UX.")]
        void ClearFocus();

        /// <summary>
        /// Returns list of focused controls in all focus scopes inside specified root.
        /// </summary>
        IEnumerable<IInputElement> GetFocusedElements(IRenderRoot root);
    }
}
