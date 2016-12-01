using System;
using System.Collections.Generic;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{
    public class LayerDirtyRects : Dictionary<IVisual, DirtyRects>
    {
        public bool IsEmpty
        {
            get
            {
                foreach (var i in Values)
                {
                    if (!i.IsEmpty)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public void Add(IVisual layerRoot, Rect rect)
        {
            DirtyRects rects;

            if (!TryGetValue(layerRoot, out rects))
            {
                Add(layerRoot, rects = new DirtyRects());
            }

            rects.Add(rect);
        }

        public void Coalesce()
        {
            foreach (var i in Values)
            {
                i.Coalesce();
            }
        }
    }
}
