// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Perspex.Input
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

            if (element.Bounds.Contains(p) &&
                element.IsVisible &&
                element.IsHitTestVisible &&
                element.IsEnabledCore)
            {
                p -= element.Bounds.Position;

                if (element.VisualChildren.Any())
                {
                    foreach (var child in ZSort(element.VisualChildren.OfType<IInputElement>()))
                    {
                        foreach (var result in child.GetInputElementsAt(p))
                        {
                            yield return result;
                        }
                    }
                }

                yield return element;
            }
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

        private static IEnumerable<IInputElement> ZSort(IEnumerable<IInputElement> elements)
        {
            return elements
                .Select((element, index) => new ZOrderElement
                {
                    Element = element,
                    Index = index,
                    ZIndex = element.ZIndex,
                })
                .OrderBy(x => x, null)
                .Select(x => x.Element);
                
        }

        private class ZOrderElement : IComparable<ZOrderElement>
        {
            public IInputElement Element { get; set; }
            public int Index { get; set; }
            public int ZIndex { get; set; }

            public int CompareTo(ZOrderElement other)
            {
                var z = other.ZIndex - ZIndex;

                if (z != 0)
                {
                    return z;
                }
                else
                {
                    return other.Index - Index;
                }
            }
        }
    }
}
