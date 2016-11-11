// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Platform;
using SharpDX.Direct2D1;
using D2DGeometry = SharpDX.Direct2D1.Geometry;

namespace Avalonia.Direct2D1.Media
{
    /// <summary>
    /// A Direct2D implementation of a <see cref="Avalonia.Media.StreamGeometry"/>.
    /// </summary>
    public class StreamGeometryImpl : GeometryImpl, IStreamGeometryImpl
    {
        private readonly PathGeometry _path;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryImpl"/> class.
        /// </summary>
        public StreamGeometryImpl()
        {
            Factory factory = AvaloniaLocator.Current.GetService<Factory>();
            _path = new PathGeometry(factory);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryImpl"/> class.
        /// </summary>
        /// <param name="geometry">An existing Direct2D <see cref="PathGeometry"/>.</param>
        protected StreamGeometryImpl(PathGeometry geometry)
        {
            _path = geometry;
        }

        /// <inheritdoc/>
        public override Rect Bounds => _path.GetWidenedBounds(0).ToAvalonia();

        /// <inheritdoc/>
        public override D2DGeometry DefiningGeometry => _path;

        /// <summary>
        /// Clones the geometry.
        /// </summary>
        /// <returns>A cloned geometry.</returns>
        public IStreamGeometryImpl Clone()
        {
            Factory factory = AvaloniaLocator.Current.GetService<Factory>();
            var result = new PathGeometry(factory);
            var sink = result.Open();
            _path.Stream(sink);
            sink.Close();
            return new StreamGeometryImpl(result);
        }

        /// <summary>
        /// Opens the geometry to start defining it.
        /// </summary>
        /// <returns>
        /// An <see cref="Avalonia.Platform.IStreamGeometryContextImpl"/> which can be used to define the geometry.
        /// </returns>
        public IStreamGeometryContextImpl Open()
        {
            return new StreamGeometryContextImpl(_path.Open());
        }
    }
}
