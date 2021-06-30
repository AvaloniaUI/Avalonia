using System;
using Avalonia.Collections;

#nullable enable

namespace Avalonia.Styling
{
    /// <summary>
    /// Interface for styleable elements.
    /// </summary>
    public interface IStyleable : IAvaloniaObject, INamed
    {
        /// <summary>
        /// Gets the list of style classes for the control.
        /// </summary>
        IAvaloniaReadOnlyList<string> Classes { get; }

        /// <summary>
        /// Gets the type by which the control is styled.
        /// </summary>
        Type StyleKey { get; }

        /// <summary>
        /// Gets the template parent of this element if the control comes from a template.
        /// </summary>
        ITemplatedControl? TemplatedParent { get; }

        /// <summary>
        /// Applies a style to the element.
        /// </summary>
        /// <param name="style">The style.</param>
        /// <param name="host">The control that hosts the style.</param>
        SelectorMatchResult ApplyStyle(Style style, IStyleHost? host = null);

        /// <summary>
        /// Begins a styling update.
        /// </summary>
        /// <remarks>
        /// Surrounding a set of <see cref="ApplyStyle(Style, IStyleHost)"/> calls with calls to
        /// <see cref="BeginStyling"/> and <see cref="EndStyling"/> will cause evaluation of
        /// style changes to only take place once, when <see cref="EndStyling"/> is called.
        /// </remarks>
        void BeginStyling();

        /// <summary>
        /// Ends a styling update.
        /// </summary>
        void EndStyling();
    }
}
