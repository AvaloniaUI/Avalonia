using System;
using System.Collections.Generic;
using Avalonia.Collections;

#nullable enable

namespace Avalonia.Media
{
    public sealed class GeometryCollection : AvaloniaList<Geometry> 
    {
        public GeometryCollection()
        {
            ResetBehavior = ResetBehavior.Remove;

            this.ForEachItem(
               x =>
               {
                   Parent?.Invalidate();
               },
               x =>
               {
                   Parent?.Invalidate();
               },
               () => throw new NotSupportedException());
        }

        public GeometryCollection(IEnumerable<Geometry> items) : base(items)
        {
            ResetBehavior = ResetBehavior.Remove;

            this.ForEachItem(
               x =>
               {
                   Parent?.Invalidate();
               },
               x =>
               {
                   Parent?.Invalidate();
               },
               () => throw new NotSupportedException());
        }

        public GeometryGroup? Parent { get; set; }
    }
}
