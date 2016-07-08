// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.VisualTree
{
    /// <summary>
    /// Tracks the bounds of a control.
    /// </summary>
    /// <remarks>
    /// This class is used by Adorners to track the control that the adorner is attached to.
    /// </remarks>
    public class BoundsTracker
    {
        private static AttachedProperty<TransformedBounds> TransformedBoundsProperty =
            AvaloniaProperty.RegisterAttached<BoundsTracker, Visual, TransformedBounds>("TransformedBounds");

        /// <summary>
        /// Starts tracking the specified visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>An observable that returns the tracked bounds.</returns>
        public IObservable<TransformedBounds> Track(Visual visual)
        {
            return visual.GetObservable(TransformedBoundsProperty);
        }

        internal static void SetTransformedBounds(Visual visual, TransformedBounds bounds)
        {
            visual.SetValue(TransformedBoundsProperty, bounds);
        }

        /// <summary>
        /// Gets the transformed bounds of the visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The transformed bounds.</returns>
        public static TransformedBounds GetTransformedBounds(Visual visual) => visual.GetValue(TransformedBoundsProperty);
    }
}
