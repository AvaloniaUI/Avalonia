using System;

namespace Avalonia.Documents
{
    /// <summary>
    /// Represents a <see cref="TextElement"/> with a <see cref="Text"/> property.
    /// </summary>
    public interface IHasText
    {
        /// <summary>
        /// Gets the element text.
        /// </summary>
        string Text { get; }
    }
}
