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

            // A pure insertion or removal: the Parent change of the affected visual already invalidates its area.
            if (oldEnd == start || newEnd == start)
                return;

            for (var c = start; c < newEnd; c++)
                newList[c].InvalidateZOrder();
        }
    }
}
