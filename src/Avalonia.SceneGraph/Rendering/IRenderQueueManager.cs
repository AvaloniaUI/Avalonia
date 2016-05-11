// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Defines the interface for a <see cref="RenderQueueManager"/>.
    /// </summary>
    public interface IRenderQueueManager
    {
        /// <summary>
        /// Gets an observable that is fired whenever a render is required.
        /// </summary>
        IObservable<Unit> RenderNeeded { get; }

        /// <summary>
        /// Invalidates the render for the specified visual and raises <see cref="RenderNeeded"/>.
        /// </summary>
        /// <param name="visual">The visual.</param>
        void InvalidateRender(IVisual visual);

        /// <summary>
        /// Called when rendering is finished.
        /// </summary>
        void RenderFinished();
    }
}
