// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Avalonia.Rendering
{
    public class DirtyRects
    {
        private List<Rect> _rects = new List<Rect>();

        public void Add(Rect rect)
        {
            for (var i = 0; i < _rects.Count; ++i)
            {
                var intersection = _rects[i].Intersect(rect);

                if (intersection != Rect.Empty)
                {
                    _rects[i] = intersection;
                    return;
                }
            }

            _rects.Add(rect);
        }

        public IList<Rect> Coalesce()
        {
            // TODO: Final coalesce
            return _rects;
        }
    }
}
