using System;
using Avalonia.Collections;

namespace Avalonia.Styling
{
    /// <summary>
    /// Interface for styleable elements.
    /// </summary>
    [Obsolete("This interface may be removed in 12.0. Use StyledElement, or override StyledElement.StyleKeyOverride to override the StyleKey for a class.")]
    public interface IStyleable : INamed
    {
        /// <summary>
        /// Gets the list of classes for the control.
        /// </summary>
        IAvaloniaReadOnlyList<string> Classes { get; }

        /// <summary>
        /// Gets the type by which the control is styled.
        /// </summary>
        [Obsolete("Override StyledElement.StyleKeyOverride instead.")]
        Type StyleKey { get; }

        /// <summary>
        /// Gets the template parent of this element if the control comes from a template.
        /// </summary>
        AvaloniaObject? TemplatedParent { get; }
    }
}
