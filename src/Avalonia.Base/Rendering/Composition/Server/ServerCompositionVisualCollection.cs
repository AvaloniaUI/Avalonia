using System;
using System.Collections.Generic;

namespace Avalonia.Rendering.Composition.Server
{
    partial class ServerCompositionVisualCollection
    {
        protected override void OnListChanged(List<ServerCompositionVisual> oldList)
        {
            var newList = List;

            var start = 0;
            var commonCount = Math.Min(oldList.Count, newList.Count);
            while (start < commonCount && ReferenceEquals(oldList[start], newList[start]))
                start++;

            var oldEnd = oldList.Count;
            var newEnd = newList.Count;
            while (oldEnd > start && newEnd > start && ReferenceEquals(oldList[oldEnd - 1], newList[newEnd - 1]))
            {
                oldEnd--;
                newEnd--;
            }

            // Visuals that enter or leave the list invalidate their area when their Parent changes. If one of the
            // ranges is empty, the change is only an insertion or a removal and the remaining visuals keep their
            // relative order.
            if (oldEnd == start || newEnd == start)
                return;

            // A visual that only moves within the list keeps its Parent and all of its other properties, so
            // nothing else redraws the area where its order relative to the siblings has changed.
            for (var c = start; c < newEnd; c++)
                newList[c].InvalidateZOrder();
        }
    }
}
