// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using SharpDX.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    public class TransformedGeometryImpl : GeometryImpl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryImpl"/> class.
        /// </summary>
        /// <param name="geometry">An existing Direct2D <see cref="TransformedGeometry"/>.</param>
        public TransformedGeometryImpl(TransformedGeometry geometry)
            : base(geometry)
        {
        }

        /// <inheritdoc/>
        public override Matrix Transform => ((TransformedGeometry)Geometry).Transform.ToAvalonia();

        protected override Geometry GetSourceGeometry() => ((TransformedGeometry)Geometry).SourceGeometry;
    }
}
