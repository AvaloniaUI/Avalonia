using System.Collections.Generic;
using SharpDX.Direct2D1;
using AM = Avalonia.Media;

namespace Avalonia.Direct2D1.Media
{
    /// <summary>
    /// A Direct2D implementation of a <see cref="Avalonia.Media.GeometryGroup"/>.
    /// </summary>
    internal class GeometryGroupImpl : GeometryImpl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryImpl"/> class.
        /// </summary>
        public GeometryGroupImpl(AM.FillRule fillRule, IReadOnlyList<AM.Geometry> geometry)
            : base(CreateGeometry(fillRule, geometry))
        {
        }

        private static Geometry CreateGeometry(AM.FillRule fillRule, IReadOnlyList<AM.Geometry> children)
        {
            var count = children.Count;
            var c = new Geometry[count];

            for (var i = 0; i < count; ++i)
            {
                c[i] = ((GeometryImpl)children[i].PlatformImpl).Geometry;
            }

            return new GeometryGroup(Direct2D1Platform.Direct2D1Factory, (FillMode)fillRule, c);
        }
    }
}
