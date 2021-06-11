// Copyright (c) 2009-2015 Math.NET Taken from http://github.com/mathnet/mathnet-numerics and modified for Wasabi Wallet

using System;

namespace MathNet
{
	/// <summary>
	/// Utilities for working with floating point numbers.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Useful links:
	/// <list type="bullet">
	/// <item>
	/// http://docs.sun.com/source/806-3568/ncg_goldberg.html#689 - What every computer scientist should know about floating-point arithmetic
	/// </item>
	/// <item>
	/// http://en.wikipedia.org/wiki/Machine_epsilon - Gives the definition of machine epsilon
	/// </item>
	/// </list>
	/// </para>
	/// </remarks>
	public static partial class Precision
	{
		/// <summary>
		/// The number of binary digits used to represent the binary number for a double precision floating
		/// point value. i.e. there are this many digits used to represent the
		/// actual number, where in a number as: 0.134556 * 10^5 the digits are 0.134556 and the exponent is 5.
		/// </summary>
		private const int DoubleWidth = 53;

		/// <summary>
		/// Standard epsilon, the maximum relative precision of IEEE 754 double-precision floating numbers (64 bit).
		/// According to the definition of Prof. Demmel and used in LAPACK and Scilab.
		/// </summary>
		public static readonly double DoublePrecision = Math.Pow(2, -DoubleWidth);

		/// <summary>
		/// Value representing 10 * 2^(-53) = 1.11022302462516E-15
		/// </summary>
		private static readonly double DefaultDoubleAccuracy = DoublePrecision * 10;
	}
}
