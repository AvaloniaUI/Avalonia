using System;
using System.Collections;
using System.Collections.Generic;

namespace Avalonia.Rendering
{
    public class DisplayDirtyRects : IEnumerable<DisplayDirtyRect>
    {
        private List<DisplayDirtyRect> _inner = new List<DisplayDirtyRect>();

        public void Add(Rect rect)
        {
            foreach (var r in _inner)
            {
                if (r.Rect == rect)
                {
                    r.ResetAge();
                    return;
                }
            }

            _inner.Add(new DisplayDirtyRect(rect));
        }

        public void Tick()
        {
            var now = DateTimeOffset.UtcNow;

            for (var i = _inner.Count - 1; i >= 0; --i)
            {
                var r = _inner[i];

                if (now > r.Dies)
                {
                    _inner.RemoveAt(i);
                }
            }
        }

        public IEnumerator<DisplayDirtyRect> GetEnumerator() => _inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
