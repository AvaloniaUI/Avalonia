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
        /// Clears focus from control while keeping focus in another scopes.
        /// </summary>
        /// <param name="control">Control which should lose focus</param>
        /// <param name="parent">Optional control parent (for cases when it is not available from control itself, i.e. detaching from tree)</param>
        void ClearFocus(IInputElement control, IInputElement? parent);

        void UpdateFocusWithin(IRenderRoot root);

        IEnumerable<IInputElement> GetFocusedElements(IRenderRoot root);
    }
}
