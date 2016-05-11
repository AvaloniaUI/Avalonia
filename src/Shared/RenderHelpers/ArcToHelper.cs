// Copyright © 2003-2004, Luc Maisonobe
// 2015 - Alexey Rozanov <thehdotx@gmail.com> - Adaptations for Avalonia and oval center computations
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with
// or without modification, are permitted provided that
// the following conditions are met:
// 
//    Redistributions of source code must retain the
//    above copyright notice, this list of conditions and
//    the following disclaimer. 
//    Redistributions in binary form must reproduce the
//    above copyright notice, this list of conditions and
//    the following disclaimer in the documentation
//    and/or other materials provided with the
//    distribution. 
//    Neither the names of spaceroots.org, spaceroots.com
//    nor the names of their contributors may be used to
//    endorse or promote products derived from this
//    software without specific prior written permission. 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
// CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED
// WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
// PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL
// THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF
// USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
// HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER
// IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
// USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

// C#/WPF/Avalonia adaptation by Alexey Rozanov <thehdotx@gmail.com>, 2015.
// I do not mind if anyone would find this adaptation useful, but
// please retain the above disclaimer made by the original class 
// author Luc Maisonobe. He worked really hard on this subject, so
// please respect him by at least keeping the above disclaimer intact
// if you use his code.
//
// Commented out some unused values calculations.
// These are not supposed to be removed from the source code,
// as these may be helpful for debugging.

using System;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.RenderHelpers
{
    static class ArcToHelper
    {
        /// <summary>
        /// This class represents an elliptical arc on a 2D plane.
        /// 
        /// This class is adapted for use with WPF StreamGeometryContext, and needs to be created explicitly
        /// for each particular arc.
        /// 
        /// Some helpers
        /// 
        /// It can handle ellipses which are not aligned with the x and y reference axes of the plane,
        /// as well as their parts.
        /// 
        /// Another improvement is that this class can handle degenerated cases like for example very 
        /// flat ellipses(semi-minor axis much smaller than semi-major axis) and drawing of very small 
        /// parts of such ellipses at very high magnification scales.This imply monitoring the drawing 
        /// approximation error for extremely small values.Such cases occur for example while drawing 
        /// orbits of comets near the perihelion.
        /// 
        /// When the arc does not cover the complete ellipse, the lines joining the center of the 
        /// ellipse to the endpoints can optionally be included or not in the outline, hence allowing 
        /// to use it for pie-charts rendering. If these lines are not included, the curve is not 
        /// naturally closed.
        /// </summary>
        public sealed class EllipticalArc
        {

            private const double TwoPi = 2 * Math.PI;

            /// <summary>
            /// Coefficients for error estimation while using quadratic Bezier curves for approximation,
            /// 0 ≤ b/a ≤ 0.25
            /// </summary>
            private static readonly double[][][] Coeffs2Low = {
            new[]
            {
                new[] {3.92478, -13.5822, -0.233377, 0.0128206},
                new[] {-1.08814, 0.859987, 3.62265E-4, 2.29036E-4},
                new[] {-0.942512, 0.390456, 0.0080909, 0.00723895},
                new[] {-0.736228, 0.20998, 0.0129867, 0.0103456}
            },
            new[]
            {
                new[] {-0.395018, 6.82464, 0.0995293, 0.0122198},
                new[] {-0.545608, 0.0774863, 0.0267327, 0.0132482},
                new[] {0.0534754, -0.0884167, 0.012595, 0.0343396},
                new[] {0.209052, -0.0599987, -0.00723897, 0.00789976}
            }
        };

            /// <summary>
            /// Coefficients for error estimation while using quadratic Bezier curves for approximation,
            /// 0.25 ≤ b/a ≤ 1
            /// </summary>
            private static readonly double[][][] Coeffs2High = {
            new[]
            {
                new[] {0.0863805, -11.5595, -2.68765, 0.181224},
                new[] {0.242856, -1.81073, 1.56876, 1.68544},
                new[] {0.233337, -0.455621, 0.222856, 0.403469},
                new[] {0.0612978, -0.104879, 0.0446799, 0.00867312}
            },
            new[]
            {
                new[] {0.028973, 6.68407, 0.171472, 0.0211706},
                new[] {0.0307674, -0.0517815, 0.0216803, -0.0749348},
                new[] {-0.0471179, 0.1288, -0.0781702, 2.0},
                new[] {-0.0309683, 0.0531557, -0.0227191, 0.0434511}
            }
        };

            /// <summary>
            /// Safety factor to convert the "best" error approximation into a "max bound" error
            /// </summary>
            private static readonly double[] Safety2 = { 0.02, 2.83, 0.125, 0.01 };

            /// <summary>
            /// Coefficients for error estimation while using cubic Bezier curves for approximation,
            /// 0.25 ≤ b/a ≤ 1
            /// </summary>
            private static readonly double[][][] Coeffs3Low = {
            new[]
            {
                new[] {3.85268, -21.229, -0.330434, 0.0127842},
                new[] {-1.61486, 0.706564, 0.225945, 0.263682},
                new[] {-0.910164, 0.388383, 0.00551445, 0.00671814},
                new[] {-0.630184, 0.192402, 0.0098871, 0.0102527}
            },
            new[]
            {
                new[] {-0.162211, 9.94329, 0.13723, 0.0124084},
                new[] {-0.253135, 0.00187735, 0.0230286, 0.01264},
                new[] {-0.0695069, -0.0437594, 0.0120636, 0.0163087},
                new[] {-0.0328856, -0.00926032, -0.00173573, 0.00527385}
            }
        };

            /// <summary>
            /// Coefficients for error estimation while using cubic Bezier curves for approximation,
            /// 0.25 ≤ b/a ≤ 1
            /// </summary>
            private static readonly double[][][] Coeffs3High = {
            new[]
            {
                new[] {0.0899116, -19.2349, -4.11711, 0.183362},
                new[] {0.138148, -1.45804, 1.32044, 1.38474},
                new[] {0.230903, -0.450262, 0.219963, 0.414038},
                new[] {0.0590565, -0.101062, 0.0430592, 0.0204699}
            },
            new[]
            {
                new[] {0.0164649, 9.89394, 0.0919496, 0.00760802},
                new[] {0.0191603, -0.0322058, 0.0134667, -0.0825018},
                new[] {0.0156192, -0.017535, 0.00326508, -0.228157},
                new[] {-0.0236752, 0.0405821, -0.0173086, 0.176187}
            }
        };
            /// <summary>
            /// Safety factor to convert the "best" error approximation into a "max bound" error
            /// </summary>
            private static readonly double[] Safety3 = { 0.0010, 4.98, 0.207, 0.0067 };

            /// <summary>
            /// Abscissa of the center of the ellipse
            /// </summary>
            internal double Cx;
            /// <summary>
            /// Ordinate of the center of the ellipse
            /// </summary>
            internal double Cy;
            /// <summary>
            /// Semi-major axis
            /// </summary>
            internal double A;
            /// <summary>
            /// Semi-minor axis
            /// </summary>
            internal double B;
            /// <summary>
            /// Orientation of the major axis with respect to the x axis
            /// </summary>
            internal double Theta;
            /// <summary>
            /// Pre-calculated cosine value for the major-axis-to-X orientation (Theta)
            /// </summary>
            private readonly double _cosTheta;
            /// <summary>
            /// Pre-calculated sine value for the major-axis-to-X orientation (Theta)
            /// </summary>
            private readonly double _sinTheta;
            /// <summary>
            /// Start angle of the arc
            /// </summary>
            internal double Eta1;
            /// <summary>
            /// End angle of the arc
            /// </summary>
            internal double Eta2;
            /// <summary>
            /// Abscissa of the start point
            /// </summary>
            internal double X1;
            /// <summary>
            /// Ordinate of the start point
            /// </summary>
            internal double Y1;
            /// <summary>
            /// Abscissa of the end point
            /// </summary>
            internal double X2;
            /// <summary>
            /// Ordinate of the end point
            /// </summary>
            internal double Y2;
            /// <summary>
            /// Abscissa of the first focus
            /// </summary>
            internal double FirstFocusX;
            /// <summary>
            /// Ordinate of the first focus
            /// </summary>
            internal double FirstFocusY;
            /// <summary>
            /// Abscissa of the second focus
            /// </summary>
            internal double SecondFocusX;
            /// <summary>
            /// Ordinate of the second focus
            /// </summary>
            internal double SecondFocusY;
            /// <summary>
            /// Abscissa of the leftmost point of the arc
            /// </summary>
            private double _xLeft;
            /// <summary>
            /// Ordinate of the highest point of the arc
            /// </summary>
            private double _yUp;
            /// <summary>
            /// Horizontal width of the arc
            /// </summary>
            private double _width;
            /// <summary>
            /// Vertical height of the arc
            /// </summary>
            private double _height;
            /// <summary>
            /// Indicator for center to endpoints line inclusion
            /// </summary>
            internal bool IsPieSlice;
            /// <summary>
            /// Maximal degree for Bezier curve approximation
            /// </summary>
            private int _maxDegree;
            /// <summary>
            /// Default flatness for Bezier curve approximation
            /// </summary>
            private double _defaultFlatness;

            /// <summary>
            /// Indicator for semi-major axis significance (compared to semi-minor one).
            /// Computed by dividing the (A-B) difference by the value of A.
            /// This indicator is used for an early escape in intersection test
            /// </summary>
            internal double F;
            /// <summary>
            /// Indicator used for an early escape in intersection test
            /// </summary>
            internal double E2;
            /// <summary>
            /// Indicator used for an early escape in intersection test
            /// </summary>
            internal double G;
            /// <summary>
            /// Indicator used for an early escape in intersection test
            /// </summary>
            internal double G2;

            /// <summary>
            /// Builds an elliptical arc composed of the full unit circle around (0,0)
            /// </summary>
            public EllipticalArc()
            {
                Cx = 0;
                Cy = 0;
                A = 1;
                B = 1;
                Theta = 0;
                Eta1 = 0;
                Eta2 = TwoPi;
                _cosTheta = 1;
                _sinTheta = 0;
                IsPieSlice = false;
                _maxDegree = 3;
                _defaultFlatness = 0.5;
                ComputeFocii();
                ComputeEndPoints();
                ComputeBounds();
                ComputeDerivedFlatnessParameters();
            }

            /// <summary>
            /// Builds an elliptical arc from its canonical geometrical elements
            /// </summary>
            /// <param name="center">Center of the ellipse</param>
            /// <param name="a">Semi-major axis</param>
            /// <param name="b">Semi-minor axis</param>
            /// <param name="theta">Orientation of the major axis with respect to the x axis</param>
            /// <param name="lambda1">Start angle of the arc</param>
            /// <param name="lambda2">End angle of the arc</param>
            /// <param name="isPieSlice">If true, the lines between the center of the ellipse
            ///  and the endpoints are part of the shape (it is pie slice like)</param>
            public EllipticalArc(Point center, double a, double b, double theta, double lambda1, double lambda2,
                bool isPieSlice) : this(center.X, center.Y, a, b, theta, lambda1,
                    lambda2, isPieSlice)
            {
            }
            /// <summary>
            /// Builds an elliptical arc from its canonical geometrical elements
            /// </summary>
            /// <param name="cx">Abscissa of the center of the ellipse</param>
            /// <param name="cy">Ordinate of the center of the ellipse</param>
            /// <param name="a">Semi-major axis</param>
            /// <param name="b">Semi-minor axis</param>
            /// <param name="theta">Orientation of the major axis with respect to the x axis</param>
            /// <param name="lambda1">Start angle of the arc</param>
            /// <param name="lambda2">End angle of the arc</param>
            /// <param name="isPieSlice">If true, the lines between the center of the ellipse
            ///  and the endpoints are part of the shape (it is pie slice like)</param>
            public EllipticalArc(double cx, double cy, double a, double b, double theta, double lambda1, double lambda2,
                bool isPieSlice)
            {
                Cx = cx;
                Cy = cy;
                A = a;
                B = b;
                Theta = theta;
                IsPieSlice = isPieSlice;
                Eta1 = Math.Atan2(Math.Sin(lambda1) / b, Math.Cos(lambda1) / a);
                Eta2 = Math.Atan2(Math.Sin(lambda2) / b, Math.Cos(lambda2) / a);
                _cosTheta = Math.Cos(theta);
                _sinTheta = Math.Sin(theta);
                _maxDegree = 3;
                _defaultFlatness = 0.5; // half a pixel
                Eta2 -= TwoPi * Math.Floor((Eta2 - Eta1) / TwoPi); //make sure we have eta1 <= eta2 <= eta1 + 2 PI
                                                                   // the preceding correction fails if we have exactly eta2-eta1 == 2*PI
                                                                   // it reduces the interval to zero length
                if (lambda2 - lambda1 > Math.PI && Eta2 - Eta1 < Math.PI)
                {
                    Eta2 += TwoPi;
                }
                ComputeFocii();
                ComputeEndPoints();
                ComputeBounds();
                ComputeDerivedFlatnessParameters();
            }
            /// <summary>
            /// Build a full ellipse from its canonical geometrical elements
            /// </summary>
            /// <param name="center">Center of the ellipse</param>
            /// <param name="a">Semi-major axis</param>
            /// <param name="b">Semi-minor axis</param>
            /// <param name="theta">Orientation of the major axis with respect to the x axis</param>
            public EllipticalArc(Point center, double a, double b, double theta) : this(center.X, center.Y, a, b, theta)
            {
            }

            /// <summary>
            /// Build a full ellipse from its canonical geometrical elements
            /// </summary>
            /// <param name="cx">Abscissa of the center of the ellipse</param>
            /// <param name="cy">Ordinate of the center of the ellipse</param>
            /// <param name="a">Semi-major axis</param>
            /// <param name="b">Semi-minor axis</param>
            /// <param name="theta">Orientation of the major axis with respect to the x axis</param>
            public EllipticalArc(double cx, double cy, double a, double b, double theta)
            {
                Cx = cx;
                Cy = cy;
                A = a;
                B = b;
                Theta = theta;
                IsPieSlice = false;
                Eta1 = 0;
                Eta2 = TwoPi;
                _cosTheta = Math.Cos(theta);
                _sinTheta = Math.Sin(theta);
                _maxDegree = 3;
                _defaultFlatness = 0.5; //half a pixel
                ComputeFocii();
                ComputeEndPoints();
                ComputeBounds();
                ComputeDerivedFlatnessParameters();
            }

            /// <summary>
            /// Sets the maximal degree allowed for Bezier curve approximation.
            /// </summary>
            /// <param name="maxDegree">Maximal allowed degree (must be between 1 and 3)</param>
            /// <exception cref="ArgumentException">Thrown if maxDegree is not between 1 and 3</exception>
            public void SetMaxDegree(int maxDegree)
            {
                if (maxDegree < 1 || maxDegree > 3)
                {
                    throw new ArgumentException(@"maxDegree must be between 1 and 3", nameof(maxDegree));
                }
                _maxDegree = maxDegree;
            }

            /// <summary>
            /// Sets the default flatness for Bezier curve approximation
            /// </summary>
            /// <param name="defaultFlatness">default flatness (must be greater than 1e-10)</param>
            /// <exception cref="ArgumentException">Thrown if defaultFlatness is lower than 1e-10</exception>
            public void SetDefaultFlatness(double defaultFlatness)
            {
                if (defaultFlatness < 1.0E-10)
                {
                    throw new ArgumentException(@"defaultFlatness must be greater than 1.0e-10", nameof(defaultFlatness));
                }
                _defaultFlatness = defaultFlatness;
            }

            /// <summary>
            /// Computes the locations of the focii
            /// </summary>
            private void ComputeFocii()
            {
                double d = Math.Sqrt(A * A - B * B);
                double dx = d * _cosTheta;
                double dy = d * _sinTheta;
                FirstFocusX = Cx - dx;
                FirstFocusY = Cy - dy;
                SecondFocusX = Cx + dx;
                SecondFocusY = Cy + dy;
            }

            /// <summary>
            /// Computes the locations of the endpoints
            /// </summary>
            private void ComputeEndPoints()
            {
                double aCosEta1 = A * Math.Cos(Eta1);
                double bSinEta1 = B * Math.Sin(Eta1);
                X1 = Cx + aCosEta1 * _cosTheta - bSinEta1 * _sinTheta;
                Y1 = Cy + aCosEta1 * _sinTheta + bSinEta1 * _cosTheta;
                double aCosEta2 = A * Math.Cos(Eta2);
                double bSinEta2 = B * Math.Sin(Eta2);
                X2 = Cx + aCosEta2 * _cosTheta - bSinEta2 * _sinTheta;
                Y2 = Cy + aCosEta2 * _sinTheta + bSinEta2 * _cosTheta;
            }

            /// <summary>
            /// Computes the bounding box
            /// </summary>
            private void ComputeBounds()
            {
                double bOnA = B / A;
                double etaXMin;
                double etaXMax;
                double etaYMin;
                double etaYMax;
                if (Math.Abs(_sinTheta) < 0.1)
                {
                    double tanTheta = _sinTheta / _cosTheta;
                    if (_cosTheta < 0)
                    {
                        etaXMin = -Math.Atan(tanTheta * bOnA);
                        etaXMax = etaXMin + Math.PI;
                        etaYMin = 0.5 * Math.PI - Math.Atan(tanTheta / bOnA);
                        etaYMax = etaYMin + Math.PI;
                    }
                    else
                    {
                        etaXMax = -Math.Atan(tanTheta * bOnA);
                        etaXMin = etaXMax - Math.PI;
                        etaYMax = 0.5 * Math.PI - Math.Atan(tanTheta / bOnA);
                        etaYMin = etaYMax - Math.PI;
                    }
                }
                else
                {
                    double invTanTheta = _cosTheta / _sinTheta;
                    if (_sinTheta < 0)
                    {
                        etaXMax = 0.5 * Math.PI + Math.Atan(invTanTheta / bOnA);
                        etaXMin = etaXMax - Math.PI;
                        etaYMin = Math.Atan(invTanTheta * bOnA);
                        etaYMax = etaYMin + Math.PI;
                    }
                    else
                    {
                        etaXMin = 0.5 * Math.PI + Math.Atan(invTanTheta / bOnA);
                        etaXMax = etaXMin + Math.PI;
                        etaYMax = Math.Atan(invTanTheta * bOnA);
                        etaYMin = etaYMax - Math.PI;
                    }
                }
                etaXMin -= TwoPi * Math.Floor((etaXMin - Eta1) / TwoPi);
                etaYMin -= TwoPi * Math.Floor((etaYMin - Eta1) / TwoPi);
                etaXMax -= TwoPi * Math.Floor((etaXMax - Eta1) / TwoPi);
                etaYMax -= TwoPi * Math.Floor((etaYMax - Eta1) / TwoPi);
                _xLeft = etaXMin <= Eta2
                    ? Cx + A * Math.Cos(etaXMin) * _cosTheta - B * Math.Sin(etaXMin) * _sinTheta
                    : Math.Min(X1, X2);
                _yUp = etaYMin <= Eta2 ? Cy + A * Math.Cos(etaYMin) * _sinTheta + B * Math.Sin(etaYMin) * _cosTheta : Math.Min(Y1, Y2);
                _width = (etaXMax <= Eta2
                    ? Cx + A * Math.Cos(etaXMax) * _cosTheta - B * Math.Sin(etaXMax) * _sinTheta
                    : Math.Max(X1, X2)) - _xLeft;
                _height = (etaYMax <= Eta2
                    ? Cy + A * Math.Cos(etaYMax) * _sinTheta + B * Math.Sin(etaYMax) * _cosTheta
                    : Math.Max(Y1, Y2)) - _yUp;
            }

            /// <summary>
            /// Computes the flatness parameters used in intersection tests
            /// </summary>
            private void ComputeDerivedFlatnessParameters()
            {
                F = (A - B) / A;
                E2 = F * (2.0 - F);
                G = 1.0 - F;
                G2 = G * G;
            }

            /// <summary>
            /// Computes the value of a rational function.
            /// This method handles rational functions where the numerator is quadratic
            /// and the denominator is linear
            /// </summary>
            /// <param name="x">Abscissa for which the value should be computed</param>
            /// <param name="c">Coefficients array of the rational function</param>
            /// <returns></returns>
            private static double RationalFunction(double x, double[] c)
            {
                return (x * (x * c[0] + c[1]) + c[2]) / (x + c[3]);
            }

            /// <summary>
            /// Estimate the approximation error for a sub-arc of the instance
            /// </summary>
            /// <param name="degree">Degree of the Bezier curve to use (1, 2 or 3)</param>
            /// <param name="etaA">Start angle of the sub-arc</param>
            /// <param name="etaB">End angle of the sub-arc</param>
            /// <returns>Upper bound of the approximation error between the Bezier curve and the real ellipse</returns>
            public double EstimateError(int degree, double etaA, double etaB)
            {
                if (degree < 1 || degree > _maxDegree)
                    throw new ArgumentException($"degree should be between {1} and {_maxDegree}", nameof(degree));
                double eta = 0.5 * (etaA + etaB);
                if (degree < 2)
                {
                    //start point
                    double aCosEtaA = A * Math.Cos(etaA);
                    double bSinEtaA = B * Math.Sin(etaA);
                    double xA = Cx + aCosEtaA * _cosTheta - bSinEtaA * _sinTheta;
                    double yA = Cy + aCosEtaA * _sinTheta + bSinEtaA * _cosTheta;

                    //end point
                    double aCosEtaB = A * Math.Cos(etaB);
                    double bSinEtaB = B * Math.Sin(etaB);
                    double xB = Cx + aCosEtaB * _cosTheta - bSinEtaB * _sinTheta;
                    double yB = Cy + aCosEtaB * _sinTheta + bSinEtaB * _cosTheta;

                    //maximal error point
                    double aCosEta = A * Math.Cos(eta);
                    double bSinEta = B * Math.Sin(eta);
                    double x = Cx + aCosEta * _cosTheta - bSinEta * _sinTheta;
                    double y = Cy + aCosEta * _sinTheta + bSinEta * _cosTheta;

                    double dx = xB - xA;
                    double dy = yB - yA;

                    return Math.Abs(x * dy - y * dx + xB * yA - xA * yB) / Math.Sqrt(dx * dx + dy * dy);
                }
                else
                {
                    double x = B / A;
                    double dEta = etaB - etaA;
                    double cos2 = Math.Cos(2 * eta);
                    double cos4 = Math.Cos(4 * eta);
                    double cos6 = Math.Cos(6 * eta);

                    // select the right coeficients set according to degree and b/a
                    double[][][] coeffs;
                    double[] safety;
                    if (degree == 2)
                    {
                        coeffs = x < 0.25 ? Coeffs2Low : Coeffs2High;
                        safety = Safety2;
                    }
                    else
                    {
                        coeffs = x < 0.25 ? Coeffs3Low : Coeffs3High;
                        safety = Safety3;
                    }
                    double c0 = RationalFunction(x, coeffs[0][0]) + cos2 * RationalFunction(x, coeffs[0][1]) +
                                cos4 * RationalFunction(x, coeffs[0][2]) + cos6 * RationalFunction(x,
                                    coeffs[0][3]);
                    double c1 = RationalFunction(x, coeffs[1][0]) + cos2 * RationalFunction(x, coeffs[1][1]) +
                                cos4 * RationalFunction(x, coeffs[1][2]) + cos6 * RationalFunction(x,
                                    coeffs[1][3]);
                    return RationalFunction(x, safety) * A * Math.Exp(c0 + c1 * dEta);
                }
            }

            /// <summary>
            /// Get the elliptical arc point for a given angular parameter
            /// </summary>
            /// <param name="lambda">Angular parameter for which point is desired</param> 
            /// <returns>The desired elliptical arc point location</returns>
            public Point PointAt(double lambda)
            {
                double eta = Math.Atan2(Math.Sin(lambda) / B, Math.Cos(lambda) / A);
                double aCosEta = A * Math.Cos(eta);
                double bSinEta = B * Math.Sin(eta);
                Point p = new Point(Cx + aCosEta * _cosTheta - bSinEta * _sinTheta, Cy + aCosEta * _sinTheta + bSinEta * _cosTheta);
                return p;
            }

            /// <summary>
            /// Tests if the specified coordinates are inside the closed shape formed by this arc.
            /// If this is not a pie, then a shape derived by adding a closing chord is considered.
            /// </summary>
            /// <param name="x">Abscissa of the test point</param>
            /// <param name="y">Ordinate of the test point</param>
            /// <returns>True if the specified coordinates are inside the closed shape of this arc</returns>
            public bool Contains(double x, double y)
            {
                // position relative to the focii
                double dx1 = x - FirstFocusX;
                double dy1 = y - FirstFocusY;
                double dx2 = x - SecondFocusX;
                double dy2 = y - SecondFocusY;
                if (dx1 * dx1 + dy1 * dy1 + dx2 * dx2 + dy2 * dy2 > 4 * A * A)
                {
                    // the point is outside of the ellipse
                    return false;
                }
                if (IsPieSlice)
                {
                    // check the location of the test point with respect to the
                    // angular sector counted from the centre of the ellipse
                    double dxC = x - Cx;
                    double dyC = y - Cy;
                    double u = dxC * _cosTheta + dyC * _sinTheta;
                    double v = dyC * _cosTheta - dxC * _sinTheta;
                    double eta = Math.Atan2(v / B, u / A);
                    eta -= TwoPi * Math.Floor((eta - Eta1) / TwoPi);
                    return eta <= Eta2;
                }
                // check the location of the test point with respect to the
                // chord joining the start and end points
                double dx = X2 - X1;
                double dy = Y2 - Y1;
                return x * dy - y * dx + X2 * Y1 - X1 * Y2 >= 0;
            }

            /// <summary>
            /// Tests if a line segment intersects the arc
            /// </summary>
            /// <param name="xA">abscissa of the first point of the line segment</param>
            /// <param name="yA">ordinate of the first point of the line segment</param>
            /// <param name="xB">abscissa of the second point of the line segment</param>
            /// <param name="yB">ordinate of the second point of the line segment</param>
            /// <returns>true if the two line segments intersect</returns>
            private bool IntersectArc(double xA, double yA, double xB, double yB)
            {
                double dx = xA - xB;
                double dy = yA - yB;
                double l = Math.Sqrt(dx * dx + dy * dy);
                if (l < 1.0E-10 * A)
                {
                    // too small line segment, we consider it doesn't intersect anything
                    return false;
                }
                double cz = (dx * _cosTheta + dy * _sinTheta) / l;
                double sz = (dy * _cosTheta - dx * _sinTheta) / l;

                // express position of the first point in canonical frame
                dx = xA - Cx;
                dy = yA - Cy;
                double u = dx * _cosTheta + dy * _sinTheta;
                double v = dy * _cosTheta - dx * _sinTheta;
                double u2 = u * u;
                double v2 = v * v;
                double g2U2Ma2 = G2 * (u2 - A * A);
                //double g2U2Ma2Mv2 = g2U2Ma2 - v2;
                double g2U2Ma2Pv2 = g2U2Ma2 + v2;

                // compute intersections with the ellipse along the line
                // as the roots of a 2nd degree polynom : c0 k^2 - 2 c1 k + c2 = 0
                double c0 = 1.0 - E2 * cz * cz;
                double c1 = G2 * u * cz + v * sz;
                double c2 = g2U2Ma2Pv2;
                double c12 = c1 * c1;
                double c0C2 = c0 * c2;
                if (c12 < c0C2)
                {
                    // the line does not intersect the ellipse at all
                    return false;
                }
                double k = c1 >= 0 ? (c1 + Math.Sqrt(c12 - c0C2)) / c0 : c2 / (c1 - Math.Sqrt(c12 - c0C2));
                if (k >= 0 && k <= l)
                {
                    double uIntersect = u - k * cz;
                    double vIntersect = v - k * sz;
                    double eta = Math.Atan2(vIntersect / B, uIntersect / A);
                    eta -= TwoPi * Math.Floor((eta - Eta1) / TwoPi);
                    if (eta <= Eta2)
                    {
                        return true;
                    }
                }
                k = c2 / (k * c0);
                if (k >= 0 && k <= l)
                {
                    double uIntersect = u - k * cz;
                    double vIntersect = v - k * sz;
                    double eta = Math.Atan2(vIntersect / B, uIntersect / A);
                    eta -= TwoPi * Math.Floor((eta - Eta1) / TwoPi);
                    if (eta <= Eta2)
                    {
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// Tests if two line segments intersect
            /// </summary>
            /// <param name="x1">Abscissa of the first point of the first line segment</param>
            /// <param name="y1">Ordinate of the first point of the first line segment</param>
            /// <param name="x2">Abscissa of the second point of the first line segment</param>
            /// <param name="y2">Ordinate of the second point of the first line segment</param>
            /// <param name="xA">Abscissa of the first point of the second line segment</param>
            /// <param name="yA">Ordinate of the first point of the second line segment</param>
            /// <param name="xB">Abscissa of the second point of the second line segment</param>
            /// <param name="yB">Ordinate of the second point of the second line segment</param>
            /// <returns>true if the two line segments intersect</returns>
            private static bool Intersect(double x1, double y1, double x2, double y2, double xA, double yA, double xB,
                double yB)
            {
                // elements of the equation of the (1, 2) line segment
                double dx12 = x2 - x1;
                double dy12 = y2 - y1;
                double k12 = x2 * y1 - x1 * y2;
                // elements of the equation of the (A, B) line segment
                double dxAb = xB - xA;
                double dyAb = yB - yA;
                double kAb = xB * yA - xA * yB;
                // compute relative positions of endpoints versus line segments
                double pAvs12 = xA * dy12 - yA * dx12 + k12;
                double pBvs12 = xB * dy12 - yB * dx12 + k12;
                double p1VsAb = x1 * dyAb - y1 * dxAb + kAb;
                double p2VsAb = x2 * dyAb - y2 * dxAb + kAb;

                return pAvs12 * pBvs12 <= 0 && p1VsAb * p2VsAb <= 0;
            }

            /// <summary>
            /// Tests if a line segment intersects the outline
            /// </summary>
            /// <param name="xA">Abscissa of the first point of the line segment</param>
            /// <param name="yA">Ordinate of the first point of the line segment</param>
            /// <param name="xB">Abscissa of the second point of the line segment</param>
            /// <param name="yB">Ordinate of the second point of the line segment</param>
            /// <returns>true if the two line segments intersect</returns>
            private bool IntersectOutline(double xA, double yA, double xB, double yB)
            {
                if (IntersectArc(xA, yA, xB, yB))
                {
                    return true;
                }
                if (IsPieSlice)
                {
                    return Intersect(Cx, Cy, X1, Y1, xA, yA, xB, yB) || Intersect(Cx, Cy, X2, Y2, xA, yA, xB, yB);
                }
                return Intersect(X1, Y1, X2, Y2, xA, yA, xB, yB);
            }

            /// <summary>
            /// Tests if the interior of a closed path derived from this arc entirely contains the specified rectangular area.
            /// The closed path is derived with respect to the IsPieSlice value.
            /// </summary>
            /// <param name="x">Abscissa of the upper-left corner of the test rectangle</param>
            /// <param name="y">Ordinate of the upper-left corner of the test rectangle</param>
            /// <param name="w">Width of the test rectangle</param>
            /// <param name="h">Height of the test rectangle</param>
            /// <returns>true if the interior of a closed path derived from this arc entirely contains the specified rectangular area; false otherwise</returns>
            public bool Contains(double x, double y, double w, double h)
            {
                double xPlusW = x + w;
                double yPlusH = y + h;
                return Contains(x, y) && Contains(xPlusW, y) && Contains(x, yPlusH) && Contains(xPlusW, yPlusH) &&
                       !IntersectOutline(x, y, xPlusW, y) && !IntersectOutline(xPlusW,
                           y, xPlusW, yPlusH) && !IntersectOutline(xPlusW, yPlusH, x, yPlusH) &&
                       !IntersectOutline(x, yPlusH, x, y);
            }

            /// <summary>
            /// Tests if a specified Point2D is inside the boundary of a closed path derived from this arc.
            /// The closed path is derived with respect to the IsPieSlice value.
            /// </summary>
            /// <param name="p">Test point</param>
            /// <returns>true if the specified point is inside a closed path derived from this arc</returns>
            public bool Contains(Point p)
            {
                return Contains(p.X, p.Y);
            }

            /// <summary>
            /// Tests if the interior of a closed path derived from this arc entirely contains the specified Rectangle2D.
            /// The closed path is derived with respect to the IsPieSlice value.
            /// </summary>
            /// <param name="r">Test rectangle</param>
            /// <returns>True if the interior of a closed path derived from this arc entirely contains the specified Rectangle2D; false otherwise</returns>
            public bool Contains(Rect r)
            {
                return Contains(r.X, r.Y, r.Width, r.Height);
            }

            /// <summary>
            /// Returns an integer Rectangle that completely encloses the closed path derived from this arc.
            /// The closed path is derived with respect to the IsPieSlice value.
            /// </summary> 
            public Rect GetBounds()
            {
                return new Rect(_xLeft, _yUp, _width, _height);
            }

            /// <summary>
            /// Builds the arc outline using given StreamGeometryContext and default (max) Bezier curve degree and acceptable error of half a pixel (0.5)
            /// </summary>
            /// <param name="path">A StreamGeometryContext to output the path commands to</param>
            public void BuildArc(IStreamGeometryContextImpl path)
            {
                BuildArc(path, _maxDegree, _defaultFlatness, true);
            }

            /// <summary>
            /// Builds the arc outline using given StreamGeometryContext
            /// </summary>
            /// <param name="path">A StreamGeometryContext to output the path commands to</param>
            /// <param name="degree">degree of the Bezier curve to use</param>
            /// <param name="threshold">acceptable error</param>
            /// <param name="openNewFigure">if true, a new figure will be started in the specified StreamGeometryContext</param>
            public void BuildArc(IStreamGeometryContextImpl path, int degree, double threshold, bool openNewFigure)
            {
                if (degree < 1 || degree > _maxDegree)
                    throw new ArgumentException($"degree should be between {1} and {_maxDegree}", nameof(degree));

                // find the number of Bezier curves needed
                bool found = false;
                int n = 1;
                double dEta;
                double etaB;
                while (!found && n < 1024)
                {
                    dEta = (Eta2 - Eta1) / n;
                    if (dEta <= 0.5 * Math.PI)
                    {
                        etaB = Eta1;
                        found = true;
                        for (int i = 0; found && i < n; ++i)
                        {
                            double etaA = etaB;
                            etaB += dEta;
                            found = EstimateError(degree, etaA, etaB) <= threshold;
                        }
                    }
                    n = n << 1;
                }
                dEta = (Eta2 - Eta1) / n;
                etaB = Eta1;
                double cosEtaB = Math.Cos(etaB);
                double sinEtaB = Math.Sin(etaB);
                double aCosEtaB = A * cosEtaB;
                double bSinEtaB = B * sinEtaB;
                double aSinEtaB = A * sinEtaB;
                double bCosEtaB = B * cosEtaB;
                double xB = Cx + aCosEtaB * _cosTheta - bSinEtaB * _sinTheta;
                double yB = Cy + aCosEtaB * _sinTheta + bSinEtaB * _cosTheta;
                double xBDot = -aSinEtaB * _cosTheta - bCosEtaB * _sinTheta;
                double yBDot = -aSinEtaB * _sinTheta + bCosEtaB * _cosTheta;

                /*
                  This controls the drawing in case of pies
                if (openNewFigure)
                {
                    if (IsPieSlice)
                    {
                        path.BeginFigure(new Point(Cx, Cy), false, false);
                        path.LineTo(new Point(xB, yB), true, true);
                    }
                    else
                    {
                        path.BeginFigure(new Point(xB, yB), false, false);
                    }
                }
                else
                {
                    //path.LineTo(new Point(xB, yB), true, true);
                }
                */

                //otherwise we're supposed to be already at the (xB,yB)

                double t = Math.Tan(0.5 * dEta);
                double alpha = Math.Sin(dEta) * (Math.Sqrt(4 + 3 * t * t) - 1) / 3;
                for (int i = 0; i < n; ++i)
                {
                    //double etaA = etaB;
                    double xA = xB;
                    double yA = yB;
                    double xADot = xBDot;
                    double yADot = yBDot;
                    etaB += dEta;
                    cosEtaB = Math.Cos(etaB);
                    sinEtaB = Math.Sin(etaB);
                    aCosEtaB = A * cosEtaB;
                    bSinEtaB = B * sinEtaB;
                    aSinEtaB = A * sinEtaB;
                    bCosEtaB = B * cosEtaB;
                    xB = Cx + aCosEtaB * _cosTheta - bSinEtaB * _sinTheta;
                    yB = Cy + aCosEtaB * _sinTheta + bSinEtaB * _cosTheta;
                    xBDot = -aSinEtaB * _cosTheta - bCosEtaB * _sinTheta;
                    yBDot = -aSinEtaB * _sinTheta + bCosEtaB * _cosTheta;
                    if (degree == 1)
                    {
                        path.LineTo(new Point(xB, yB));
                    }
                    else if (degree == 2)
                    {
                        double k = (yBDot * (xB - xA) - xBDot * (yB - yA)) / (xADot * yBDot - yADot * xBDot);
                        path.QuadraticBezierTo(new Point(xA + k * xADot, yA + k * yADot), new Point(xB, yB));
                    }
                    else
                    {
                        path.CubicBezierTo(
                            new Point(xA + alpha * xADot, yA + alpha * yADot),
                            new Point(xB - alpha * xBDot, yB - alpha * yBDot),
                            new Point(xB, yB)
                            );
                    }
                }
                if (IsPieSlice)
                {
                    path.LineTo(new Point(Cx, Cy));
                }
            }

            /// <summary>
            /// Calculates the angle between two vectors
            /// </summary>
            /// <param name="v1">Vector V1</param>
            /// <param name="v2">Vector V2</param>
            /// <returns>The signed angle between v2 and v1</returns>
            static double GetAngle(Vector v1, Vector v2)
            {
                var scalar = v1 * v2;
                return Math.Atan2(v1.X * v2.Y - v2.X * v1.Y, scalar);
            }

            /// <summary>
            /// Simple matrix used for rotate transforms. 
            /// At some point I did not trust the WPF Matrix struct, and wrote my own simple one -_-
            /// This is supposed to be replaced with proper WPF Matrices everywhere
            /// </summary>
            private struct SimpleMatrix
            {
                private readonly double _a, _b, _c, _d;

                public SimpleMatrix(double a, double b, double c, double d)
                {
                    _a = a;
                    _b = b;
                    _c = c;
                    _d = d;
                }

                public static Point operator *(SimpleMatrix m, Point p)
                {
                    return new Point(m._a * p.X + m._b * p.Y, m._c * p.X + m._d * p.Y);
                }
            }

            /// <summary>
            /// ArcTo Helper for StreamGeometryContext
            /// </summary>
            /// <param name="path">Target path</param>
            /// <param name="p1">Start point</param>
            /// <param name="p2">End point</param>
            /// <param name="size">Ellipse radii</param>
            /// <param name="theta">Ellipse theta (angle measured from the abscissa)</param>
            /// <param name="isLargeArc">Large Arc Indicator</param>
            /// <param name="clockwise">Clockwise direction flag</param>
            public static void BuildArc(IStreamGeometryContextImpl path, Point p1, Point p2, Size size, double theta, bool isLargeArc, bool clockwise)
            {

                // var orthogonalizer = new RotateTransform(-theta);
                var orth = new SimpleMatrix(Math.Cos(theta), Math.Sin(theta), -Math.Sin(theta), Math.Cos(theta));
                var rest = new SimpleMatrix(Math.Cos(theta), -Math.Sin(theta), Math.Sin(theta), Math.Cos(theta));

                // var restorer = orthogonalizer.Inverse;
                // if(restorer == null) throw new InvalidOperationException("Can't get a restorer!");

                Point p1S = orth * (new Point((p1.X - p2.X) / 2, (p1.Y - p2.Y) / 2));

                double rx = size.Width;
                double ry = size.Height;
                double rx2 = rx * rx;
                double ry2 = ry * ry;
                double y1S2 = p1S.Y * p1S.Y;
                double x1S2 = p1S.X * p1S.X;

                double numerator = rx2*ry2 - rx2*y1S2 - ry2*x1S2;
                double denominator = rx2*y1S2 + ry2*x1S2;

                if (Math.Abs(denominator) < 1e-8)
                {
                    path.LineTo(p2);
                    return;
                }
                if ((numerator / denominator) < 0)
                {
                    double lambda = x1S2/rx2 + y1S2/ry2;
                    double lambdaSqrt = Math.Sqrt(lambda);
                    if (lambda > 1)
                    {
                        rx *= lambdaSqrt;
                        ry *= lambdaSqrt;
                        rx2 = rx*rx;
                        ry2 = ry*ry;
                        numerator = rx2 * ry2 - rx2 * y1S2 - ry2 * x1S2;
                        if (numerator < 0)
                            numerator = 0;

                        denominator = rx2 * y1S2 + ry2 * x1S2;
                    }

                }

                double multiplier = Math.Sqrt(numerator / denominator);
                Point mulVec = new Point(rx * p1S.Y / ry, -ry * p1S.X / rx);

                int sign = (clockwise != isLargeArc) ? 1 : -1;

                Point cs = new Point(mulVec.X * multiplier * sign, mulVec.Y * multiplier * sign);

                Vector translation = new Vector((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);

                Point c = rest * (cs) + translation;

                // See "http://www.w3.org/TR/SVG/implnote.html#ArcConversionEndpointToCenter" to understand
                // how the ellipse center is calculated 


                // from here, W3C recommendations from the above link make less sense than Darth Vader pouring
                // some sea water in a water filter while standing in the water confused 

                // Therefore, we are on our own with our task of finding out lambda1 and lambda2
                // matching our points p1 and p2.

                // Fortunately it is not so difficult now, when we already know the ellipse centre.

                // We eliminate the offset, making our ellipse zero-centered, then we eliminate the theta,
                // making its Y and X axes the same as global axes. Then we can easily get our angles using
                // good old school formula for angles between vectors.

                // We should remember that this class expects true angles, and not the t-values for ellipse equation.
                // To understand how t-values are obtained, one should see Etas calculation in the constructor code.

                var p1NoOffset = orth * (p1-c);
                var p2NoOffset = orth * (p2-c);

                // if the arc is drawn clockwise, we swap start and end points
                var revisedP1 = clockwise ? p1NoOffset : p2NoOffset;
                var revisedP2 = clockwise ? p2NoOffset : p1NoOffset;


                var thetaStart = GetAngle(new Vector(1, 0), revisedP1);
                var thetaEnd = GetAngle(new Vector(1, 0), revisedP2);


                // Uncomment this to draw a pie
                // path.LineTo(c, true, true);
                // path.LineTo(clockwise ? p1 : p2, true,true);

                path.LineTo(clockwise ? p1 : p2);
                var arc = new EllipticalArc(c.X, c.Y, rx, ry, theta, thetaStart, thetaEnd, false);
                arc.BuildArc(path, arc._maxDegree, arc._defaultFlatness, false);

                //uncomment this to draw a pie
                //path.LineTo(c, true, true);
            }

            /// <summary>
            /// Tests if the interior of the closed path derived from this arc intersects the interior of a specified rectangular area.
            /// The closed path is derived with respect to the IsPieSlice value.
            /// </summary>
            public bool Intersects(double x, double y, double w, double h)
            {
                double xPlusW = x + w;
                double yPlusH = y + h;
                return Contains(x, y) || Contains(xPlusW, y) || Contains(x, yPlusH) || Contains(xPlusW, yPlusH) ||
                       IntersectOutline(x, y, xPlusW, y) || IntersectOutline(xPlusW,
                           y, xPlusW, yPlusH) || IntersectOutline(xPlusW, yPlusH, x, yPlusH) ||
                       IntersectOutline(x, yPlusH, x, y);
            }

            /// <summary>
            /// Tests if the interior of the closed path derived from this arc intersects the interior of a specified rectangular area.
            /// The closed path is derived with respect to the IsPieSlice value.
            /// </summary>
            public bool Intersects(Rect r)
            {
                return Intersects(r.X, r.Y, r.Width, r.Height);
            }
        }

        public static void ArcTo(IStreamGeometryContextImpl streamGeometryContextImpl, Point currentPoint, Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection)
        {
            EllipticalArc.BuildArc(streamGeometryContextImpl, currentPoint, point, size, rotationAngle*Math.PI/180,
                isLargeArc,
                sweepDirection == SweepDirection.Clockwise);
        }
    }
}
