// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Avalonia.Rendering
{
    public class DirtyRects : IEnumerable<Rect>
    {
        private List<Rect> _rects = new List<Rect>();

        public bool IsEmpty => _rects.Count == 0;

        public void Add(Rect rect)
        {
            if (!rect.IsEmpty)
            {
                for (var i = 0; i < _rects.Count; ++i)
                {
                    var r = _rects[i];

                    if (r.Inflate(1).Intersects(rect))
                    {
                        _rects[i] = r.Union(rect);
                        return;
                    }
                }

                _rects.Add(rect);
            }
        }

        public void Coalesce()
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
        }

        public IEnumerator<Rect> GetEnumerator() => _rects.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _rects.GetEnumerator();
    }
}
