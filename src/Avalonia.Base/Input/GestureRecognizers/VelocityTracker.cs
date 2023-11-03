// Code in this file is derived from
// https://github.com/flutter/flutter/blob/master/packages/flutter/lib/src/gestures/velocity_tracker.dart

using System;
using System.Diagnostics;
using Avalonia.Utilities;

namespace Avalonia.Input.GestureRecognizers
{
    // Possible enhancement: add Flutter's 'IOSScrollViewFlingVelocityTracker' and 'MacOSScrollViewFlingVelocityTracker'?

    internal readonly record struct Velocity(Vector PixelsPerSecond)
    {
        public Velocity ClampMagnitude(double minValue, double maxValue)
        {
            Debug.Assert(minValue >= 0.0);
            Debug.Assert(maxValue >= 0.0 && maxValue >= minValue);
            double valueSquared = PixelsPerSecond.SquaredLength;
            if (valueSquared > maxValue * maxValue)
            {
                double length = PixelsPerSecond.Length;
                return new Velocity(length != 0.0 ? (PixelsPerSecond / length) * maxValue : Vector.Zero);
                // preventing double.NaN in Vector PixelsPerSecond is important -- if a NaN eventually gets into a
                // ScrollGestureEventArgs it results in runtime errors.
            }
            if (valueSquared < minValue * minValue)
            {
                double length = PixelsPerSecond.Length;
                return new Velocity(length != 0.0 ? (PixelsPerSecond / length) * minValue : Vector.Zero);
            }
            return this;
        }
    }

    /// A two dimensional velocity estimate.
    ///
    /// VelocityEstimates are computed by [VelocityTracker.getVelocityEstimate]. An
    /// estimate's [confidence] measures how well the velocity tracker's position
    /// data fit a straight line, [duration] is the time that elapsed between the
    /// first and last position sample used to compute the velocity, and [offset]
    /// is similarly the difference between the first and last positions.
    ///
    /// See also:
    ///
    ///  * [VelocityTracker], which computes [VelocityEstimate]s.
    ///  * [Velocity], which encapsulates (just) a velocity vector and provides some
    ///    useful velocity operations.
    internal record VelocityEstimate(Vector PixelsPerSecond, double Confidence, TimeSpan Duration, Vector Offset);

    internal record struct PointAtTime(bool Valid, Vector Point, TimeSpan Time);

    /// Computes a pointer's velocity based on data from [PointerMoveEvent]s.
    ///
    /// The input data is provided by calling [addPosition]. Adding data is cheap.
    ///
    /// To obtain a velocity, call [getVelocity] or [getVelocityEstimate]. This will
    /// compute the velocity based on the data added so far. Only call these when
    /// you need to use the velocity, as they are comparatively expensive.
    ///
    /// The quality of the velocity estimation will be better if more data points
    /// have been received.
    internal class VelocityTracker
    {
        private const int AssumePointerMoveStoppedMilliseconds = 40;
        private const int HistorySize = 20;
        private const int HorizonMilliseconds = 100;
        private const int MinSampleSize = 3;
        private const double MinFlingVelocity = 50.0; // Logical pixels / second
        private const double MaxFlingVelocity = 8000.0;

        private readonly PointAtTime[] _samples = new PointAtTime[HistorySize];
        private int _index = 0;

        /// <summary>
        /// Adds a position as the given time to the tracker.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="position"></param>
        public void AddPosition(TimeSpan time, Vector position)
        {
            _index++;
            if (_index == HistorySize)
            {
                _index = 0;
            }
            _samples[_index] = new PointAtTime(true, position, time);
        }

        /// Returns an estimate of the velocity of the object being tracked by the
        /// tracker given the current information available to the tracker.
        ///
        /// Information is added using [addPosition].
        ///
        /// Returns null if there is no data on which to base an estimate.
        protected virtual VelocityEstimate? GetVelocityEstimate()
        {
            Span<double> x = stackalloc double[HistorySize];
            Span<double> y = stackalloc double[HistorySize];
            Span<double> w = stackalloc double[HistorySize];
            Span<double> time = stackalloc double[HistorySize];
            int sampleCount = 0;
            int index = _index;

            var newestSample = _samples[index];
            if (!newestSample.Valid)
            {
                return null;
            }

            var previousSample = newestSample;
            var oldestSample = newestSample;

            // Starting with the most recent PointAtTime sample, iterate backwards while
            // the samples represent continuous motion.
            do
            {
                var sample = _samples[index];
                if (!sample.Valid)
                {
                    break;
                }

                double age = (newestSample.Time - sample.Time).TotalMilliseconds;
                double delta = Math.Abs((sample.Time - previousSample.Time).TotalMilliseconds);
                previousSample = sample;
                if (age > HorizonMilliseconds || delta > AssumePointerMoveStoppedMilliseconds)
                {
                    break;
                }

                oldestSample = sample;
                var position = sample.Point;
                x[sampleCount] = position.X;
                y[sampleCount] = position.Y;
                w[sampleCount] = 1.0;
                time[sampleCount] = -age;
                index = (index == 0 ? HistorySize : index) - 1;

                sampleCount++;
            } while (sampleCount < HistorySize);

            if (sampleCount >= MinSampleSize)
            {
                var xFit = LeastSquaresSolver.Solve(2, time.Slice(0, sampleCount), x.Slice(0, sampleCount), w.Slice(0, sampleCount));
                if (xFit != null)
                {
                    var yFit = LeastSquaresSolver.Solve(2, time.Slice(0, sampleCount), y.Slice(0, sampleCount), w.Slice(0, sampleCount));
                    if (yFit != null)
                    {
                        return new VelocityEstimate( // convert from pixels/ms to pixels/s
                          PixelsPerSecond: new Vector(xFit.Coefficients[1] * 1000, yFit.Coefficients[1] * 1000),
                          Confidence: xFit.Confidence * yFit.Confidence,
                          Duration: newestSample.Time - oldestSample.Time,
                          Offset: newestSample.Point - oldestSample.Point
                        );
                    }
                }
            }

            // We're unable to make a velocity estimate but we did have at least one
            // valid pointer position.
            return new VelocityEstimate(
              PixelsPerSecond: Vector.Zero,
              Confidence: 1.0,
              Duration: newestSample.Time - oldestSample.Time,
              Offset: newestSample.Point - oldestSample.Point
            );
        }

        /// <summary>
        /// Computes the velocity of the pointer at the time of the last
        /// provided data point.
        ///
        /// This can be expensive. Only call this when you need the velocity.
        ///
        /// Returns [Velocity.zero] if there is no data from which to compute an
        /// estimate or if the estimated velocity is zero./// 
        /// </summary>
        /// <returns></returns>
        internal Velocity GetVelocity()
        {
            var estimate = GetVelocityEstimate();
            if (estimate == null || estimate.PixelsPerSecond == default(Vector))
            {
                return new Velocity(Vector.Zero);
            }
            return new Velocity(estimate.PixelsPerSecond);
        }

        internal virtual Velocity GetFlingVelocity()
        {
            return GetVelocity().ClampMagnitude(MinFlingVelocity, MaxFlingVelocity);
        }
    }

    /// An nth degree polynomial fit to a dataset.
    internal sealed class PolynomialFit
    {
        /// Creates a polynomial fit of the given degree.
        ///
        /// There are n + 1 coefficients in a fit of degree n.
        internal PolynomialFit(int degree)
        {
            Coefficients = new double[degree + 1];
        }

        /// The polynomial coefficients of the fit.
        public double[] Coefficients { get; }

        /// An indicator of the quality of the fit.
        ///
        /// Larger values indicate greater quality.
        public double Confidence { get; set; }
    }

    internal sealed class LeastSquaresSolver
    {
        private const double PrecisionErrorTolerance = 1e-10;

        /// <summary>
        /// Fits a polynomial of the given degree to the data points.
        /// When there is not enough data to fit a curve null is returned.
        /// </summary>
        public static PolynomialFit? Solve(int degree, ReadOnlySpan<double> x, ReadOnlySpan<double> y, ReadOnlySpan<double> w)
        {
            if (degree > x.Length)
            {
                // Not enough data to fit a curve.
                return null;
            }

            PolynomialFit result = new PolynomialFit(degree);

            // Shorthands for the purpose of notation equivalence to original C++ code.
            int m = x.Length;
            int n = degree + 1;

            // Expand the X vector to a matrix A, pre-multiplied by the weights.
            _Matrix a = new _Matrix(m, stackalloc double[n * m]);
            for (int h = 0; h < m; h += 1)
            {
                a[0, h] = w[h];
                for (int i = 1; i < n; i += 1)
                {
                    a[i, h] = a[i - 1, h] * x[h];
                }
            }

            // Apply the Gram-Schmidt process to A to obtain its QR decomposition.

            // Orthonormal basis, column-major order Vector.
            _Matrix q = new _Matrix(m, stackalloc double[n * m]);
            // Upper triangular matrix, row-major order.
            _Matrix r = new _Matrix(n, stackalloc double[n * n]);
            for (int j = 0; j < n; j += 1)
            {
                for (int h = 0; h < m; h += 1)
                {
                    q[j, h] = a[j, h];
                }
                for (int i = 0; i < j; i += 1)
                {
                    double dot = Multiply(q.GetRow(j), q.GetRow(i));
                    for (int h = 0; h < m; h += 1)
                    {
                        q[j, h] = q[j, h] - dot * q[i, h];
                    }
                }

                double norm = Norm(q.GetRow(j));
                if (norm < PrecisionErrorTolerance)
                {
                    // Vectors are linearly dependent or zero so no solution.
                    return null;
                }

                double inverseNorm = 1.0 / norm;
                for (int h = 0; h < m; h += 1)
                {
                    q[j, h] = q[j, h] * inverseNorm;
                }
                for (int i = 0; i < n; i += 1)
                {
                    r[j, i] = i < j ? 0.0 : Multiply(q.GetRow(j), a.GetRow(i));
                }
            }

            // Solve R B = Qt W Y to find B. This is easy because R is upper triangular.
            // We just work from bottom-right to top-left calculating B's coefficients.
            // "m" isn't expected to be bigger than HistorySize=20, so allocation on stack is safe.
            Span<double> wy = stackalloc double[m];  
            for (int h = 0; h < m; h += 1)
            {
                wy[h] = y[h] * w[h];
            }
            for (int i = n - 1; i >= 0; i -= 1)
            {
                result.Coefficients[i] = Multiply(q.GetRow(i), wy);
                for (int j = n - 1; j > i; j -= 1)
                {
                    result.Coefficients[i] -= r[i, j] * result.Coefficients[j];
                }
                result.Coefficients[i] /= r[i, i];
            }

            // Calculate the coefficient of determination (confidence) as:
            //   1 - (sumSquaredError / sumSquaredTotal)
            // ...where sumSquaredError is the residual sum of squares (variance of the
            // error), and sumSquaredTotal is the total sum of squares (variance of the
            // data) where each has been weighted.
            double yMean = 0.0;
            for (int h = 0; h < m; h += 1)
            {
                yMean += y[h];
            }
            yMean /= m;

            double sumSquaredError = 0.0;
            double sumSquaredTotal = 0.0;
            for (int h = 0; h < m; h += 1)
            {
                double term = 1.0;
                double err = y[h] - result.Coefficients[0];
                for (int i = 1; i < n; i += 1)
                {
                    term *= x[h];
                    err -= term * result.Coefficients[i];
                }
                sumSquaredError += w[h] * w[h] * err * err;
                double v = y[h] - yMean;
                sumSquaredTotal += w[h] * w[h] * v * v;
            }

            result.Confidence = sumSquaredTotal <= PrecisionErrorTolerance ? 1.0 :
                                  1.0 - (sumSquaredError / sumSquaredTotal);

            return result;
        }

        private static double Multiply(Span<double> v1, Span<double> v2)
        {
            double result = 0.0;
            for (int i = 0; i < v1.Length; i += 1)
            {
                result += v1[i] * v2[i];
            }
            return result;
        }
        
        private static double Norm(Span<double> v)
        {
            return Math.Sqrt(Multiply(v, v));
        }

        private readonly ref struct _Matrix
        {
            private readonly int _columns;
            private readonly Span<double> _elements;

            internal _Matrix(int cols, Span<double> elements)
            {
                _columns = cols;
                _elements = elements;
            }

            public double this[int row, int col]
            {
                get => _elements[row * _columns + col];
                set => _elements[row * _columns + col] = value;
            }

            public Span<double> GetRow(int row) => _elements.Slice(row * _columns, _columns);
        }
    }
}
