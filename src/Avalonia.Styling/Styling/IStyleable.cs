// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
        /// Gets the list of classes for the control.
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
        /// Notifies the element that a style has been applied.
        /// </summary>
        /// <param name="instance">The style instance.</param>
        void StyleApplied(IStyleInstance instance);

        /// <summary>
        /// Detaches all styles applied to the element.
        /// </summary>
        void DetachStyles();
    }
}
