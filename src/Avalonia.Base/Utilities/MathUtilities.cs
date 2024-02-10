using System;
using System.Runtime.CompilerServices;

namespace Avalonia.Utilities
{
    /// <summary>
    /// Provides math utilities not provided in System.Math.
    /// </summary>
#if !BUILDTASK
    public
#endif
    static class MathUtilities
    {
        // smallest such that 1.0+DoubleEpsilon != 1.0
        internal const double DoubleEpsilon = 2.2204460492503131e-016;

        private const float FloatEpsilon = 1.192092896e-07F;

        /// <summary>
        /// AreClose - Returns whether or not two doubles are "close".  That is, whether or 
        /// not they are within epsilon of each other.
        /// </summary> 
        /// <param name="value1"> The first double to compare. </param>
        /// <param name="value2"> The second double to compare. </param>
        public static bool AreClose(double value1, double value2)
        {
            //in case they are Infinities (then epsilon check does not work)
            if (value1 == value2) return true;
            double eps = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * DoubleEpsilon;
            double delta = value1 - value2;
            return (-eps < delta) && (eps > delta);
        }

        /// <summary>
        /// AreClose - Returns whether or not two doubles are "close".  That is, whether or
        /// not they are within epsilon of each other.
        /// </summary>
        /// <param name="value1"> The first double to compare. </param>
        /// <param name="value2"> The second double to compare. </param>
        /// <param name="eps"> The fixed epsilon value used to compare.</param>
        public static bool AreClose(double value1, double value2, double eps)
        {
            //in case they are Infinities (then epsilon check does not work)
            if (value1 == value2) return true;
            double delta = value1 - value2;
            return (-eps < delta) && (eps > delta);
        }

        /// <summary>
        /// AreClose - Returns whether or not two floats are "close".  That is, whether or 
        /// not they are within epsilon of each other.
        /// </summary> 
        /// <param name="value1"> The first float to compare. </param>
        /// <param name="value2"> The second float to compare. </param>
        public static bool AreClose(float value1, float value2)
        {
            //in case they are Infinities (then epsilon check does not work)
            if (value1 == value2) return true;
            float eps = (Math.Abs(value1) + Math.Abs(value2) + 10.0f) * FloatEpsilon;
            float delta = value1 - value2;
            return (-eps < delta) && (eps > delta);
        }

        /// <summary>
        /// LessThan - Returns whether or not the first double is less than the second double.
        /// That is, whether or not the first is strictly less than *and* not within epsilon of
        /// the other number.
        /// </summary>
        /// <param name="value1"> The first double to compare. </param>
        /// <param name="value2"> The second double to compare. </param>
        public static bool LessThan(double value1, double value2)
        {
            return (value1 < value2) && !AreClose(value1, value2);
        }

        /// <summary>
        /// LessThan - Returns whether or not the first float is less than the second float.
        /// That is, whether or not the first is strictly less than *and* not within epsilon of
        /// the other number.
        /// </summary>
        /// <param name="value1"> The first single float to compare. </param>
        /// <param name="value2"> The second single float to compare. </param>
        public static bool LessThan(float value1, float value2)
        {
            return (value1 < value2) && !AreClose(value1, value2);
        }

        /// <summary>
        /// GreaterThan - Returns whether or not the first double is greater than the second double.
        /// That is, whether or not the first is strictly greater than *and* not within epsilon of
        /// the other number.
        /// </summary>
        /// <param name="value1"> The first double to compare. </param>
        /// <param name="value2"> The second double to compare. </param>
        public static bool GreaterThan(double value1, double value2)
        {
            return (value1 > value2) && !AreClose(value1, value2);
        }

        /// <summary>
        /// GreaterThan - Returns whether or not the first float is greater than the second float.
        /// That is, whether or not the first is strictly greater than *and* not within epsilon of
        /// the other number.
        /// </summary>
        /// <param name="value1"> The first float to compare. </param>
        /// <param name="value2"> The second float to compare. </param>
        public static bool GreaterThan(float value1, float value2)
        {
            return (value1 > value2) && !AreClose(value1, value2);
        }

        /// <summary>
        /// LessThanOrClose - Returns whether or not the first double is less than or close to
        /// the second double.  That is, whether or not the first is strictly less than or within
        /// epsilon of the other number.
        /// </summary>
        /// <param name="value1"> The first double to compare. </param>
        /// <param name="value2"> The second double to compare. </param>
        public static bool LessThanOrClose(double value1, double value2)
        {
            return (value1 < value2) || AreClose(value1, value2);
        }

        /// <summary>
        /// LessThanOrClose - Returns whether or not the first float is less than or close to
        /// the second float.  That is, whether or not the first is strictly less than or within
        /// epsilon of the other number.
        /// </summary>
        /// <param name="value1"> The first float to compare. </param>
        /// <param name="value2"> The second float to compare. </param>
        public static bool LessThanOrClose(float value1, float value2)
        {
            return (value1 < value2) || AreClose(value1, value2);
        }

        /// <summary>
        /// GreaterThanOrClose - Returns whether or not the first double is greater than or close to
        /// the second double.  That is, whether or not the first is strictly greater than or within
        /// epsilon of the other number.
        /// </summary>
        /// <param name="value1"> The first double to compare. </param>
        /// <param name="value2"> The second double to compare. </param>
        public static bool GreaterThanOrClose(double value1, double value2)
        {
            return (value1 > value2) || AreClose(value1, value2);
        }

        /// <summary>
        /// GreaterThanOrClose - Returns whether or not the first float is greater than or close to
        /// the second float.  That is, whether or not the first is strictly greater than or within
        /// epsilon of the other number.
        /// </summary>
        /// <param name="value1"> The first float to compare. </param>
        /// <param name="value2"> The second float to compare. </param>
        public static bool GreaterThanOrClose(float value1, float value2)
        {
            return (value1 > value2) || AreClose(value1, value2);
        }

        /// <summary>
        /// IsOne - Returns whether or not the double is "close" to 1.  Same as AreClose(double, 1),
        /// but this is faster.
        /// </summary>
        /// <param name="value"> The double to compare to 1. </param>
        public static bool IsOne(double value)
        {
            return Math.Abs(value - 1.0) < 10.0 * DoubleEpsilon;
        }

        /// <summary>
        /// IsOne - Returns whether or not the float is "close" to 1.  Same as AreClose(float, 1),
        /// but this is faster.
        /// </summary>
        /// <param name="value"> The float to compare to 1. </param>
        public static bool IsOne(float value)
        {
            return Math.Abs(value - 1.0f) < 10.0f * FloatEpsilon;
        }

        /// <summary>
        /// IsZero - Returns whether or not the double is "close" to 0.  Same as AreClose(double, 0),
        /// but this is faster.
        /// </summary>
        /// <param name="value"> The double to compare to 0. </param>
        public static bool IsZero(double value)
        {
            return Math.Abs(value) < 10.0 * DoubleEpsilon;
        }

        /// <summary>
        /// IsZero - Returns whether or not the float is "close" to 0.  Same as AreClose(float, 0),
        /// but this is faster.
        /// </summary>
        /// <param name="value"> The float to compare to 0. </param>
        public static bool IsZero(float value)
        {
            return Math.Abs(value) < 10.0f * FloatEpsilon;
        }

        /// <summary>
        /// Clamps a value between a minimum and maximum value.
        /// </summary>
        /// <param name="val">The value.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>The clamped value.</returns>
        public static double Clamp(double val, double min, double max)
        {
            if (min > max)
            {
                ThrowCannotBeGreaterThanException(min, max);
            }

            if (val < min)
            {
                return min;
            }
            else if (val > max)
            {
                return max;
            }
            else
            {
                return val;
            }
        }

        /// <summary>
        /// Clamps a value between a minimum and maximum value.
        /// </summary>
        /// <param name="val">The value.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>The clamped value.</returns>
        public static decimal Clamp(decimal val, decimal min, decimal max)
        {
            if (min > max)
            {
                ThrowCannotBeGreaterThanException(min, max);
            }

            if (val < min)
            {
                return min;
            }
            else if (val > max)
            {
                return max;
            }
            else
            {
                return val;
            }
        }
        
        /// <summary>
        /// Clamps a value between a minimum and maximum value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>The clamped value.</returns>
        public static float Clamp(float value, float min, float max)
        {
            var amax = Math.Max(min, max);
            var amin = Math.Min(min, max);
            return Math.Min(Math.Max(value, amin), amax);
        }

        /// <summary>
        /// Clamps a value between a minimum and maximum value.
        /// </summary>
        /// <param name="val">The value.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>The clamped value.</returns>
        public static int Clamp(int val, int min, int max)
        {
            if (min > max)
            {
                ThrowCannotBeGreaterThanException(min, max);
            }

            if (val < min)
            {
                return min;
            }
            else if (val > max)
            {
                return max;
            }
            else
            {
                return val;
            }
        }

        /// <summary>
        /// Converts an angle in degrees to radians.
        /// </summary>
        /// <param name="angle">The angle in degrees.</param>
        /// <returns>The angle in radians.</returns>
        public static double Deg2Rad(double angle)
        {
            return angle * (Math.PI / 180d);
        }

        /// <summary>
        /// Converts an angle in gradians to radians.
        /// </summary>
        /// <param name="angle">The angle in gradians.</param>
        /// <returns>The angle in radians.</returns>
        public static double Grad2Rad(double angle)
        {
            return angle * (Math.PI / 200d);
        }

        /// <summary>
        /// Converts an angle in turns to radians.
        /// </summary>
        /// <param name="angle">The angle in turns.</param>
        /// <returns>The angle in radians.</returns>
        public static double Turn2Rad(double angle)
        {
            return angle * 2 * Math.PI;
        }

        /// <summary>
        /// Calculates the point of an angle on an ellipse.
        /// </summary>
        /// <param name="centre">The centre point of the ellipse.</param>
        /// <param name="radiusX">The x radius of the ellipse.</param>
        /// <param name="radiusY">The y radius of the ellipse.</param>
        /// <param name="angle">The angle in radians.</param>
        /// <returns>A point on the ellipse.</returns>
        public static Point GetEllipsePoint(Point centre, double radiusX, double radiusY, double angle)
        {
            return new Point(radiusX * Math.Cos(angle) + centre.X, radiusY * Math.Sin(angle) + centre.Y);
        }

        /// <summary>
        /// Gets the minimum and maximum from the specified numbers.
        /// </summary>
        /// <param name="a">The first number.</param>
        /// <param name="b">The second number.</param>
        /// <returns>A tuple containing the minimum and maximum of the two specified numbers.</returns>
        public static (double min, double max) GetMinMax(double a, double b)
        {
            return a < b ? (a, b) : (b, a);
        }

        /// <summary>
        /// Gets the minimum and maximum from the specified number and the difference with that number.
        /// </summary>
        /// <param name="initialValue">The initial value to use.</param>
        /// <param name="delta">The difference for <paramref name="initialValue"/>.</param>
        /// <returns>A tuple containing the minimum and maximum of the specified number and the difference with that number.</returns>
        public static (double min, double max) GetMinMaxFromDelta(double initialValue, double delta)
        {
            return GetMinMax(initialValue, initialValue + delta);
        }
        
#if !BUILDTASK
        public static int WhichPolygonSideIntersects(
            uint cPoly,
            ReadOnlySpan<Vector> pPtPoly,
            Vector ptCurrent,
            Vector vecEdge)
        {
            uint nPositive = 0;
            uint nNegative = 0;
            uint nZero = 0;

            var vecEdgeNormal = new Point(-vecEdge.Y, vecEdge.X);

            for (var i = 0; i < cPoly; i++)
            {
                var vecCurrent = ptCurrent - pPtPoly[i];
                var rDot = Vector.Dot(vecCurrent, vecEdgeNormal);

                if (rDot > 0.0f)
                {
                    nPositive++;
                }
                else if (rDot < 0.0f)
                {
                    nNegative++;
                }
                else
                {
                    nZero++;
                }

                if ((nPositive > 0 && nNegative > 0) || (nZero > 0))
                {
                    return 0;
                }
            }

            return nPositive > 0 ? 1 : -1;
        }

        public static bool DoPolygonsIntersect(
            uint cPolyA,
            ReadOnlySpan<Vector> pPtPolyA,
            uint cPolyB,
            ReadOnlySpan<Vector> pPtPolyB)
        {
            for (var i = 0; i < cPolyA; i++)
            {
                var vecEdge = pPtPolyA[(int)((i + 1) % cPolyA)] - pPtPolyA[i];
                if (WhichPolygonSideIntersects(cPolyB, pPtPolyB, pPtPolyA[i], vecEdge) < 0)
                {
                    return false;
                }
            }

            for (var i = 0; i < cPolyB; i++)
            {
                var vecEdge = pPtPolyB[(int)((i + 1) % cPolyB)] - pPtPolyB[i];
                if (WhichPolygonSideIntersects(cPolyA, pPtPolyA, pPtPolyB[i], vecEdge) < 0)
                {
                    return false;
                }
            }

            return true;
        }
        
        public static bool IsEntirelyContained(
            uint cPolyA,
            ReadOnlySpan<Vector> pPtPolyA,
            uint cPolyB,
            ReadOnlySpan<Vector> pPtPolyB)
        {
            for (var i = 0; i < cPolyB; i++)
            {
                var vecEdge = pPtPolyB[(i + 1) % (int)cPolyB] - pPtPolyB[i];
                if (WhichPolygonSideIntersects(cPolyA, pPtPolyA, pPtPolyB[i], vecEdge) <= 0)
                {
                    // The whole of the polygon is entirely on the outside of the edge,
                    // so we can never intersect
                    return false;
                }
            }

            return true;
        }
#endif
        
        private static void ThrowCannotBeGreaterThanException<T>(T min, T max)
        {
            throw new ArgumentException($"{min} cannot be greater than {max}.");
        }
    }
}
