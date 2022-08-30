using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;

#nullable enable

namespace Avalonia.Media
{
    public sealed class GeometryCollection : MediaCollection<Geometry>
    {
        public GeometryCollection()
        {
            Setup();
        }

        public GeometryCollection(IEnumerable<Geometry> items) : base(items)
        {
            Setup();
        }

        private void Setup()
        {
            this.ForEachItem(Invalidate, Invalidate, () => throw new NotSupportedException());
        }

        private void Invalidate(Geometry child)
        {
            foreach (var parent in Parents.OfType<GeometryGroup>())
            {
                parent.Invalidate();
            }
        }
    }
}
