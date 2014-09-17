// -----------------------------------------------------------------------
// <copyright file="Thickness.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;

    public struct Thickness
    {
        /// <summary>
        /// The thickness on the left.
        /// </summary>
        private double left;

        /// <summary>
        /// The thickness on the top.
        /// </summary>
        private double top;

        /// <summary>
        /// The thickness on the right.
        /// </summary>
        private double right;

        /// <summary>
        /// The thickness on the bottom.
        /// </summary>
        private double bottom;

        /// <summary>
        /// Initializes a new instance of the <see cref="Thickness"/> structure.
        /// </summary>
        /// <param name="uniformLength">The length that should be applied to all sides.</param>
        public Thickness(double uniformLength)
        {
            Contract.Requires<ArgumentException>(uniformLength >= 0);

            this.left = this.top = this.right = this.bottom = uniformLength;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Thickness"/> structure.
        /// </summary>
        /// <param name="horizontal">The thickness on the left and right.</param>
        /// <param name="top">The thickness on the top and bottom.</param>
        public Thickness(double horizontal, double vertical)
        {
            Contract.Requires<ArgumentException>(horizontal >= 0);
            Contract.Requires<ArgumentException>(vertical >= 0);

            this.left = this.right = horizontal;
            this.top = this.bottom = vertical;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Thickness"/> structure.
        /// </summary>
        /// <param name="left">The thickness on the left.</param>
        /// <param name="top">The thickness on the top.</param>
        /// <param name="right">The thickness on the right.</param>
        /// <param name="bottom">The thickness on the bottom.</param>
        public Thickness(double left, double top, double right, double bottom)
        {
            Contract.Requires<ArgumentException>(left >= 0);
            Contract.Requires<ArgumentException>(top >= 0);
            Contract.Requires<ArgumentException>(right >= 0);
            Contract.Requires<ArgumentException>(bottom >= 0);

            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }

        /// <summary>
        /// Gets the thickness on the left.
        /// </summary>
        public double Left
        {
            get { return this.left; }
        }

        /// <summary>
        /// Gets the thickness on the top.
        /// </summary>
        public double Top
        {
            get { return this.top; }
        }

        /// <summary>
        /// Gets the thickness on the right.
        /// </summary>
        public double Right
        {
            get { return this.right; }
        }

        /// <summary>
        /// Gets the thickness on the bottom.
        /// </summary>
        public double Bottom
        {
            get { return this.bottom; }
        }

        /// <summary>
        /// Gets a value indicating whether all sides are set to 0.
        /// </summary>
        public bool IsEmpty
        {
            get { return this.Left == 0 && this.Top == 0 && this.Right == 0 && this.Bottom == 0; }
        }

        /// <summary>
        /// Compares two Thicknesses.
        /// </summary>
        /// <param name="a">The first thickness.</param>
        /// <param name="b">The second thickness.</param>
        /// <returns>The equality.</returns>
        public static bool operator ==(Thickness a, Thickness b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Compares two Thicknesses.
        /// </summary>
        /// <param name="a">The first thickness.</param>
        /// <param name="b">The second thickness.</param>
        /// <returns>The unequality.</returns>
        public static bool operator !=(Thickness a, Thickness b)
        {
            return !a.Equals(b);
        }

        /// <summary>
        /// Adds two Thicknesses.
        /// </summary>
        /// <param name="a">The first thickness.</param>
        /// <param name="b">The second thickness.</param>
        /// <returns>The equality.</returns>
        public static Thickness operator +(Thickness a, Thickness b)
        {
            return new Thickness(
                a.Left + b.Left,
                a.Top + b.Top,
                a.Right + b.Right,
                a.Bottom + b.Bottom);
        }

        public override bool Equals(object obj)
        {
            if (obj is Thickness)
            {
                Thickness other = (Thickness)obj;
                return this.Left == other.Left &&
                       this.Top == other.Top &&
                       this.Right == other.Right &&
                       this.Bottom == other.Bottom;
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + this.Left.GetHashCode();
                hash = (hash * 23) + this.Top.GetHashCode();
                hash = (hash * 23) + this.Right.GetHashCode();
                hash = (hash * 23) + this.Bottom.GetHashCode();
                return hash;
            }
        }
    }
}
