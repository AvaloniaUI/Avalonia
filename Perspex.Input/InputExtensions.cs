// -----------------------------------------------------------------------
// <copyright file="InputExtensions.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class InputExtensions
    {
        public static IEnumerable<IInputElement> GetInputElementsAt(this IInputElement element, Point p)
        {
            Contract.Requires<NullReferenceException>(element != null);

            if (element.Bounds.Contains(p) && element.IsHitTestVisible && element.IsEnabledCore)
            {
                p -= element.Bounds.Position;

                if (element.VisualChildren.Any())
                {
                    foreach (var child in element.VisualChildren.OfType<IInputElement>())
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
    }
}
