// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Collections;

namespace Perspex.Styling
{
    /// <summary>
    /// Interface for styleable elements.
    /// </summary>
    public interface IStyleable : IPerspexObject, INamed
    {
        /// <summary>
        /// Signalled when the control's style should be removed.
        /// </summary>
        IObservable<IStyleable> StyleDetach { get; }

        /// <summary>
        /// Gets the list of classes for the control.
        /// </summary>
        IPerspexReadOnlyList<string> Classes { get; }

        /// <summary>
        /// Gets the type by which the control is styled.
        /// </summary>
        Type StyleKey { get; }

        /// <summary>
        /// Gets the template parent of this element if the control comes from a template.
        /// </summary>
        ITemplatedControl TemplatedParent { get; }
    }
}
