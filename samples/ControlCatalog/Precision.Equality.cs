// Copyright (c) 2009-2013 Math.NET Taken from http://github.com/mathnet/mathnet-numerics and modified for Wasabi Wallet

using System;

namespace MathNet
{
	public static partial class Precision
	{
		/// <summary>
		/// Compares two doubles and determines if they are equal
		/// within the specified maximum absolute error.
		/// </summary>
		/// <param name="a">The norm of the first value (can be negative).</param>
		/// <param name="b">The norm of the second value (can be negative).</param>
		/// <param name="diff">The norm of the difference of the two values (can be negative).</param>
		/// <param name="maximumAbsoluteError">The absolute accuracy required for being almost equal.</param>
		/// <returns>True if both doubles are almost equal up to the specified maximum absolute error, false otherwise.</returns>
		public static bool AlmostEqualNorm(this double a, double b, double diff, double maximumAbsoluteError)
		{
			// If A or B are infinity (positive or negative) then
			// only return true if they are exactly equal to each other -
			// that is, if they are both infinities of the same sign.
			if (double.IsInfinity(a) || double.IsInfinity(b))
			{
				return a == b;
			}

			// If A or B are a NAN, return false. NANs are equal to nothing,
			// not even themselves.
			if (double.IsNaN(a) || double.IsNaN(b))
			{
				return false;
			}

			return Math.Abs(diff) < maximumAbsoluteError;
		}

		/// <summary>
		/// Checks whether two real numbers are almost equal.
		/// </summary>
		/// <param name="a">The first number</param>
		/// <param name="b">The second number</param>
		/// <returns>true if the two values differ by no more than 10 * 2^(-52); false otherwise.</returns>
		public static bool AlmostEqual(this double a, double b)
		{
			return AlmostEqualNorm(a, b, a - b, DefaultDoubleAccuracy);
		}
	}
}
