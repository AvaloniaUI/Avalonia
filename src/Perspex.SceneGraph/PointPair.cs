using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perspex
{
    /// <summary>
    /// Defines a PointPair.
    /// </summary>
    public class PointPair
    {
        /// <summary>
        /// The first point.
        /// </summary>
        public Point P1 { get; set; }

        /// <summary>
        /// The second point.
        /// </summary>
        public Point P2 { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PointPair"/> structure.
        /// </summary>
        /// <param name="p1">The first point.</param>
        /// <param name="p2">The second point.</param>
        public PointPair(Point p1, Point p2)
        {
            P1 = p1;
            P2 = p2;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PointPair"/> structure.
        /// </summary>
        /// <param name="x1">The x position of first point.</param>
        /// <param name="y1">The y position of first point.</param>
        /// <param name="x2">The x position of second point.</param>
        /// <param name="y2">The y position of second point.</param>
        public PointPair(double x1, double y1, double x2, double y2)
        {
            P1 = new Point(x1, y1);
            P2 = new Point(x2, y2);
        }
    }
}
