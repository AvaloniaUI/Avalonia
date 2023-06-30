using Avalonia.Platform;

namespace Avalonia.Media
{
    internal sealed class PlatformGeometry : Geometry
    {
        private readonly IGeometryImpl _geometryImpl;

        public PlatformGeometry(IGeometryImpl geometryImpl)
        {
            _geometryImpl = geometryImpl;
        }

        public override Geometry Clone()
        {
            return new PlatformGeometry(_geometryImpl);
        }

        private protected override IGeometryImpl? CreateDefiningGeometry()
        {
           return _geometryImpl;
        }
    }
}
