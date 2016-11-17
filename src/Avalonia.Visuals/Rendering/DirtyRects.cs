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
            if (!rect.IsEmpty)
            {
                for (var i = 0; i < _rects.Count; ++i)
                {
                    var union = _rects[i].Union(rect);

                    if (union != Rect.Empty)
                    {
                        _rects[i] = union;
                        return;
                    }
                }

                _rects.Add(rect);
            }
        }

        public IList<Rect> Coalesce()
        {
            for (var i = _rects.Count - 1; i >= 0; --i)
            {
                var a = _rects[i].Inflate(1);

                for (var j = 0; j < i; ++j)
                {
                    var b = _rects[j];

                    if (a.Intersects(b))
                    {
                        _rects[i] = _rects[i].Union(b);
                        _rects.RemoveAt(i);
                    }
                }
            }

            return _rects;
        }
    }
}
