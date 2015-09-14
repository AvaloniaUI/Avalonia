// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;

namespace Perspex
{
    /// <summary>
    /// Defines a rectangle.
    /// </summary>
    public struct Rect
    {
        /// <summary>
        /// The X position.
        /// </summary>
        private readonly double _x;

        /// <summary>
        /// The Y position.
        /// </summary>
        private readonly double _y;

        /// <summary>
        /// The width.
        /// </summary>
        private readonly double _width;

        /// <summary>
        /// The height.
        /// </summary>
        private readonly double _height;

        /// <summary>
        /// Initializes a new instance of the <see cref="Rect"/> structure.
        /// </summary>
        /// <param name="x">The X position.</param>
        /// <param name="y">The Y position.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        public Rect(double x, double y, double width, double height)
        {
            _x = x;
            _y = y;
            _width = width;
            _height = height;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rect"/> structure.
        /// </summary>
        /// <param name="size">The size of the rectangle.</param>
        public Rect(Size size)
        {
            _x = 0;
            _y = 0;
            _width = size.Width;
            _height = size.Height;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rect"/> structure.
        /// </summary>
        /// <param name="position">The position of the rectangle.</param>
        /// <param name="size">The size of the rectangle.</param>
        public Rect(Point position, Size size)
        {
            _x = position.X;
            _y = position.Y;
            _width = size.Width;
            _height = size.Height;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rect"/> structure.
        /// </summary>
        /// <param name="topLeft">The top left position of the rectangle.</param>
        /// <param name="bottomRight">The bottom right position of the rectangle.</param>
        public Rect(Point topLeft, Point bottomRight)
        {
            _x = topLeft.X;
            _y = topLeft.Y;
            _width = bottomRight.X - topLeft.X;
            _height = bottomRight.Y - topLeft.Y;
        }

        /// <summary>
        /// Gets the X position.
        /// </summary>
        public double X => _x;

        /// <summary>
        /// Gets the Y position.
        /// </summary>
        public double Y => _y;

        /// <summary>
        /// Gets the width.
        /// </summary>
        public double Width => _width;

        /// <summary>
        /// Gets the height.
        /// </summary>
        public double Height => _height;

        /// <summary>
        /// Gets the position of the rectangle.
        /// </summary>
        public Point Position => new Point(_x, _y);

        /// <summary>
        /// Gets the size of the rectangle.
        /// </summary>
        public Size Size => new Size(_width, _height);

        /// <summary>
        /// Gets the right position of the rectangle.
        /// </summary>
        public double Right => _x + _width;

        /// <summary>
        /// Gets the bottom position of the rectangle.
        /// </summary>
        public double Bottom => _y + _height;

        /// <summary>
        /// Gets the top left point of the rectangle.
        /// </summary>
        public Point TopLeft => new Point(_x, _y);

        /// <summary>
        /// Gets the top right point of the rectangle.
        /// </summary>
        public Point TopRight => new Point(Right, _y);

        /// <summary>
        /// Gets the bottom left point of the rectangle.
        /// </summary>
        public Point BottomLeft => new Point(_x, Bottom);

        /// <summary>
        /// Gets the bottom right point of the rectangle.
        /// </summary>
        public Point BottomRight => new Point(Right, Bottom);

        /// <summary>
        /// Gets the center point of the rectangle.
        /// </summary>
        public Point Center => new Point(_x + (_width / 2), _y + (_height / 2));

        /// <summary>
        /// Gets a value that indicates whether the rectangle is empty.
        /// </summary>
        public bool IsEmpty => _width == 0 && _height == 0;

        /// <summary>
        /// Checks for equality between two <see cref="Rect"/>s.
        /// </summary>
        /// <param name="left">The first rect.</param>
        /// <param name="right">The second rect.</param>
        /// <returns>True if the rects are equal; otherwise false.</returns>
        public static bool operator ==(Rect left, Rect right)
        {
            return left.Position == right.Position && left.Size == right.Size;
        }

        /// <summary>
        /// Checks for unequality between two <see cref="Rect"/>s.
        /// </summary>
        /// <param name="left">The first rect.</param>
        /// <param name="right">The second rect.</param>
        /// <returns>True if the rects are unequal; otherwise false.</returns>
        public static bool operator !=(Rect left, Rect right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Multiplies a rectangle by a vector.
        /// </summary>
        /// <param name="rect">The rectangle.</param>
        /// <param name="scale">The vector scale.</param>
        /// <returns>The scaled rectangle.</returns>
        public static Rect operator *(Rect rect, Vector scale)
        {
            double centerX = rect._x + (rect._width / 2);
            double centerY = rect._y + (rect._height / 2);
            double width = rect._width * scale.X;
            double height = rect._height * scale.Y;
            return new Rect(
                centerX - (width / 2),
                centerY - (height / 2),
                width,
                height);
        }

        /// <summary>
        /// Transforms a rectangle by a matrix and returns the axis-aligned bounding box.
        /// </summary>
        /// <param name="rect">The rectangle.</param>
        /// <param name="matrix">The matrix.</param>
        /// <returns>The axis-aligned bounding box.</returns>
        public static Rect operator *(Rect rect, Matrix matrix)
        {
            return new Rect(rect.TopLeft * matrix, rect.BottomRight * matrix);
        }

        /// <summary>
        /// Divides a rectangle by a vector.
        /// </summary>
        /// <param name="rect">The rectangle.</param>
        /// <param name="scale">The vector scale.</param>
        /// <returns>The scaled rectangle.</returns>
        public static Rect operator /(Rect rect, Vector scale)
        {
            double centerX = rect._x + (rect._width / 2);
            double centerY = rect._y + (rect._height / 2);
            double width = rect._width / scale.X;
            double height = rect._height / scale.Y;
            return new Rect(
                centerX - (width / 2),
                centerY - (height / 2),
                width,
                height);
        }

        /// <summary>
        /// Determines whether a points in in the bounds of the rectangle.
        /// </summary>
        /// <param name="p">The point.</param>
        /// <returns>true if the point is in the bounds of the rectangle; otherwise false.</returns>
        public bool Contains(Point p)
        {
            return p.X >= _x && p.X < _x + _width &&
                   p.Y >= _y && p.Y < _y + _height;
        }

        /// <summary>
        /// Centers another rectangle in this rectangle.
        /// </summary>
        /// <param name="rect">The rectangle to center.</param>
        /// <returns>The centered rectangle.</returns>
        public Rect CenterIn(Rect rect)
        {
            return new Rect(
                _x + ((_width - rect._width) / 2),
                _y + ((_height - rect._height) / 2),
                rect._width,
                rect._height);
        }

        /// <summary>
        /// Inflates the rectangle.
        /// </summary>
        /// <param name="thickness">The thickness.</param>
        /// <returns>The inflated rectangle.</returns>
        public Rect Inflate(double thickness)
        {
            return Inflate(thickness);
        }

        /// <summary>
        /// Inflates the rectangle.
        /// </summary>
        /// <param name="thickness">The thickness.</param>
        /// <returns>The inflated rectangle.</returns>
        public Rect Inflate(Thickness thickness)
        {
            return new Rect(
                new Point(_x - thickness.Left, _y - thickness.Top),
                Size.Inflate(thickness));
        }

        /// <summary>
        /// Deflates the rectangle.
        /// </summary>
        /// <param name="thickness">The thickness.</param>
        /// <returns>The deflated rectangle.</returns>
        /// <remarks>The deflated rectangle size cannot be less than 0.</remarks>
        public Rect Deflate(double thickness)
        {
            return Deflate(new Thickness(thickness / 2));
        }

        /// <summary>
        /// Deflates the rectangle by a <see cref="Thickness"/>.
        /// </summary>
        /// <param name="thickness">The thickness.</param>
        /// <returns>The deflated rectangle.</returns>
        /// <remarks>The deflated rectangle size cannot be less than 0.</remarks>
        public Rect Deflate(Thickness thickness)
        {
            return new Rect(
                new Point(_x + thickness.Left, _y + thickness.Top),
                Size.Deflate(thickness));
        }

        /// <summary>
        /// Returns a boolean indicating whether the given object is equal to this rectangle.
        /// </summary>
        /// <param name="obj">The object to compare against.</param>
        /// <returns>True if the object is equal to this rectangle; false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (obj is Rect)
            {
                return this == (Rect)obj;
            }

            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + X.GetHashCode();
                hash = (hash * 23) + Y.GetHashCode();
                hash = (hash * 23) + Width.GetHashCode();
                hash = (hash * 23) + Height.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Gets the intersection of two rectangles.
        /// </summary>
        /// <param name="rect">The other rectangle.</param>
        /// <returns>The intersection.</returns>
        public Rect Intersect(Rect rect)
        {
            double x = Math.Max(_x, rect._x);
            double y = Math.Max(_y, rect._y);
            double width = Math.Min(Right, rect.Right) - x;
            double height = Math.Min(Bottom, rect.Bottom) - y;

            if (width < 0 || height < 0)
            {
                return new Rect(
                    double.PositiveInfinity,
                    double.PositiveInfinity,
                    double.NegativeInfinity,
                    double.NegativeInfinity);
            }
            else
            {
                return new Rect(x, y, width, height);
            }
        }

        /// <summary>
        /// Returns the string representation of the rectangle.
        /// </summary>
        /// <returns>The string representation of the rectangle.</returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}, {1}, {2}, {3}",
                _x,
                _y,
                _width,
                _height);
        }
    }
}
