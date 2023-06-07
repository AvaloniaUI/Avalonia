// ReSharper disable InconsistentNaming
// Ported from Chromium project https://github.com/chromium/chromium/blob/374d31b7704475fa59f7b2cb836b3b68afdc3d79/ui/gfx/geometry/cubic_bezier.cc

using System;
using Avalonia.Utilities;

// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable CommentTypo
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable TooWideLocalVariableScope
// ReSharper disable UnusedMember.Global
#pragma warning disable 649

namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Represents a cubic bezier curve and can compute Y coordinate for a given X
    /// </summary>
    internal unsafe struct CubicBezier
    {
        const int CUBIC_BEZIER_SPLINE_SAMPLES = 11;
        double ax_;
        double bx_;
        double cx_;

        double ay_;
        double by_;
        double cy_;

        double start_gradient_;
        double end_gradient_;

        double range_min_;
        double range_max_;
        private bool monotonically_increasing_;

        fixed double spline_samples_[CUBIC_BEZIER_SPLINE_SAMPLES];

        public CubicBezier(double p1x, double p1y, double p2x, double p2y) : this()
        {
            InitCoefficients(p1x, p1y, p2x, p2y);
            InitGradients(p1x, p1y, p2x, p2y);
            InitRange(p1y, p2y);
            InitSpline();
        }

        public readonly double SampleCurveX(double t)
        {
            // `ax t^3 + bx t^2 + cx t' expanded using Horner's rule.
            return ((ax_ * t + bx_) * t + cx_) * t;
        }

        readonly double SampleCurveY(double t)
        {
            return ((ay_ * t + by_) * t + cy_) * t;
        }

        readonly double SampleCurveDerivativeX(double t)
        {
            return (3.0 * ax_ * t + 2.0 * bx_) * t + cx_;
        }

        readonly double SampleCurveDerivativeY(double t)
        {
            return (3.0 * ay_ * t + 2.0 * by_) * t + cy_;
        }

        public readonly double SolveWithEpsilon(double x, double epsilon)
        {
            if (x < 0.0)
                return 0.0 + start_gradient_ * x;
            if (x > 1.0)
                return 1.0 + end_gradient_ * (x - 1.0);
            return SampleCurveY(SolveCurveX(x, epsilon));
        }
        
        void InitCoefficients(double p1x,
            double p1y,
            double p2x,
            double p2y)
        {
            // Calculate the polynomial coefficients, implicit first and last control
            // points are (0,0) and (1,1).
            cx_ = 3.0 * p1x;
            bx_ = 3.0 * (p2x - p1x) - cx_;
            ax_ = 1.0 - cx_ - bx_;

            cy_ = 3.0 * p1y;
            by_ = 3.0 * (p2y - p1y) - cy_;
            ay_ = 1.0 - cy_ - by_;

#if DEBUG
            // Bezier curves with x-coordinates outside the range [0,1] for internal
            // control points may have multiple values for t for a given value of x.
            // In this case, calls to SolveCurveX may produce ambiguous results.
            monotonically_increasing_ = p1x >= 0 && p1x <= 1 && p2x >= 0 && p2x <= 1;
#endif
        }

        void InitGradients(double p1x,
            double p1y,
            double p2x,
            double p2y)
        {
            // End-point gradients are used to calculate timing function results
            // outside the range [0, 1].
            //
            // There are four possibilities for the gradient at each end:
            // (1) the closest control point is not horizontally coincident with regard to
            //     (0, 0) or (1, 1). In this case the line between the end point and
            //     the control point is tangent to the bezier at the end point.
            // (2) the closest control point is coincident with the end point. In
            //     this case the line between the end point and the far control
            //     point is tangent to the bezier at the end point.
            // (3) both internal control points are coincident with an endpoint. There
            //     are two special case that fall into this category:
            //     CubicBezier(0, 0, 0, 0) and CubicBezier(1, 1, 1, 1). Both are
            //     equivalent to linear.
            // (4) the closest control point is horizontally coincident with the end
            //     point, but vertically distinct. In this case the gradient at the
            //     end point is Infinite. However, this causes issues when
            //     interpolating. As a result, we break down to a simple case of
            //     0 gradient under these conditions.

            if (p1x > 0)
                start_gradient_ = p1y / p1x;
            else if (p1y == 0 && p2x > 0)
                start_gradient_ = p2y / p2x;
            else if (p1y == 0 && p2y == 0)
                start_gradient_ = 1;
            else
                start_gradient_ = 0;

            if (p2x < 1)
                end_gradient_ = (p2y - 1) / (p2x - 1);
            else if (p2y == 1 && p1x < 1)
                end_gradient_ = (p1y - 1) / (p1x - 1);
            else if (p2y == 1 && p1y == 1)
                end_gradient_ = 1;
            else
                end_gradient_ = 0;
        }

        const double kBezierEpsilon = 1e-7;

        void InitRange(double p1y, double p2y)
        {
            range_min_ = 0;
            range_max_ = 1;
            if (0 <= p1y && p1y < 1 && 0 <= p2y && p2y <= 1)
                return;

            double epsilon = kBezierEpsilon;

            // Represent the function's derivative in the form at^2 + bt + c
            // as in sampleCurveDerivativeY.
            // (Technically this is (dy/dt)*(1/3), which is suitable for finding zeros
            // but does not actually give the slope of the curve.)
            double a = 3.0 * ay_;
            double b = 2.0 * by_;
            double c = cy_;

            // Check if the derivative is constant.
            if (Math.Abs(a) < epsilon && Math.Abs(b) < epsilon)
                return;

            // Zeros of the function's derivative.
            double t1;
            double t2 = 0;

            if (Math.Abs(a) < epsilon)
            {
                // The function's derivative is linear.
                t1 = -c / b;
            }
            else
            {
                // The function's derivative is a quadratic. We find the zeros of this
                // quadratic using the quadratic formula.
                double discriminant = b * b - 4 * a * c;
                if (discriminant < 0)
                    return;
                double discriminant_sqrt = Math.Sqrt(discriminant);
                t1 = (-b + discriminant_sqrt) / (2 * a);
                t2 = (-b - discriminant_sqrt) / (2 * a);
            }

            double sol1 = 0;
            double sol2 = 0;

            // If the solution is in the range [0,1] then we include it, otherwise we
            // ignore it.

            // An interesting fact about these beziers is that they are only
            // actually evaluated in [0,1]. After that we take the tangent at that point
            // and linearly project it out.
            if (0 < t1 && t1 < 1)
                sol1 = SampleCurveY(t1);

            if (0 < t2 && t2 < 1)
                sol2 = SampleCurveY(t2);

            range_min_ = Math.Min(Math.Min(range_min_, sol1), sol2);
            range_max_ = Math.Max(Math.Max(range_max_, sol1), sol2);
        }

        void InitSpline()
        {
            double delta_t = 1.0 / (CUBIC_BEZIER_SPLINE_SAMPLES - 1);
            for (int i = 0; i < CUBIC_BEZIER_SPLINE_SAMPLES; i++)
            {
                spline_samples_[i] = SampleCurveX(i * delta_t);
            }
        }

        const int kMaxNewtonIterations = 4;


        public readonly double SolveCurveX(double x, double epsilon)
        {
            if (x < 0 || x > 1)
                throw new ArgumentException();

            double t0 = 0;
            double t1 = 0;
            double t2 = x;
            double x2 = 0;
            double d2;
            int i;

#if DEBUG
            if (!monotonically_increasing_)
                throw new InvalidOperationException();
#endif

            // Linear interpolation of spline curve for initial guess.
            double delta_t = 1.0 / (CUBIC_BEZIER_SPLINE_SAMPLES - 1);
            for (i = 1; i < CUBIC_BEZIER_SPLINE_SAMPLES; i++)
            {
                if (x <= spline_samples_[i])
                {
                    t1 = delta_t * i;
                    t0 = t1 - delta_t;
                    t2 = t0 + (t1 - t0) * (x - spline_samples_[i - 1]) /
                        (spline_samples_[i] - spline_samples_[i - 1]);
                    break;
                }
            }

            // Perform a few iterations of Newton's method -- normally very fast.
            // See https://en.wikipedia.org/wiki/Newton%27s_method.
            double newton_epsilon = Math.Min(kBezierEpsilon, epsilon);
            for (i = 0; i < kMaxNewtonIterations; i++)
            {
                x2 = SampleCurveX(t2) - x;
                if (Math.Abs(x2) < newton_epsilon)
                    return t2;
                d2 = SampleCurveDerivativeX(t2);
                if (Math.Abs(d2) < kBezierEpsilon)
                    break;
                t2 = t2 - x2 / d2;
            }

            if (Math.Abs(x2) < epsilon)
                return t2;

            // Fall back to the bisection method for reliability.
            while (t0 < t1)
            {
                x2 = SampleCurveX(t2);
                if (Math.Abs(x2 - x) < epsilon)
                    return t2;
                if (x > x2)
                    t0 = t2;
                else
                    t1 = t2;
                t2 = (t1 + t0) * .5;
            }

            // Failure.
            return t2;
        }

        public readonly double Solve(double x)
        {
            return SolveWithEpsilon(x, kBezierEpsilon);
        }

        public readonly double SlopeWithEpsilon(double x, double epsilon)
        {
            x = MathUtilities.Clamp(x, 0.0, 1.0);
            double t = SolveCurveX(x, epsilon);
            double dx = SampleCurveDerivativeX(t);
            double dy = SampleCurveDerivativeY(t);
            return dy / dx;
        }

        public readonly double Slope(double x)
        {
            return SlopeWithEpsilon(x, kBezierEpsilon);
        }

        public readonly double RangeMin => range_min_;
        public readonly double RangeMax => range_max_;
    }
}