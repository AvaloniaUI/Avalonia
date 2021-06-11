// Copyright (c) 2009-2014 Math.NET Taken from http://github.com/mathnet/mathnet-numerics and modified for Wasabi Wallet

using System;

namespace MathNet
{
	/// <summary>
	/// Cubic Spline Interpolation.
	/// </summary>
	/// <remarks>Supports both differentiation and integration.</remarks>
	public class CubicSpline
	{
		private readonly double[] _x;
		private readonly double[] _c0;
		private readonly double[] _c1;
		private readonly double[] _c2;
		private readonly double[] _c3;
		private readonly Lazy<double[]> _indefiniteIntegral;

		/// <param name="x">Sample points (N+1), sorted ascending</param>
		/// <param name="c0">Zero order spline coefficients (N)</param>
		/// <param name="c1">First order spline coefficients (N)</param>
		/// <param name="c2">Second order spline coefficients (N)</param>
		/// <param name="c3">Third order spline coefficients (N)</param>
		public CubicSpline(double[] x, double[] c0, double[] c1, double[] c2, double[] c3)
		{
			if (x.Length != c0.Length + 1 || x.Length != c1.Length + 1 || x.Length != c2.Length + 1 || x.Length != c3.Length + 1)
			{
				throw new ArgumentException("All vectors must have the same dimensionality.");
			}

			if (x.Length < 2)
			{
				throw new ArgumentException("The given array is too small. It must be at least 2 long.", nameof(x));
			}

			_x = x;
			_c0 = c0;
			_c1 = c1;
			_c2 = c2;
			_c3 = c3;
			_indefiniteIntegral = new Lazy<double[]>(ComputeIndefiniteIntegral);
		}

		/// <summary>
		/// Create a Hermite cubic spline interpolation from a set of (x,y) value pairs and their slope (first derivative), sorted ascendingly by x.
		/// </summary>
		public static CubicSpline InterpolateHermiteSorted(double[] x, double[] y, double[] firstDerivatives)
		{
			if (x.Length != y.Length || x.Length != firstDerivatives.Length)
			{
				throw new ArgumentException("All vectors must have the same dimensionality.");
			}

			if (x.Length < 2)
			{
				throw new ArgumentException("The given array is too small. It must be at least 2 long.", nameof(x));
			}

			var c0 = new double[x.Length - 1];
			var c1 = new double[x.Length - 1];
			var c2 = new double[x.Length - 1];
			var c3 = new double[x.Length - 1];
			for (int i = 0; i < c1.Length; i++)
			{
				double w = x[i + 1] - x[i];
				double w2 = w * w;
				c0[i] = y[i];
				c1[i] = firstDerivatives[i];
				c2[i] = (3 * (y[i + 1] - y[i]) / w - 2 * firstDerivatives[i] - firstDerivatives[i + 1]) / w;
				c3[i] = (2 * (y[i] - y[i + 1]) / w + firstDerivatives[i] + firstDerivatives[i + 1]) / w2;
			}

			return new CubicSpline(x, c0, c1, c2, c3);
		}

		/// <summary>
		/// Create an Akima cubic spline interpolation from a set of (x,y) value pairs, sorted ascendingly by x.
		/// Akima splines are robust to outliers.
		/// </summary>
		public static CubicSpline InterpolateAkimaSorted(double[] x, double[] y)
		{
			if (x.Length != y.Length)
			{
				throw new ArgumentException("All vectors must have the same dimensionality.");
			}

			if (x.Length < 5)
			{
				throw new ArgumentException("The given array is too small. It must be at least 5 long.", nameof(x));
			}

			/* Prepare divided differences (diff) and weights (w) */

			var diff = new double[x.Length - 1];
			var weights = new double[x.Length - 1];

			for (int i = 0; i < diff.Length; i++)
			{
				diff[i] = (y[i + 1] - y[i]) / (x[i + 1] - x[i]);
			}

			for (int i = 1; i < weights.Length; i++)
			{
				weights[i] = Math.Abs(diff[i] - diff[i - 1]);
			}

			/* Prepare Hermite interpolation scheme */

			var dd = new double[x.Length];

			for (int i = 2; i < dd.Length - 2; i++)
			{
				dd[i] = weights[i - 1].AlmostEqual(0.0) && weights[i + 1].AlmostEqual(0.0)
					? (((x[i + 1] - x[i]) * diff[i - 1]) + ((x[i] - x[i - 1]) * diff[i])) / (x[i + 1] - x[i - 1])
					: ((weights[i + 1] * diff[i - 1]) + (weights[i - 1] * diff[i])) / (weights[i + 1] + weights[i - 1]);
			}

			dd[0] = DifferentiateThreePoint(x, y, 0, 0, 1, 2);
			dd[1] = DifferentiateThreePoint(x, y, 1, 0, 1, 2);
			dd[x.Length - 2] = DifferentiateThreePoint(x, y, x.Length - 2, x.Length - 3, x.Length - 2, x.Length - 1);
			dd[x.Length - 1] = DifferentiateThreePoint(x, y, x.Length - 1, x.Length - 3, x.Length - 2, x.Length - 1);

			/* Build Akima spline using Hermite interpolation scheme */

			return InterpolateHermiteSorted(x, y, dd);
		}

		/// <summary>
		/// Create a piecewise cubic Hermite interpolating polynomial from an unsorted set of (x,y) value pairs.
		/// Monotone-preserving interpolation with continuous first derivative.
		/// </summary>
		public static CubicSpline InterpolatePchipSorted(double[] x, double[] y)
		{
			// Implementation based on "Numerical Computing with Matlab" (Moler, 2004).

			if (x.Length != y.Length)
			{
				throw new ArgumentException("All vectors must have the same dimensionality.");
			}

			if (x.Length < 3)
			{
				throw new ArgumentException("The given array is too small. It must be at least 3 long.", nameof(x));
			}

			var m = new double[x.Length - 1];

			for (int i = 0; i < m.Length; i++)
			{
				m[i] = (y[i + 1] - y[i]) / (x[i + 1] - x[i]);
			}

			var dd = new double[x.Length];
			var hPrev = x[1] - x[0];

			// This check is quite costly as it usually involves a Math.Pow().
			var mPrevIs0 = m[0].AlmostEqual(0.0);

			for (var i = 1; i < x.Length - 1; ++i)
			{
				var h = x[i + 1] - x[i];
				var mIs0 = m[i].AlmostEqual(0.0);

				if (mIs0 || mPrevIs0 || Math.Sign(m[i]) != Math.Sign(m[i - 1]))
				{
					dd[i] = 0;
				}
				else
				{
					// Weighted harmonic mean of each slope.
					var w1 = 2 * h + hPrev;
					var w2 = h + 2 * hPrev;
					dd[i] = (w1 + w2) / (w1 / m[i - 1] + w2 / m[i]);
				}

				hPrev = h;
				mPrevIs0 = mIs0;
			}

			// Special case end-points.
			dd[0] = PchipEndPoints(x[1] - x[0], x[2] - x[1], m[0], m[1]);
			dd[dd.Length - 1] = PchipEndPoints(
				x[x.Length - 1] - x[x.Length - 2],
				x[x.Length - 2] - x[x.Length - 3],
				m[m.Length - 1],
				m[m.Length - 2]);

			return InterpolateHermiteSorted(x, y, dd);
		}

		private static double PchipEndPoints(double h0, double h1, double m0, double m1)
		{
			// One-sided, shape-preserving, three-point estimate for the derivative.
			var d = ((2 * h0 + h1) * m0 - h0 * m1) / (h0 + h1);

			if (Math.Sign(d) != Math.Sign(m0))
			{
				return 0.0;
			}

			if (Math.Sign(m0) != Math.Sign(m1) && (Math.Abs(d) > 3 * Math.Abs(m0)))
			{
				return 3 * m0;
			}

			return d;
		}

		/// <summary>
		/// Create a cubic spline interpolation from a set of (x,y) value pairs, sorted ascendingly by x,
		/// and custom boundary/termination conditions.
		/// </summary>
		public static CubicSpline InterpolateBoundariesSorted(
			double[] x,
			double[] y,
			SplineBoundaryCondition leftBoundaryCondition,
			double leftBoundary,
			SplineBoundaryCondition rightBoundaryCondition,
			double rightBoundary)
		{
			if (x.Length != y.Length)
			{
				throw new ArgumentException("All vectors must have the same dimensionality.");
			}

			if (x.Length < 2)
			{
				throw new ArgumentException("The given array is too small. It must be at least 2 long.", nameof(x));
			}

			int n = x.Length;

			// normalize special cases
			if ((n == 2)
				&& (leftBoundaryCondition == SplineBoundaryCondition.ParabolicallyTerminated)
				&& (rightBoundaryCondition == SplineBoundaryCondition.ParabolicallyTerminated))
			{
				leftBoundaryCondition = SplineBoundaryCondition.SecondDerivative;
				leftBoundary = 0d;
				rightBoundaryCondition = SplineBoundaryCondition.SecondDerivative;
				rightBoundary = 0d;
			}

			if (leftBoundaryCondition == SplineBoundaryCondition.Natural)
			{
				leftBoundaryCondition = SplineBoundaryCondition.SecondDerivative;
				leftBoundary = 0d;
			}

			if (rightBoundaryCondition == SplineBoundaryCondition.Natural)
			{
				rightBoundaryCondition = SplineBoundaryCondition.SecondDerivative;
				rightBoundary = 0d;
			}

			var a1 = new double[n];
			var a2 = new double[n];
			var a3 = new double[n];
			var b = new double[n];

			// Left Boundary
			switch (leftBoundaryCondition)
			{
				case SplineBoundaryCondition.ParabolicallyTerminated:
					a1[0] = 0;
					a2[0] = 1;
					a3[0] = 1;
					b[0] = 2 * (y[1] - y[0]) / (x[1] - x[0]);
					break;
				case SplineBoundaryCondition.FirstDerivative:
					a1[0] = 0;
					a2[0] = 1;
					a3[0] = 0;
					b[0] = leftBoundary;
					break;
				case SplineBoundaryCondition.SecondDerivative:
					a1[0] = 0;
					a2[0] = 2;
					a3[0] = 1;
					b[0] = (3 * ((y[1] - y[0]) / (x[1] - x[0]))) - (0.5 * leftBoundary * (x[1] - x[0]));
					break;
				default:
					throw new NotSupportedException("Invalid Left Boundary Condition.");
			}

			// Central Conditions
			for (int i = 1; i < x.Length - 1; i++)
			{
				a1[i] = x[i + 1] - x[i];
				a2[i] = 2 * (x[i + 1] - x[i - 1]);
				a3[i] = x[i] - x[i - 1];
				b[i] = (3 * (y[i] - y[i - 1]) / (x[i] - x[i - 1]) * (x[i + 1] - x[i])) + (3 * (y[i + 1] - y[i]) / (x[i + 1] - x[i]) * (x[i] - x[i - 1]));
			}

			// Right Boundary
			switch (rightBoundaryCondition)
			{
				case SplineBoundaryCondition.ParabolicallyTerminated:
					a1[n - 1] = 1;
					a2[n - 1] = 1;
					a3[n - 1] = 0;
					b[n - 1] = 2 * (y[n - 1] - y[n - 2]) / (x[n - 1] - x[n - 2]);
					break;
				case SplineBoundaryCondition.FirstDerivative:
					a1[n - 1] = 0;
					a2[n - 1] = 1;
					a3[n - 1] = 0;
					b[n - 1] = rightBoundary;
					break;
				case SplineBoundaryCondition.SecondDerivative:
					a1[n - 1] = 1;
					a2[n - 1] = 2;
					a3[n - 1] = 0;
					b[n - 1] = (3 * (y[n - 1] - y[n - 2]) / (x[n - 1] - x[n - 2])) + (0.5 * rightBoundary * (x[n - 1] - x[n - 2]));
					break;
				default:
					throw new NotSupportedException("Invalid Right Boundary Condition.");
			}

			// Build Spline
			double[] dd = SolveTridiagonal(a1, a2, a3, b);
			return InterpolateHermiteSorted(x, y, dd);
		}

		/// <summary>
		/// Create a natural cubic spline interpolation from a set of (x,y) value pairs
		/// and zero second derivatives at the two boundaries, sorted ascendingly by x.
		/// </summary>
		public static CubicSpline InterpolateNaturalSorted(double[] x, double[] y)
		{
			return InterpolateBoundariesSorted(x, y, SplineBoundaryCondition.SecondDerivative, 0.0, SplineBoundaryCondition.SecondDerivative, 0.0);
		}

		/// <summary>
		/// Three-Point Differentiation Helper.
		/// </summary>
		/// <param name="xx">Sample Points t.</param>
		/// <param name="yy">Sample Values x(t).</param>
		/// <param name="indexT">Index of the point of the differentiation.</param>
		/// <param name="index0">Index of the first sample.</param>
		/// <param name="index1">Index of the second sample.</param>
		/// <param name="index2">Index of the third sample.</param>
		/// <returns>The derivative approximation.</returns>
		private static double DifferentiateThreePoint(double[] xx, double[] yy, int indexT, int index0, int index1, int index2)
		{
			double x0 = yy[index0];
			double x1 = yy[index1];
			double x2 = yy[index2];

			double t = xx[indexT] - xx[index0];
			double t1 = xx[index1] - xx[index0];
			double t2 = xx[index2] - xx[index0];

			double a = (x2 - x0 - (t2 / t1 * (x1 - x0))) / (t2 * (t2 - t1));
			double b = (x1 - x0 - a * t1 * t1) / t1;
			return (2 * a * t) + b;
		}

		/// <summary>
		/// Tridiagonal Solve Helper.
		/// </summary>
		/// <param name="a">The a-vector[n].</param>
		/// <param name="b">The b-vector[n], will be modified by this function.</param>
		/// <param name="c">The c-vector[n].</param>
		/// <param name="d">The d-vector[n], will be modified by this function.</param>
		/// <returns>The x-vector[n]</returns>
		private static double[] SolveTridiagonal(double[] a, double[] b, double[] c, double[] d)
		{
			for (int k = 1; k < a.Length; k++)
			{
				double t = a[k] / b[k - 1];
				b[k] = b[k] - (t * c[k - 1]);
				d[k] = d[k] - (t * d[k - 1]);
			}

			var x = new double[a.Length];
			x[x.Length - 1] = d[d.Length - 1] / b[b.Length - 1];
			for (int k = x.Length - 2; k >= 0; k--)
			{
				x[k] = (d[k] - (c[k] * x[k + 1])) / b[k];
			}

			return x;
		}

		/// <summary>
		/// Interpolate at point t.
		/// </summary>
		/// <param name="t">Point t to interpolate at.</param>
		/// <returns>Interpolated value x(t).</returns>
		public double Interpolate(double t)
		{
			int k = LeftSegmentIndex(t);
			var x = t - _x[k];
			return _c0[k] + x * (_c1[k] + x * (_c2[k] + x * _c3[k]));
		}

		/// <summary>
		/// Differentiate at point t.
		/// </summary>
		/// <param name="t">Point t to interpolate at.</param>
		/// <returns>Interpolated first derivative at point t.</returns>
		public double Differentiate(double t)
		{
			int k = LeftSegmentIndex(t);
			var x = t - _x[k];
			return _c1[k] + x * (2 * _c2[k] + x * 3 * _c3[k]);
		}

		/// <summary>
		/// Differentiate twice at point t.
		/// </summary>
		/// <param name="t">Point t to interpolate at.</param>
		/// <returns>Interpolated second derivative at point t.</returns>
		public double Differentiate2(double t)
		{
			int k = LeftSegmentIndex(t);
			var x = t - _x[k];
			return 2 * _c2[k] + x * 6 * _c3[k];
		}

		/// <summary>
		/// Indefinite integral at point t.
		/// </summary>
		/// <param name="t">Point t to integrate at.</param>
		public double Integrate(double t)
		{
			int k = LeftSegmentIndex(t);
			var x = t - _x[k];
			return _indefiniteIntegral.Value[k] + x * (_c0[k] + x * (_c1[k] / 2 + x * (_c2[k] / 3 + x * _c3[k] / 4)));
		}

		/// <summary>
		/// Definite integral between points a and b.
		/// </summary>
		/// <param name="a">Left bound of the integration interval [a,b].</param>
		/// <param name="b">Right bound of the integration interval [a,b].</param>
		public double Integrate(double a, double b)
		{
			return Integrate(b) - Integrate(a);
		}

		private double[] ComputeIndefiniteIntegral()
		{
			var integral = new double[_c1.Length];
			for (int i = 0; i < integral.Length - 1; i++)
			{
				double w = _x[i + 1] - _x[i];
				integral[i + 1] = integral[i] + w * (_c0[i] + w * (_c1[i] / 2 + w * (_c2[i] / 3 + w * _c3[i] / 4)));
			}

			return integral;
		}

		/// <summary>
		/// Find the index of the greatest sample point smaller than t,
		/// or the left index of the closest segment for extrapolation.
		/// </summary>
		private int LeftSegmentIndex(double t)
		{
			int index = Array.BinarySearch(_x, t);
			if (index < 0)
			{
				index = ~index - 1;
			}

			return Math.Min(Math.Max(index, 0), _x.Length - 2);
		}
	}
}
