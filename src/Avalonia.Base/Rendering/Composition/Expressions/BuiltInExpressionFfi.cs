using System;
using System.Collections.Generic;
using System.Numerics;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Utilities;

// Special license applies <see href="https://raw.githubusercontent.com/AvaloniaUI/Avalonia/master/src/Avalonia.Base/Rendering/Composition/License.md">License.md</see>

namespace Avalonia.Rendering.Composition.Expressions
{
    /// <summary>
    /// Built-in functions for Foreign Function Interface available from composition animation expressions
    /// </summary>
    internal class BuiltInExpressionFfi : IExpressionForeignFunctionInterface
    {
        private readonly DelegateExpressionFfi _registry;

        static float Lerp(float a, float b, float p) => p * (b - a) + a;

        static Matrix3x2 Inverse(Matrix3x2 m)
        {
            Matrix3x2.Invert(m, out var r);
            return r;
        }

        static Matrix4x4 Inverse(Matrix4x4 m)
        {
            Matrix4x4.Invert(m, out var r);
            return r;
        }

        static float SmoothStep(float edge0, float edge1, float x)
        {
            var t = MathUtilities.Clamp((x - edge0) / (edge1 - edge0), 0.0f, 1.0f);
            return t * t * (3.0f - 2.0f * t);
        }

        static Vector2 SmoothStep(Vector2 edge0, Vector2 edge1, Vector2 x)
        {
            return new Vector2(
                SmoothStep(edge0.X, edge1.X, x.X),
                SmoothStep(edge0.Y, edge1.Y, x.Y)
                
                );
        }
        static Vector3 SmoothStep(Vector3 edge0, Vector3 edge1, Vector3 x)
        {
            return new Vector3(
                SmoothStep(edge0.X, edge1.X, x.X),
                SmoothStep(edge0.Y, edge1.Y, x.Y),
                SmoothStep(edge0.Z, edge1.Z, x.Z)
                
                );
        }

        static Vector4 SmoothStep(Vector4 edge0, Vector4 edge1, Vector4 x)
        {
            return new Vector4(
                SmoothStep(edge0.X, edge1.X, x.X),
                SmoothStep(edge0.Y, edge1.Y, x.Y),
                SmoothStep(edge0.Z, edge1.Z, x.Z),
                SmoothStep(edge0.W, edge1.W, x.W)
            );
        }

        private BuiltInExpressionFfi()
        {
            _registry = new DelegateExpressionFfi
            {
                {"Abs", (float f) => Math.Abs(f)},
                {"Abs", (Vector2 v) => Vector2.Abs(v)},
                {"Abs", (Vector3 v) => Vector3.Abs(v)},
                {"Abs", (Vector4 v) => Vector4.Abs(v)},

                {"ACos", (float f) => (float) Math.Acos(f)},
                {"ASin", (float f) => (float) Math.Asin(f)},
                {"ATan", (float f) => (float) Math.Atan(f)},
                {"Ceil", (float f) => (float) Math.Ceiling(f)},

                {"Clamp", (float a1, float a2, float a3) => MathUtilities.Clamp(a1, a2, a3)},
                {"Clamp", (Vector2 a1, Vector2 a2, Vector2 a3) => Vector2.Clamp(a1, a2, a3)},
                {"Clamp", (Vector3 a1, Vector3 a2, Vector3 a3) => Vector3.Clamp(a1, a2, a3)},
                {"Clamp", (Vector4 a1, Vector4 a2, Vector4 a3) => Vector4.Clamp(a1, a2, a3)},

                {"Concatenate", (Quaternion a1, Quaternion a2) => Quaternion.Concatenate(a1, a2)},
                {"Cos", (float a) => (float) Math.Cos(a)},

                /*
                TODO:
                    ColorHsl(Float h, Float s, Float l)
                    ColorLerpHSL(Color colorTo, CompositionColorcolorFrom, Float progress)
                */

                {
                    "ColorLerp", (Avalonia.Media.Color to, Avalonia.Media.Color from, float progress) =>
                        ColorInterpolator.LerpRGB(to, from, progress)
                },
                {
                    "ColorLerpRGB", (Avalonia.Media.Color to, Avalonia.Media.Color from, float progress) =>
                        ColorInterpolator.LerpRGB(to, from, progress)
                },
                {
                    "ColorRGB", (float a, float r, float g, float b) => Avalonia.Media.Color.FromArgb(
                        (byte) MathUtilities.Clamp(a, 0, 255),
                        (byte) MathUtilities.Clamp(r, 0, 255),
                        (byte) MathUtilities.Clamp(g, 0, 255),
                        (byte) MathUtilities.Clamp(b, 0, 255)
                    )
                },

                {"Distance", (Vector2 a1, Vector2 a2) => Vector2.Distance(a1, a2)},
                {"Distance", (Vector3 a1, Vector3 a2) => Vector3.Distance(a1, a2)},
                {"Distance", (Vector4 a1, Vector4 a2) => Vector4.Distance(a1, a2)},

                {"DistanceSquared", (Vector2 a1, Vector2 a2) => Vector2.DistanceSquared(a1, a2)},
                {"DistanceSquared", (Vector3 a1, Vector3 a2) => Vector3.DistanceSquared(a1, a2)},
                {"DistanceSquared", (Vector4 a1, Vector4 a2) => Vector4.DistanceSquared(a1, a2)},

                {"Floor", (float v) => (float) Math.Floor(v)},

                {"Inverse", (Matrix3x2 v) => Inverse(v)},
                {"Inverse", (Matrix4x4 v) => Inverse(v)},


                {"Length", (Vector2 a1) => a1.Length()},
                {"Length", (Vector3 a1) => a1.Length()},
                {"Length", (Vector4 a1) => a1.Length()},
                {"Length", (Quaternion a1) => a1.Length()},

                {"LengthSquared", (Vector2 a1) => a1.LengthSquared()},
                {"LengthSquared", (Vector3 a1) => a1.LengthSquared()},
                {"LengthSquared", (Vector4 a1) => a1.LengthSquared()},
                {"LengthSquared", (Quaternion a1) => a1.LengthSquared()},

                {"Lerp", (float a1, float a2, float a3) => Lerp(a1, a2, a3)},
                {"Lerp", (Vector2 a1, Vector2 a2, float a3) => Vector2.Lerp(a1, a2, a3)},
                {"Lerp", (Vector3 a1, Vector3 a2, float a3) => Vector3.Lerp(a1, a2, a3)},
                {"Lerp", (Vector4 a1, Vector4 a2, float a3) => Vector4.Lerp(a1, a2, a3)},


                {"Ln", (float f) => (float) Math.Log(f)},
                {"Log10", (float f) => (float) Math.Log10(f)},

                {"Matrix3x2.CreateFromScale", (Vector2 v) => Matrix3x2.CreateScale(v)},
                {"Matrix3x2.CreateFromTranslation", (Vector2 v) => Matrix3x2.CreateTranslation(v)},
                {"Matrix3x2.CreateRotation", (float v) => Matrix3x2.CreateRotation(v)},
                {"Matrix3x2.CreateScale", (Vector2 v) => Matrix3x2.CreateScale(v)},
                {"Matrix3x2.CreateSkew", (float a1, float a2, Vector2 a3) => Matrix3x2.CreateSkew(a1, a2, a3)},
                {"Matrix3x2.CreateTranslation", (Vector2 v) => Matrix3x2.CreateScale(v)},
                {
                    "Matrix3x2", (float m11, float m12, float m21, float m22, float m31, float m32) =>
                        new Matrix3x2(m11, m12, m21, m22, m31, m32)
                },
                {"Matrix4x4.CreateFromAxisAngle", (Vector3 v, float angle) => Matrix4x4.CreateFromAxisAngle(v, angle)},
                {"Matrix4x4.CreateFromScale", (Vector3 v) => Matrix4x4.CreateScale(v)},
                {"Matrix4x4.CreateFromTranslation", (Vector3 v) => Matrix4x4.CreateTranslation(v)},
                {"Matrix4x4.CreateScale", (Vector3 v) => Matrix4x4.CreateScale(v)},
                {"Matrix4x4.CreateTranslation", (Vector3 v) => Matrix4x4.CreateScale(v)},
                {"Matrix4x4", (Matrix3x2 m) => new Matrix4x4(m)},
                {
                    "Matrix4x4",
                    (float m11, float m12, float m13, float m14,
                            float m21, float m22, float m23, float m24,
                            float m31, float m32, float m33, float m34,
                            float m41, float m42, float m43, float m44) =>
                        new Matrix4x4(
                            m11, m12, m13, m14,
                            m21, m22, m23, m24,
                            m31, m32, m33, m34,
                            m41, m42, m43, m44)
                },


                {"Max", (float a1, float a2) => Math.Max(a1, a2)},
                {"Max", (Vector2 a1, Vector2 a2) => Vector2.Max(a1, a2)},
                {"Max", (Vector3 a1, Vector3 a2) => Vector3.Max(a1, a2)},
                {"Max", (Vector4 a1, Vector4 a2) => Vector4.Max(a1, a2)},


                {"Min", (float a1, float a2) => Math.Min(a1, a2)},
                {"Min", (Vector2 a1, Vector2 a2) => Vector2.Min(a1, a2)},
                {"Min", (Vector3 a1, Vector3 a2) => Vector3.Min(a1, a2)},
                {"Min", (Vector4 a1, Vector4 a2) => Vector4.Min(a1, a2)},

                {"Mod", (float a, float b) => a % b},

                {"Normalize", (Quaternion a) => Quaternion.Normalize(a)},
                {"Normalize", (Vector2 a) => Vector2.Normalize(a)},
                {"Normalize", (Vector3 a) => Vector3.Normalize(a)},
                {"Normalize", (Vector4 a) => Vector4.Normalize(a)},

                {"Pow", (float a, float b) => (float) Math.Pow(a, b)},
                {"Quaternion.CreateFromAxisAngle", (Vector3 a, float b) => Quaternion.CreateFromAxisAngle(a, b)},
                {"Quaternion", (float a, float b, float c, float d) => new Quaternion(a, b, c, d)},

                {"Round", (float a) => (float) Math.Round(a)},

                {"Scale", (Matrix3x2 a, float b) => a * b},
                {"Scale", (Matrix4x4 a, float b) => a * b},
                {"Scale", (Vector2 a, float b) => a * b},
                {"Scale", (Vector3 a, float b) => a * b},
                {"Scale", (Vector4 a, float b) => a * b},

                {"Sin", (float a) => (float) Math.Sin(a)},

                {"SmoothStep", (float a1, float a2, float a3) => SmoothStep(a1, a2, a3)},
                {"SmoothStep", (Vector2 a1, Vector2 a2, Vector2 a3) => SmoothStep(a1, a2, a3)},
                {"SmoothStep", (Vector3 a1, Vector3 a2, Vector3 a3) => SmoothStep(a1, a2, a3)},
                {"SmoothStep", (Vector4 a1, Vector4 a2, Vector4 a3) => SmoothStep(a1, a2, a3)},

                // I have no idea how to do a spherical interpolation for a scalar value, so we are doing a linear one
                {"Slerp", (float a1, float a2, float a3) => Lerp(a1, a2, a3)},
                {"Slerp", (Quaternion a1, Quaternion a2, float a3) => Quaternion.Slerp(a1, a2, a3)},

                {"Sqrt", (float a) => (float) Math.Sqrt(a)},
                {"Square", (float a) => a * a},
                {"Tan", (float a) => (float) Math.Tan(a)},

                {"ToRadians", (float a) => (float) (a * Math.PI / 180)},
                {"ToDegrees", (float a) => (float) (a * 180d / Math.PI)},

                {"Transform", (Vector2 a, Matrix3x2 b) => Vector2.Transform(a, b)},
                {"Transform", (Vector3 a, Matrix4x4 b) => Vector3.Transform(a, b)},

                {"Vector2", (float a, float b) => new Vector2(a, b)},
                {"Vector3", (float a, float b, float c) => new Vector3(a, b, c)},
                {"Vector3", (Vector2 v2, float z) => new Vector3(v2, z)},
                {"Vector4", (float a, float b, float c, float d) => new Vector4(a, b, c, d)},
                {"Vector4", (Vector2 v2, float z, float w) => new Vector4(v2, z, w)},
                {"Vector4", (Vector3 v3, float w) => new Vector4(v3, w)},
            };
        }

        public bool Call(string name, IReadOnlyList<ExpressionVariant> arguments, out ExpressionVariant result) =>
            _registry.Call(name, arguments, out result);

        public static BuiltInExpressionFfi Instance { get; } = new BuiltInExpressionFfi();
    }
}
