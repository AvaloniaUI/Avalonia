// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Rendering;

namespace Perspex
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
        /// <param name="root">The root visual.</param>
        /// <param name="nameScope">The name scope.</param>
        public VisualTreeAttachmentEventArgs(IRenderRoot root, INameScope nameScope)
        {
            Root = root;
            NameScope = nameScope;
        }

        /// <summary>
        /// Gets the root of the visual tree that the visual is being attached to or detached from.
        /// </summary>
        public IRenderRoot Root { get; }

        /// <summary>
        /// Gets the element's name scope.
        /// </summary>
        public INameScope NameScope { get; }
    }
}
