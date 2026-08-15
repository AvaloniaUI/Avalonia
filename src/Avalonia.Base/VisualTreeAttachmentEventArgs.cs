using System;
using System.Diagnostics;
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
            RootVisual = presentationSource.RootVisual ??
                         throw new InvalidOperationException("PresentationSource must have a non-null RootVisual.");
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

        [Obsolete("This was previously always returning TopLevel. This is no longer guaranteed. Use TopLevel.GetTopLevel(this) if you need a TopLevel or args.RootVisual if you are interested in the root of the visual tree.")]
        public Visual Root => RootVisual;
        
        /// <summary>
        /// The root visual of the tree this visual is being attached to or detached from.
        /// This is guaranteed to be non-null and will be the same as <see cref="IPresentationSource.RootVisual"/>.
        /// </summary>
        public Visual RootVisual { get; set; }
    }
}
