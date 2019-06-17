// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    /// <summary>
    /// Defines extensions for the <see cref="IInputElement"/> interface.
    /// </summary>
    public static class InputExtensions
    {
        /// <summary>
        /// Returns the active input elements at a point on an <see cref="IInputElement"/>.
        /// </summary>
        /// <param name="element">The element to test.</param>
        /// <param name="p">The point on <paramref name="element"/>.</param>
        /// <returns>
        /// The active input elements found at the point, ordered topmost first.
        /// </returns>
        public static IEnumerable<IInputElement> GetInputElementsAt(this IInputElement element, Point p)
        {
            Contract.Requires<ArgumentNullException>(element != null);

            return element.GetVisualsAt(p, IsHitTestVisible).Cast<IInputElement>();
        }

        /// <summary>
        /// Returns the topmost active input element at a point on an <see cref="IInputElement"/>.
        /// </summary>
        /// <param name="element">The element to test.</param>
        /// <param name="p">The point on <paramref name="element"/>.</param>
        /// <returns>The topmost <see cref="IInputElement"/> at the specified position.</returns>
        public static IInputElement InputHitTest(this IInputElement element, Point p)
        {
            return element.GetInputElementsAt(p).FirstOrDefault();
        }

        private static bool IsHitTestVisible(IVisual visual)
        {
            var element = visual as IInputElement;
            return element != null &&
                   element.IsVisible &&
                   element.IsHitTestVisible &&
                   element.IsEnabledCore &&
                   element.IsAttachedToVisualTree;
        }
        
        /// <summary>
        /// Gets the IFocusManager instance for an <see cref="IVisual"/>.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>
        /// The focus manager or null if the visual is not rooted.
        /// </returns>
        public static IFocusManager GetFocusManager(this IVisual visual)
        {
            Contract.Requires<ArgumentNullException>(visual != null);

            return (visual as IInputRoot ?? visual.VisualRoot as IInputRoot)?.FocusManager;
        }
    }
}
