// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// A Skia implementation of a <see cref="ITransformedGeometryImpl"/>.
    /// </summary>
    internal class TransformedGeometryImpl : GeometryImpl, ITransformedGeometryImpl
    {
        /// <summary>
        ///  Initializes a new instance of the <see cref="TransformedGeometryImpl"/> class.
        /// </summary>
        /// <param name="source">Source geometry.</param>
        /// <param name="transform">Transform of new geometry.</param>
        public TransformedGeometryImpl(GeometryImpl source, Matrix transform)
        {
            SourceGeometry = source;
            Transform = transform;

            var transformedPath = source.EffectivePath.Clone();
            transformedPath.Transform(transform.ToSKMatrix());

            EffectivePath = transformedPath;
            Bounds = transformedPath.TightBounds.ToAvaloniaRect();
        }

        /// <inheritdoc />
        public override SKPath EffectivePath { get; }

        /// <inheritdoc />
        public IGeometryImpl SourceGeometry { get; }

        /// <inheritdoc />
        public Matrix Transform { get; }

        /// <inheritdoc />
        public override Rect Bounds { get; }
    }
}
