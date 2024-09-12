using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Visuals.Platform;

namespace Avalonia.Media
{
    public sealed class PathFigures : AvaloniaList<PathFigure>
    {
        /// <summary>
        /// Parses the specified path data to a <see cref="PathFigures"/>.
        /// </summary>
        /// <param name="pathData">The s.</param>
        /// <returns></returns>
        public static PathFigures Parse(string pathData)
        {
            var pathGeometry = new PathGeometry();
            
            using (var context = new PathGeometryContext(pathGeometry))
            using (var parser = new PathMarkupParser(context))
            {
                parser.Parse(pathData);
            }

            return pathGeometry.Figures!;
        }
    }

    /// <summary>
    /// Represents a collection of <see cref="PathSegment"/> objects that can be individually accessed by index.
    /// </summary>
    public sealed class PathSegments : AvaloniaList<PathSegment>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PathSegments"/> class.
        /// </summary>
        public PathSegments()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PathSegments"/> class with the specified collection of <see cref="PathSegment"/> objects.
        /// </summary>
        /// <param name="collection">The collection of <see cref="PathSegment"/> objects that make up the <see cref="PathSegments"/>.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
        public PathSegments(IEnumerable<PathSegment> collection) :
            base(collection)
        {
        }

        /// <summary>
        /// Initializes a new instance of the PathSegments class with the specified capacity,
        /// or the number of PathSegment objects the collection is initially capable of storing.
        /// </summary>
        /// <param name="capacity">The number of <see cref="PathSegment"/> objects that the collection is initially capable of storing.</param>
        public PathSegments(int capacity) :
            base(capacity)
        {
        }
    }
}
