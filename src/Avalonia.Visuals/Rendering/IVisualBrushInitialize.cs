// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Media;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Internal interface for initializing controls that are to be used as the viusal in a
    /// <see cref="VisualBrush"/>.
    /// </summary>
    public interface IVisualBrushInitialize
    {
        /// <summary>
        /// Ensures that the control is ready to use as the visual in a visual brush.
        /// </summary>
        void EnsureInitialized();
    }
}
