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
        /// <param name="attachmentPoint">The parent that the visual's tree is being attached to or detached from.</param>
        /// <param name="presentationSource">Presentation source this visual is being attached to.</param>
        public VisualTreeAttachmentEventArgs(Visual? attachmentPoint, IPresentationSource presentationSource)
        {
            AttachmentPoint = attachmentPoint;
            PresentationSource = presentationSource ?? throw new ArgumentNullException(nameof(presentationSource));
        }

        /// <summary>
        /// Gets the parent that the visual's tree is being attached to or detached from, null means that
        /// the entire tree is being attached to a PresentationSource
        /// </summary>
        public Visual? AttachmentPoint { get; }

        [Obsolete("Use " + nameof(AttachmentPoint))]
        public Visual? Parent => AttachmentPoint;

        /// <summary>
        /// Gets the root of the visual tree that the visual is being attached to or detached from.
        /// </summary>
        public IPresentationSource PresentationSource { get; }

        [Obsolete("Don't use")]
        // TODO: Remove all usages from the codebase, write docs explaining that this is not necessary a TopLevel
        public Visual Root => PresentationSource.RootVisual!;
    }
}
