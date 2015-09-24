// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Platform;

namespace Perspex.Media
{
    /// <summary>
    /// Represents the geometry of an arbitrarily complex shape.
    /// </summary>
    public class StreamGeometry : Geometry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometry"/> class.
        /// </summary>
        public StreamGeometry()
        {
            IPlatformRenderInterface factory = PerspexLocator.Current.GetService<IPlatformRenderInterface>();
            PlatformImpl = factory.CreateStreamGeometry();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometry"/> class.
        /// </summary>
        /// <param name="impl">The platform-specific implementation.</param>
        private StreamGeometry(IGeometryImpl impl)
        {
            PlatformImpl = impl;
        }

        /// <inheritdoc/>
        public override Rect Bounds => PlatformImpl.Bounds;

        /// <summary>
        /// Creates a <see cref="StreamGeometry"/> from a string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>A <see cref="StreamGeometry"/>.</returns>
        public static StreamGeometry Parse(string s)
        {
            StreamGeometry result = new StreamGeometry();

            using (StreamGeometryContext ctx = result.Open())
            {
                PathMarkupParser parser = new PathMarkupParser(result, ctx);
                parser.Parse(s);
                return result;
            }
        }

        /// <inheritdoc/>
        public override Geometry Clone()
        {
            return new StreamGeometry(((IStreamGeometryImpl)PlatformImpl).Clone());
        }

        /// <summary>
        /// Opens the geometry to start defining it.
        /// </summary>
        /// <returns>
        /// A <see cref="StreamGeometryContext"/> which can be used to define the geometry.
        /// </returns>
        public StreamGeometryContext Open()
        {
            return new StreamGeometryContext(((IStreamGeometryImpl)PlatformImpl).Open());
        }
    }
}
