using System;
using Avalonia.Rendering;

namespace Avalonia
{
    /// <summary>
    /// Holds the event arguments for the <see cref="Visual.AttachedToVisualTree"/> and 
    /// <see cref="Visual.DetachedFromVisualTree"/> events.
    /// </summary>
    public class VisualTreeAttachmentEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VisualTreeAttachmentEventArgs"/> class.
        /// </summary>
        /// <param name="parent">The parent that the visual is being attached to or detached from.</param>
        /// <param name="root">The root visual.</param>
        public VisualTreeAttachmentEventArgs(Visual parent, IRenderRoot root)
        {
            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
            Root = root ?? throw new ArgumentNullException(nameof(root));
        }

        /// <summary>
        /// Gets the parent that the visual is being attached to or detached from.
        /// </summary>
        public Visual Parent { get; }

        /// <summary>
        /// Gets the root of the visual tree that the visual is being attached to or detached from.
        /// </summary>
        public IRenderRoot Root { get; }
    }
}
