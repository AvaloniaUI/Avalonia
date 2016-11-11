// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.VisualTree
{
    /// <summary>
    /// Tracks the bounds of a control.
    /// </summary>
    /// <remarks>
    /// This class is used to track a controls's bounds for hit testing.
    /// TODO: This shouldn't be implemented as an attached property: it would be more performant
    /// to just store bounds in some sort of central repository.
    /// </remarks>
    public class BoundsTracker
    {
        /// <summary>
        /// Defines the TransformedBounds attached property.
        /// </summary>
        private static AttachedProperty<TransformedBounds?> TransformedBoundsProperty =
            AvaloniaProperty.RegisterAttached<BoundsTracker, Visual, TransformedBounds?>("TransformedBounds");

        /// <summary>
        /// Starts tracking the specified visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>An observable that returns the tracked bounds.</returns>
        public IObservable<TransformedBounds?> Track(Visual visual)
        {
            return visual.GetObservable(TransformedBoundsProperty);
        }

        /// <summary>
        /// Sets the transformed bounds of the visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="value">The transformed bounds.</param>
        internal static void SetTransformedBounds(Visual visual, TransformedBounds? value)
        {
            visual.SetValue(TransformedBoundsProperty, value);
        }

        /// <summary>
        /// Gets the transformed bounds of the visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The transformed bounds or null if the visual is not visible.</returns>
        public static TransformedBounds? GetTransformedBounds(Visual visual) => visual.GetValue(TransformedBoundsProperty);
    }
}
