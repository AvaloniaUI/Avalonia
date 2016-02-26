// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Styling
{
    /// <summary>
    /// Represents a setter for a <see cref="Style"/>.
    /// </summary>
    public interface ISetter
    {
        /// <summary>
        /// Applies the setter to a control.
        /// </summary>
        /// <param name="style">The style that is being applied.</param>
        /// <param name="control">The control.</param>
        /// <param name="activator">An optional activator.</param>
        void Apply(IStyle style, IStyleable control, IObservable<bool> activator);
    }
}