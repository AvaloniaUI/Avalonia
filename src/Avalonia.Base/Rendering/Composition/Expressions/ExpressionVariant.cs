using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using Avalonia.Media;

namespace Avalonia.Rendering.Composition.Expressions
{
    internal enum VariantType
    {
        Invalid,
        Boolean,
        Double,
        Vector2,
        Vector3,
        Vector4,
        Vector,
        Vector3D,
        AvaloniaMatrix,
        Matrix3x2,
        Matrix4x4,
        Quaternion,
        Color,
        RelativePoint,
        RelativeScalar, 
        RelativeUnit
    }

    /// <summary>
    /// A VARIANT type used in expression animations. Can represent multiple value types
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct ExpressionVariant
    {
        [FieldOffset(0)] public VariantType Type;

        [FieldOffset(4)] public bool Boolean;
        [FieldOffset(4)] public double Double;
        [FieldOffset(4)] public Vector2 Vector2;
        [FieldOffset(4)] public Vector3 Vector3;
        [FieldOffset(4)] public Vector4 Vector4;
        [FieldOffset(4)] public Vector Vector;
        [FieldOffset(4)] public Vector3D Vector3D;
        [FieldOffset(4)] public Matrix AvaloniaMatrix;
        [FieldOffset(4)] public Matrix3x2 Matrix3x2;
        [FieldOffset(4)] public Matrix4x4 Matrix4x4;
        [FieldOffset(4)] public Quaternion Quaternion;
        [FieldOffset(4)] public Color Color;
        [FieldOffset(4)] public RelativePoint RelativePoint;
        [FieldOffset(4)] public RelativeScalar RelativeScalar;
        [FieldOffset(4)] public RelativeUnit RelativeUnit;

        public ExpressionVariant GetProperty(string property)
        {
            if (Type == VariantType.Vector2)
            {
                if (IsMatch(property, "X"))
                    return Vector2.X;
                if (IsMatch(property, "Y"))
                    return Vector2.Y;
                return default;
            }

            if (Type == VariantType.Vector)
            {
                if (IsMatch(property, "X"))
                    return Vector.X;
                if (IsMatch(property, "Y"))
                    return Vector.Y;
                return default;
            }

            if (Type == VariantType.Vector3)
            {
                if (IsMatch(property, "X"))
                    return Vector3.X;
                if (IsMatch(property, "Y"))
                    return Vector3.Y;
                if (IsMatch(property, "Z"))
                    return Vector3.Z;
                if (IsMatch(property, "XY"))
                    return new Vector2(Vector3.X, Vector3.Y);
                if (IsMatch(property, "YX"))
                    return new Vector2(Vector3.Y, Vector3.X);
                if (IsMatch(property, "XZ"))
                    return new Vector2(Vector3.X, Vector3.Z);
                if (IsMatch(property, "ZX"))
                    return new Vector2(Vector3.Z, Vector3.X);
                if (IsMatch(property, "YZ"))
                    return new Vector2(Vector3.Y, Vector3.Z);
                if (IsMatch(property, "ZY"))
                    return new Vector2(Vector3.Z, Vector3.Y);
                return default;
            }

            if (Type == VariantType.Vector3D)
            {
                if (IsMatch(property, "X"))
                    return Vector3D.X;
                if (IsMatch(property, "Y"))
                    return Vector3D.Y;
                if (IsMatch(property, "Z"))
                    return Vector3D.Z;
                if (IsMatch(property, "XY"))
                    return new Vector(Vector3D.X, Vector3D.Y);
                if (IsMatch(property, "YX"))
                    return new Vector(Vector3D.Y, Vector3D.X);
                if (IsMatch(property, "XZ"))
                    return new Vector(Vector3D.X, Vector3D.Z);
                if (IsMatch(property, "ZX"))
                    return new Vector(Vector3D.Z, Vector3D.X);
                if (IsMatch(property, "YZ"))
                    return new Vector(Vector3D.Y, Vector3D.Z);
                if (IsMatch(property, "ZY"))
                    return new Vector(Vector3D.Z, Vector3D.Y);
                return default;
            }

            if (Type == VariantType.Vector4)
            {
                if (IsMatch(property, "X"))
                    return Vector4.X;
                if (IsMatch(property, "Y"))
                    return Vector4.Y;
                if (IsMatch(property, "Z"))
                    return Vector4.Z;
                if (IsMatch(property, "W"))
                    return Vector4.W;
                return default;
            }

            if (Type == VariantType.Matrix3x2)
            {
                if (IsMatch(property, "M11"))
                    return Matrix3x2.M11;
                if (IsMatch(property, "M12"))
                    return Matrix3x2.M12;
                if (IsMatch(property, "M21"))
                    return Matrix3x2.M21;
                if (IsMatch(property, "M22"))
                    return Matrix3x2.M22;
                if (IsMatch(property, "M31"))
                    return Matrix3x2.M31;
                if (IsMatch(property, "M32"))
                    return Matrix3x2.M32;
                return default;
            }

            if (Type == VariantType.AvaloniaMatrix)
            {
                if (IsMatch(property, "M11"))
                    return AvaloniaMatrix.M11;
                if (IsMatch(property, "M12"))
                    return AvaloniaMatrix.M12;
                if (IsMatch(property, "M13"))
                    return AvaloniaMatrix.M13;
                if (IsMatch(property, "M21"))
                    return AvaloniaMatrix.M21;
                if (IsMatch(property, "M22"))
                    return AvaloniaMatrix.M22;
                if (IsMatch(property, "M23"))
                    return AvaloniaMatrix.M23;
                if (IsMatch(property, "M31"))
                    return AvaloniaMatrix.M31;
                if (IsMatch(property, "M32"))
                    return AvaloniaMatrix.M32;
                if (IsMatch(property, "M33"))
                    return AvaloniaMatrix.M33;
                return default;
            }

            if (Type == VariantType.Matrix4x4)
            {
                if (IsMatch(property, "M11"))
                    return Matrix4x4.M11;
                if (IsMatch(property, "M12"))
                    return Matrix4x4.M12;
                if (IsMatch(property, "M13"))
                    return Matrix4x4.M13;
                if (IsMatch(property, "M14"))
                    return Matrix4x4.M14;
                if (IsMatch(property, "M21"))
                    return Matrix4x4.M21;
                if (IsMatch(property, "M22"))
                    return Matrix4x4.M22;
                if (IsMatch(property, "M23"))
                    return Matrix4x4.M23;
                if (IsMatch(property, "M24"))
                    return Matrix4x4.M24;
                if (IsMatch(property, "M31"))
                    return Matrix4x4.M31;
                if (IsMatch(property, "M32"))
                    return Matrix4x4.M32;
                if (IsMatch(property, "M33"))
                    return Matrix4x4.M33;
                if (IsMatch(property, "M34"))
                    return Matrix4x4.M34;
                if (IsMatch(property, "M41"))
                    return Matrix4x4.M41;
                if (IsMatch(property, "M42"))
                    return Matrix4x4.M42;
                if (IsMatch(property, "M43"))
                    return Matrix4x4.M43;
                if (IsMatch(property, "M44"))
                    return Matrix4x4.M44;
                return default;
            }

            if (Type == VariantType.Quaternion)
            {
                if (IsMatch(property, "X"))
                    return Quaternion.X;
                if (IsMatch(property, "Y"))
                    return Quaternion.Y;
                if (IsMatch(property, "Z"))
                    return Quaternion.Z;
                if (IsMatch(property, "W"))
                    return Quaternion.W;
                return default;
            }

            if (Type == VariantType.Color)
            {
                if (IsMatch(property, "A"))
                    return Color.A;
                if (IsMatch(property, "R"))
                    return Color.R;
                if (IsMatch(property, "G"))
                    return Color.G;
                if (IsMatch(property, "B"))
                    return Color.B;
                return default;
            }

            if (Type == VariantType.RelativePoint)
            {
                if (ReferenceEquals(property, "X"))
                    return (float)RelativePoint.Point.X;
                if (ReferenceEquals(property, "Y"))
                    return (float)RelativePoint.Point.Y;
                if (ReferenceEquals(property, "Unit"))
                    return RelativePoint.Unit;
                return default;
            }

            if (Type == VariantType.RelativeScalar)
            {
                if (ReferenceEquals(property, "Scalar"))
                    return RelativeScalar.Scalar;
                if (ReferenceEquals(property, "Unit"))
                    return RelativeScalar.Unit;
                return default;
            }

            return default;
        }

        static bool IsMatch(string propertyName, string memberName) =>
            string.Equals(propertyName, memberName, StringComparison.Ordinal);

        public static implicit operator ExpressionVariant(bool value) =>
            new ExpressionVariant
            {
                Type = VariantType.Boolean,
                Boolean = value
            };
        
        public static implicit operator ExpressionVariant(double d) =>
            new ExpressionVariant
            {
                Type = VariantType.Double,
                Double = d
            };


        public static implicit operator ExpressionVariant(Vector2 value) =>
            new ExpressionVariant
            {
                Type = VariantType.Vector2,
                Vector2 = value
            };

        public static implicit operator ExpressionVariant(Vector value) =>
            new ExpressionVariant
            {
                Type = VariantType.Vector,
                Vector = value
            };

        public static implicit operator ExpressionVariant(Vector3 value) =>
            new ExpressionVariant
            {
                Type = VariantType.Vector3,
                Vector3 = value
            };

        public static implicit operator ExpressionVariant(Vector3D value) =>
            new ExpressionVariant
            {
                Type = VariantType.Vector3D,
                Vector3D = value
            };


        public static implicit operator ExpressionVariant(Vector4 value) =>
            new ExpressionVariant
            {
                Type = VariantType.Vector4,
                Vector4 = value
            };

        public static implicit operator ExpressionVariant(Matrix3x2 value) =>
            new ExpressionVariant
            {
                Type = VariantType.Matrix3x2,
                Matrix3x2 = value
            };

        public static implicit operator ExpressionVariant(Matrix value) =>
            new ExpressionVariant
            {
                Type = VariantType.AvaloniaMatrix,
                AvaloniaMatrix = value
            };

        public static implicit operator ExpressionVariant(Matrix4x4 value) =>
            new ExpressionVariant
            {
                Type = VariantType.Matrix4x4,
                Matrix4x4 = value
            };

        public static implicit operator ExpressionVariant(Quaternion value) =>
            new ExpressionVariant
            {
                Type = VariantType.Quaternion,
                Quaternion = value
            };

        public static implicit operator ExpressionVariant(Avalonia.Media.Color value) =>
            new ExpressionVariant
            {
                Type = VariantType.Color,
                Color = value
            };

        public static implicit operator ExpressionVariant(RelativePoint value) =>
            new ExpressionVariant
            {
                Type = VariantType.RelativePoint,
                RelativePoint = value
            };

        public static implicit operator ExpressionVariant(RelativeScalar value) =>
            new ExpressionVariant
            {
                Type = VariantType.RelativeScalar,
                RelativeScalar = value
            };

        public static implicit operator ExpressionVariant(RelativeUnit value) =>
            new ExpressionVariant
            {
                Type = VariantType.RelativeUnit,
                RelativeUnit = value
            };

        public static ExpressionVariant operator +(ExpressionVariant left, ExpressionVariant right)
        {
            if (left.Type != right.Type || left.Type == VariantType.Invalid)
                return default;

            if (left.Type == VariantType.Double)
                return left.Double + right.Double;

            if (left.Type == VariantType.Vector2)
                return left.Vector2 + right.Vector2;

            if (left.Type == VariantType.Vector)
                return left.Vector + right.Vector;

            if (left.Type == VariantType.Vector3)
                return left.Vector3 + right.Vector3;

            if (left.Type == VariantType.Vector3D)
                return Avalonia.Vector3D.Add(left.Vector3D, right.Vector3D);

            if (left.Type == VariantType.Vector4)
                return left.Vector4 + right.Vector4;

            if (left.Type == VariantType.Matrix3x2)
                return left.Matrix3x2 + right.Matrix3x2;

            if (left.Type == VariantType.Matrix4x4)
                return left.Matrix4x4 + right.Matrix4x4;

            if (left.Type == VariantType.Quaternion)
                return left.Quaternion + right.Quaternion;

            if (left.Type == VariantType.RelativePoint && left.RelativePoint.Unit == right.RelativePoint.Unit)
            {
                return new RelativePoint(
                    left.RelativePoint.Point.X + right.RelativePoint.Point.X,
                    left.RelativePoint.Point.Y + right.RelativePoint.Point.Y,
                    left.RelativePoint.Unit);
            }

            if (left.Type == VariantType.RelativeScalar && left.RelativeScalar.Unit == right.RelativeScalar.Unit)
            {
                return new RelativeScalar(left.RelativeScalar.Scalar + right.RelativeScalar.Scalar, left.RelativeScalar.Unit);
            }

            return default;
        }

        public static ExpressionVariant operator -(ExpressionVariant left, ExpressionVariant right)
        {
            if (left.Type != right.Type || left.Type == VariantType.Invalid)
                return default;

            if (left.Type == VariantType.Double)
                return left.Double - right.Double;

            if (left.Type == VariantType.Vector2)
                return left.Vector2 - right.Vector2;

            if (left.Type == VariantType.Vector)
                return left.Vector - right.Vector;

            if (left.Type == VariantType.Vector3)
                return left.Vector3 - right.Vector3;

            if (left.Type == VariantType.Vector3D)
                return Vector3D.Add(left.Vector3D, -right.Vector3D);

            if (left.Type == VariantType.Vector4)
                return left.Vector4 - right.Vector4;

            if (left.Type == VariantType.Matrix3x2)
                return left.Matrix3x2 - right.Matrix3x2;

            if (left.Type == VariantType.Matrix4x4)
                return left.Matrix4x4 - right.Matrix4x4;

            if (left.Type == VariantType.Quaternion)
                return left.Quaternion - right.Quaternion;

            if (left.Type == VariantType.RelativePoint && left.RelativePoint.Unit == right.RelativePoint.Unit)
            {
                return new RelativePoint(
                    left.RelativePoint.Point.X - right.RelativePoint.Point.X,
                    left.RelativePoint.Point.Y - right.RelativePoint.Point.Y,
                    left.RelativePoint.Unit);
            }

            if (left.Type == VariantType.RelativeScalar && left.RelativeScalar.Unit == right.RelativeScalar.Unit)
            {
                return new RelativeScalar(left.RelativeScalar.Scalar - right.RelativeScalar.Scalar, left.RelativeScalar.Unit);
            }

            return default;
        }

        public static ExpressionVariant operator -(ExpressionVariant left)
        {
            
            if (left.Type == VariantType.Double)
                return -left.Double;

            if (left.Type == VariantType.Vector2)
                return -left.Vector2;

            if (left.Type == VariantType.Vector)
                return -left.Vector;

            if (left.Type == VariantType.Vector3)
                return -left.Vector3;

            if (left.Type == VariantType.Vector3D)
                return -left.Vector3D;

            if (left.Type == VariantType.Vector4)
                return -left.Vector4;

            if (left.Type == VariantType.Matrix3x2)
                return -left.Matrix3x2;

            if (left.Type == VariantType.AvaloniaMatrix)
                return -left.AvaloniaMatrix;

            if (left.Type == VariantType.Matrix4x4)
                return -left.Matrix4x4;

            if (left.Type == VariantType.Quaternion)
                return -left.Quaternion;

            if (left.Type == VariantType.RelativePoint)
                return new RelativePoint(-left.RelativePoint.Point.X, -left.RelativePoint.Point.Y, left.RelativePoint.Unit);

            if (left.Type == VariantType.RelativeScalar)
                return new RelativeScalar(-left.RelativeScalar.Scalar, left.RelativeScalar.Unit);

            return default;
        }

        public static ExpressionVariant operator *(ExpressionVariant left, ExpressionVariant right)
        {
            if (left.Type == VariantType.Invalid || right.Type == VariantType.Invalid)
                return default;

            if (left.Type == VariantType.Double && right.Type == VariantType.Double)
                return left.Double * right.Double;

            if (left.Type == VariantType.Vector2 && right.Type == VariantType.Vector2)
                return left.Vector2 * right.Vector2;

            if (left.Type == VariantType.Vector && right.Type == VariantType.Vector)
                return Vector.Multiply(left.Vector, right.Vector);

            if (left.Type == VariantType.Vector2 && right.Type == VariantType.Double)
                return left.Vector2 * (float)right.Double;

            if (left.Type == VariantType.Vector && right.Type == VariantType.Double)
                return left.Vector * right.Double;

            if (left.Type == VariantType.Vector3 && right.Type == VariantType.Vector3)
                return left.Vector3 * right.Vector3;

            if (left.Type == VariantType.Vector3D && right.Type == VariantType.Vector3D)
                return Vector3D.Multiply(left.Vector3D, right.Vector3D);

            if (left.Type == VariantType.Vector3 && right.Type == VariantType.Double)
                return left.Vector3 * (float)right.Double;

            if (left.Type == VariantType.Vector3D && right.Type == VariantType.Double)
                return Vector3D.Multiply(left.Vector3D, right.Double);

            if (left.Type == VariantType.Vector4 && right.Type == VariantType.Vector4)
                return left.Vector4 * right.Vector4;

            if (left.Type == VariantType.Vector4 && right.Type == VariantType.Double)
                return left.Vector4 * (float)right.Double;

            if (left.Type == VariantType.Matrix3x2 && right.Type == VariantType.Matrix3x2)
                return left.Matrix3x2 * right.Matrix3x2;

            if (left.Type == VariantType.Matrix3x2 && right.Type == VariantType.Double)
                return left.Matrix3x2 * (float)right.Double;

            if (left.Type == VariantType.AvaloniaMatrix && right.Type == VariantType.AvaloniaMatrix)
                return left.AvaloniaMatrix * right.AvaloniaMatrix;

            if (left.Type == VariantType.Matrix4x4 && right.Type == VariantType.Matrix4x4)
                return left.Matrix4x4 * right.Matrix4x4;

            if (left.Type == VariantType.Matrix4x4 && right.Type == VariantType.Double)
                return left.Matrix4x4 * (float)right.Double;

            if (left.Type == VariantType.Quaternion && right.Type == VariantType.Quaternion)
                return left.Quaternion * right.Quaternion;

            if (left.Type == VariantType.Quaternion && right.Type == VariantType.Double)
                return left.Quaternion * (float)right.Double;

            if (left.Type == VariantType.RelativePoint && right.Type == VariantType.Double)
                return new RelativePoint(left.RelativePoint.Point.X * right.Double, left.RelativePoint.Point.Y * right.Double, left.RelativePoint.Unit);

            if (left.Type == VariantType.Double && right.Type == VariantType.RelativePoint)
                return new RelativePoint(left.Double * right.RelativePoint.Point.X, left.Double * right.RelativePoint.Point.Y, right.RelativePoint.Unit);

            if (left.Type == VariantType.RelativeScalar && right.Type == VariantType.Double)
                return new RelativeScalar(left.RelativeScalar.Scalar * right.Double, left.RelativeScalar.Unit);

            if (left.Type == VariantType.Double && right.Type == VariantType.RelativeScalar)
                return new RelativeScalar(left.Double * right.RelativeScalar.Scalar, right.RelativeScalar.Unit);

            if (left.Type == VariantType.RelativePoint && right.Type == VariantType.RelativePoint && left.RelativePoint.Unit == right.RelativePoint.Unit)
            {
                return new RelativePoint(
                    left.RelativePoint.Point.X * right.RelativePoint.Point.X,
                    left.RelativePoint.Point.Y * right.RelativePoint.Point.Y,
                    left.RelativePoint.Unit);
            }

            if (left.Type == VariantType.RelativeScalar && right.Type == VariantType.RelativeScalar && left.RelativeScalar.Unit == right.RelativeScalar.Unit)
                return new RelativeScalar(left.RelativeScalar.Scalar * right.RelativeScalar.Scalar, left.RelativeScalar.Unit);

            return default;
        }

        public static ExpressionVariant operator /(ExpressionVariant left, ExpressionVariant right)
        {
            if (left.Type == VariantType.Invalid || right.Type == VariantType.Invalid)
                return default;

            if (left.Type == VariantType.Double && right.Type == VariantType.Double)
                return left.Double / right.Double;

            if (left.Type == VariantType.Vector2 && right.Type == VariantType.Vector2)
                return left.Vector2 / right.Vector2;

            if (left.Type == VariantType.Vector && right.Type == VariantType.Vector)
                return Vector.Divide(left.Vector, right.Vector);

            if (left.Type == VariantType.Vector2 && right.Type == VariantType.Double)
                return left.Vector2 / (float)right.Double;

            if (left.Type == VariantType.Vector && right.Type == VariantType.Double)
                return left.Vector / right.Double;

            if (left.Type == VariantType.Vector3 && right.Type == VariantType.Vector3)
                return left.Vector3 / right.Vector3;

            if (left.Type == VariantType.Vector3D && right.Type == VariantType.Vector3D)
                return Vector3D.Divide(left.Vector3D, right.Vector3D);

            if (left.Type == VariantType.Vector3 && right.Type == VariantType.Double)
                return left.Vector3 / (float)right.Double;
             
            if (left.Type == VariantType.Vector3D && right.Type == VariantType.Double)
                return Avalonia.Vector3D.Divide(left.Vector3D, right.Double);

            if (left.Type == VariantType.Vector4 && right.Type == VariantType.Vector4)
                return left.Vector4 / right.Vector4;

            if (left.Type == VariantType.Vector4 && right.Type == VariantType.Double)
                return left.Vector4 / (float)right.Double;

            if (left.Type == VariantType.Quaternion && right.Type == VariantType.Quaternion)
                return left.Quaternion / right.Quaternion;

            if (left.Type == VariantType.RelativePoint && right.Type == VariantType.Double)
                return new RelativePoint(left.RelativePoint.Point.X / right.Double, left.RelativePoint.Point.Y / right.Double, left.RelativePoint.Unit);


            if (left.Type == VariantType.RelativeScalar && right.Type == VariantType.Double)
                return new RelativeScalar(left.RelativeScalar.Scalar / right.Double, left.RelativeScalar.Unit);

            if (left.Type == VariantType.RelativePoint && right.Type == VariantType.RelativePoint && left.RelativePoint.Unit == right.RelativePoint.Unit)
            {
                return new RelativePoint(
                    left.RelativePoint.Point.X / right.RelativePoint.Point.X,
                    left.RelativePoint.Point.Y / right.RelativePoint.Point.Y,
                    left.RelativePoint.Unit);
            }

            if (left.Type == VariantType.RelativeScalar && right.Type == VariantType.RelativeScalar && left.RelativeScalar.Unit == right.RelativeScalar.Unit)
                return new RelativeScalar(left.RelativeScalar.Scalar / right.RelativeScalar.Scalar, left.RelativeScalar.Unit);

            return default;
        }

        public ExpressionVariant EqualsTo(ExpressionVariant right)
        {
            if (Type != right.Type || Type == VariantType.Invalid)
                return default;       
            
            if (Type == VariantType.Double)
                return Double == right.Double;

            if (Type == VariantType.Vector2)
                return Vector2 == right.Vector2;

            if (Type == VariantType.Vector)
                return Vector == right.Vector;

            if (Type == VariantType.Vector3)
                return Vector3 == right.Vector3;

            if (Type == VariantType.Vector3D)
                return Vector3D == right.Vector3D;

            if (Type == VariantType.Vector4)
                return Vector4 == right.Vector4;

            if (Type == VariantType.Boolean)
                return Boolean == right.Boolean;

            if (Type == VariantType.Matrix3x2)
                return Matrix3x2 == right.Matrix3x2;

            if (Type == VariantType.AvaloniaMatrix)
                return AvaloniaMatrix == right.AvaloniaMatrix;

            if (Type == VariantType.Matrix4x4)
                return Matrix4x4 == right.Matrix4x4;

            if (Type == VariantType.Quaternion)
                return Quaternion == right.Quaternion;

            if (Type == VariantType.RelativePoint)
                return RelativePoint == right.RelativePoint;

            if (Type == VariantType.RelativeScalar)
                return RelativeScalar == right.RelativeScalar;

            if (Type == VariantType.RelativeUnit)
                return RelativeUnit == right.RelativeUnit;

            return default;
        }

        public ExpressionVariant NotEqualsTo(ExpressionVariant right)
        {
            var r = EqualsTo(right);
            if (r.Type == VariantType.Boolean)
                return !r.Boolean;
            return default;
        }

        public static ExpressionVariant operator !(ExpressionVariant v)
        {
            if (v.Type == VariantType.Boolean)
                return !v.Boolean;
            return default;
        }

        public static ExpressionVariant operator %(ExpressionVariant left, ExpressionVariant right)
        {
            if (left.Type == VariantType.Double && right.Type == VariantType.Double)
                return left.Double % right.Double;
            if (left.Type == VariantType.RelativeScalar && right.Type == VariantType.RelativeScalar && left.RelativeScalar.Unit == right.RelativeScalar.Unit)
                return new RelativeScalar(left.RelativeScalar.Scalar % right.RelativeScalar.Scalar, left.RelativeScalar.Unit);
            return default;
        }

        public static ExpressionVariant operator <(ExpressionVariant left, ExpressionVariant right)
        {
            if (left.Type == VariantType.Double && right.Type == VariantType.Double)
                return left.Double < right.Double;
            if (left.Type == VariantType.RelativeScalar && right.Type == VariantType.RelativeScalar && left.RelativeScalar.Unit == right.RelativeScalar.Unit)
                return left.RelativeScalar.Scalar < right.RelativeScalar.Scalar;
            return default;
        }

        public static ExpressionVariant operator >(ExpressionVariant left, ExpressionVariant right)
        {            
            if (left.Type == VariantType.Double && right.Type == VariantType.Double)
                return left.Double > right.Double;
            if (left.Type == VariantType.RelativeScalar && right.Type == VariantType.RelativeScalar && left.RelativeScalar.Unit == right.RelativeScalar.Unit)
                return left.RelativeScalar.Scalar > right.RelativeScalar.Scalar;
            return default;
        }

        public ExpressionVariant And(ExpressionVariant right)
        {
            if (Type == VariantType.Boolean && right.Type == VariantType.Boolean)
                return Boolean && right.Boolean;
            return default;
        }

        public ExpressionVariant Or(ExpressionVariant right)
        {
            if (Type == VariantType.Boolean && right.Type == VariantType.Boolean)
                return Boolean || right.Boolean;
            return default;
        }

        public bool TryCast<T>(out T res) where T : struct
        {
            switch (default(T))
            {
                case bool when Type is VariantType.Boolean:
                    res = (T)(object)Boolean;
                    return true;
                case float when Type is VariantType.Double:
                    res = (T)(object)(float)Double;
                    return true;
                case double when Type is VariantType.Double:
                    res = (T)(object)Double;
                    return true;
                case System.Numerics.Vector2 when Type is VariantType.Vector2:
                    res = (T)(object)Vector2;
                    return true;
                case System.Numerics.Vector2 when Type is VariantType.Vector:
                    res = (T)(object)Vector.ToVector2();
                    return true;
                case Avalonia.Vector when Type is VariantType.Vector:
                    res = (T)(object)Vector;
                    return true;
                case Avalonia.Vector when Type is VariantType.Vector2:
                    res = (T)(object)new Vector(Vector2);
                    return true;
                case System.Numerics.Vector3 when Type is VariantType.Vector3:
                    res = (T)(object)Vector3;
                    return true;
                case System.Numerics.Vector3 when Type is VariantType.Vector3D:
                    res = (T)(object)Vector3D.ToVector3();
                    return true;
                case Avalonia.Vector3D when Type is VariantType.Vector3D:
                    res = (T)(object)Vector3D;
                    return true;
                case Avalonia.Vector3D when Type is VariantType.Vector3:
                    res = (T)(object)new Vector3D(Vector3);
                    return true;
                case System.Numerics.Vector4 when Type is VariantType.Vector4:
                    res = (T)(object)Vector4;
                    return true;
                case System.Numerics.Matrix3x2 when Type is VariantType.Matrix3x2:
                    res = (T)(object)Matrix3x2;
                    return true;
                case Avalonia.Matrix when Type is VariantType.AvaloniaMatrix:
                    res = (T)(object)AvaloniaMatrix;
                    return true;
                case System.Numerics.Matrix4x4 when Type is VariantType.Matrix4x4:
                    res = (T)(object)Matrix4x4;
                    return true;
                case System.Numerics.Quaternion when Type is VariantType.Quaternion:
                    res = (T)(object)Quaternion;
                    return true;
                case Avalonia.Media.Color when Type is VariantType.Color:
                    res = (T)(object)Color;
                    return true;
                default:
                    res = default;
                    return false;
            }
        }

        public static ExpressionVariant Create<T>(T v) where T : struct
            => default(T) switch
            {
                bool => (bool)(object)v,
                float => (float)(object)v,
                double => (double)(object)v,
                System.Numerics.Vector2 => (Vector2)(object)v,
                Avalonia.Vector => (Vector)(object)v,
                System.Numerics.Vector3 => (Vector3)(object)v,
                Avalonia.Vector3D => (Vector3D)(object)v,
                System.Numerics.Vector4 => (Vector4)(object)v,
                System.Numerics.Matrix3x2 => (Matrix3x2)(object)v,
                Avalonia.Matrix => (Matrix)(object)v,
                System.Numerics.Matrix4x4 => (Matrix4x4)(object)v,
                System.Numerics.Quaternion => (Quaternion)(object)v,
                Avalonia.Media.Color => (Avalonia.Media.Color)(object)v,
                _ => throw new ArgumentException("Invalid variant type: " + typeof(T))
            };

        public T CastOrDefault<T>() where T : struct
        {
            TryCast<T>(out var r);
            return r;
        }

        public override string ToString()
        {
            return Type switch
            {
                VariantType.Boolean => Boolean.ToString(),
                VariantType.Double => Double.ToString(CultureInfo.InvariantCulture),
                VariantType.Vector2 => Vector2.ToString(),
                VariantType.Vector => Vector.ToString(),
                VariantType.Vector3 => Vector3.ToString(),
                VariantType.Vector3D => Vector3D.ToString(),
                VariantType.Vector4 => Vector4.ToString(),
                VariantType.Quaternion => Quaternion.ToString(),
                VariantType.Matrix3x2 => Matrix3x2.ToString(),
                VariantType.AvaloniaMatrix => AvaloniaMatrix.ToString(),
                VariantType.Matrix4x4 => Matrix4x4.ToString(),
                VariantType.Color => Color.ToString(),
                VariantType.Invalid => "Invalid",
                _ => "Unknown"
            };
        }
    }

}
