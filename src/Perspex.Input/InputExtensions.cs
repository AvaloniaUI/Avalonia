// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Perspex.Input
{
    public static class InputExtensions
    {
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
