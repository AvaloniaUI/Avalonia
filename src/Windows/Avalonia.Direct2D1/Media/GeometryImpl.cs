// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Platform;
using SharpDX.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    /// <summary>
    /// The platform-specific interface for <see cref="Avalonia.Media.Geometry"/>.
    /// </summary>
    public abstract class GeometryImpl : IGeometryImpl
    {
        private TransformedGeometry _transformed;

        /// <summary>
        /// Gets the geometry's bounding rectangle.
        /// </summary>
        public abstract Rect Bounds
        {
            get;
        }

        /// <summary>
        /// Gets the geomentry without any transforms applied.
        /// </summary>
        public abstract Geometry DefiningGeometry
        {
            get;
        }

        /// <summary>
        /// Gets the Direct2D <see cref="Geometry"/>.
        /// </summary>
        public Geometry Geometry => _transformed ?? DefiningGeometry;

        /// <summary>
        /// Gets or sets the transform for the geometry.
        /// </summary>
        public Matrix Transform
        {
            get
            {
                return _transformed != null ?
                    _transformed.Transform.ToAvalonia() :
                    Matrix.Identity;
            }

            set
            {
                if (value != Transform)
                {
                    if (_transformed != null)
                    {
                        _transformed.Dispose();
                        _transformed = null;
                    }

                    if (!value.IsIdentity)
                    {
                        Factory factory = AvaloniaLocator.Current.GetService<Factory>();
                        _transformed = new TransformedGeometry(
                            factory,
                            DefiningGeometry,
                            value.ToDirect2D());
                    }
                }
            }
        }

        /// <summary>
        /// Gets the geometry's bounding rectangle with the specified stroke thickness.
        /// </summary>
        /// <param name="strokeThickness">The stroke thickness.</param>
        /// <returns>The bounding rectangle.</returns>
        public Rect GetRenderBounds(double strokeThickness)
        {
            if (_transformed != null)
            {
                return _transformed.GetWidenedBounds((float)strokeThickness).ToAvalonia();
            }
            else
            {
                return DefiningGeometry.GetWidenedBounds((float)strokeThickness).ToAvalonia();
            }
        }
    }
}
