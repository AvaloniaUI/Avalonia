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
        Scalar,
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
        Color
    }

    /// <summary>
    /// A VARIANT type used in expression animations. Can represent multiple value types
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct ExpressionVariant
    {
        [FieldOffset(0)] public VariantType Type;

        [FieldOffset(4)] public bool Boolean;
        [FieldOffset(4)] public float Scalar;
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
        

        public ExpressionVariant GetProperty(string property)
        {
            if (Type == VariantType.Vector2)
            {
                if (ReferenceEquals(property, "X"))
                    return Vector2.X;
                if (ReferenceEquals(property, "Y"))
                    return Vector2.Y;
                return default;
            }
            
            if (Type == VariantType.Vector)
            {
                if (ReferenceEquals(property, "X"))
                    return Vector.X;
                if (ReferenceEquals(property, "Y"))
                    return Vector.Y;
                return default;
            }

            if (Type == VariantType.Vector3)
            {
                if (ReferenceEquals(property, "X"))
                    return Vector3.X;
                if (ReferenceEquals(property, "Y"))
                    return Vector3.Y;
                if (ReferenceEquals(property, "Z"))
                    return Vector3.Z;
                if(ReferenceEquals(property, "XY"))
                    return new Vector2(Vector3.X, Vector3.Y);
                if(ReferenceEquals(property, "YX"))
                    return new Vector2(Vector3.Y, Vector3.X);
                if(ReferenceEquals(property, "XZ"))
                    return new Vector2(Vector3.X, Vector3.Z);
                if(ReferenceEquals(property, "ZX"))
                    return new Vector2(Vector3.Z, Vector3.X);
                if(ReferenceEquals(property, "YZ"))
                    return new Vector2(Vector3.Y, Vector3.Z);
                if(ReferenceEquals(property, "ZY"))
                    return new Vector2(Vector3.Z, Vector3.Y);
                return default;
            }
            
            if (Type == VariantType.Vector3D)
            {
                if (ReferenceEquals(property, "X"))
                    return Vector3D.X;
                if (ReferenceEquals(property, "Y"))
                    return Vector3D.Y;
                if (ReferenceEquals(property, "Z"))
                    return Vector3D.Z;
                if(ReferenceEquals(property, "XY"))
                    return new Vector(Vector3D.X, Vector3D.Y);
                if(ReferenceEquals(property, "YX"))
                    return new Vector(Vector3D.Y, Vector3D.X);
                if(ReferenceEquals(property, "XZ"))
                    return new Vector(Vector3D.X, Vector3D.Z);
                if(ReferenceEquals(property, "ZX"))
                    return new Vector(Vector3D.Z, Vector3D.X);
                if(ReferenceEquals(property, "YZ"))
                    return new Vector(Vector3D.Y, Vector3D.Z);
                if(ReferenceEquals(property, "ZY"))
                    return new Vector(Vector3D.Z, Vector3D.Y);
                return default;
            }

            if (Type == VariantType.Vector4)
            {
                if (ReferenceEquals(property, "X"))
                    return Vector4.X;
                if (ReferenceEquals(property, "Y"))
                    return Vector4.Y;
                if (ReferenceEquals(property, "Z"))
                    return Vector4.Z;
                if (ReferenceEquals(property, "W"))
                    return Vector4.W;
                return default;
            }

            if (Type == VariantType.Matrix3x2)
            {
                if (ReferenceEquals(property, "M11"))
                    return Matrix3x2.M11;
                if (ReferenceEquals(property, "M12"))
                    return Matrix3x2.M12;
                if (ReferenceEquals(property, "M21"))
                    return Matrix3x2.M21;
                if (ReferenceEquals(property, "M22"))
                    return Matrix3x2.M22;
                if (ReferenceEquals(property, "M31"))
                    return Matrix3x2.M31;
                if (ReferenceEquals(property, "M32"))
                    return Matrix3x2.M32;
                return default;
            }
            
            if (Type == VariantType.AvaloniaMatrix)
            {
                if (ReferenceEquals(property, "M11"))
                    return AvaloniaMatrix.M11;
                if (ReferenceEquals(property, "M12"))
                    return AvaloniaMatrix.M12;
                if (ReferenceEquals(property, "M13"))
                    return AvaloniaMatrix.M13;
                if (ReferenceEquals(property, "M21"))
                    return AvaloniaMatrix.M21;
                if (ReferenceEquals(property, "M22"))
                    return AvaloniaMatrix.M22;
                if (ReferenceEquals(property, "M23"))
                    return AvaloniaMatrix.M23;
                if (ReferenceEquals(property, "M31"))
                    return AvaloniaMatrix.M31;
                if (ReferenceEquals(property, "M32"))
                    return AvaloniaMatrix.M32;
                if (ReferenceEquals(property, "M33"))
                    return AvaloniaMatrix.M33;
                return default;
            }

            if (Type == VariantType.Matrix4x4)
            {
                if (ReferenceEquals(property, "M11"))
                    return Matrix4x4.M11;
                if (ReferenceEquals(property, "M12"))
                    return Matrix4x4.M12;
                if (ReferenceEquals(property, "M13"))
                    return Matrix4x4.M13;
                if (ReferenceEquals(property, "M14"))
                    return Matrix4x4.M14;
                if (ReferenceEquals(property, "M21"))
                    return Matrix4x4.M21;
                if (ReferenceEquals(property, "M22"))
                    return Matrix4x4.M22;
                if (ReferenceEquals(property, "M23"))
                    return Matrix4x4.M23;
                if (ReferenceEquals(property, "M24"))
                    return Matrix4x4.M24;
                if (ReferenceEquals(property, "M31"))
                    return Matrix4x4.M31;
                if (ReferenceEquals(property, "M32"))
                    return Matrix4x4.M32;
                if (ReferenceEquals(property, "M33"))
                    return Matrix4x4.M33;
                if (ReferenceEquals(property, "M34"))
                    return Matrix4x4.M34;
                if (ReferenceEquals(property, "M41"))
                    return Matrix4x4.M41;
                if (ReferenceEquals(property, "M42"))
                    return Matrix4x4.M42;
                if (ReferenceEquals(property, "M43"))
                    return Matrix4x4.M43;
                if (ReferenceEquals(property, "M44"))
                    return Matrix4x4.M44;
                return default;
            }

            if (Type == VariantType.Quaternion)
            {
                if (ReferenceEquals(property, "X"))
                    return Quaternion.X;
                if (ReferenceEquals(property, "Y"))
                    return Quaternion.Y;
                if (ReferenceEquals(property, "Z"))
                    return Quaternion.Z;
                if (ReferenceEquals(property, "W"))
                    return Quaternion.W;
                return default;
            }
            
            if (Type == VariantType.Color)
            {
                if (ReferenceEquals(property, "A"))
                    return Color.A;
                if (ReferenceEquals(property, "R"))
                    return Color.R;
                if (ReferenceEquals(property, "G"))
                    return Color.G;
                if (ReferenceEquals(property, "B"))
                    return Color.B;
                return default;
            }

            return default;
        }

        public static implicit operator ExpressionVariant(bool value) =>
            new ExpressionVariant
            {
                Type = VariantType.Boolean,
                Boolean = value
            };

        public static implicit operator ExpressionVariant(float scalar) =>
            new ExpressionVariant
            {
                Type = VariantType.Scalar,
                Scalar = scalar
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
                Type = VariantType.Matrix3x2,
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

        public static ExpressionVariant operator +(ExpressionVariant left, ExpressionVariant right)
        {
            if (left.Type != right.Type || left.Type == VariantType.Invalid)
                return default;

            if (left.Type == VariantType.Scalar)
                return left.Scalar + right.Scalar;
            
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
            
            return default;
        }

        public static ExpressionVariant operator -(ExpressionVariant left, ExpressionVariant right)
        {
            if (left.Type != right.Type || left.Type == VariantType.Invalid)
                return default;

            if (left.Type == VariantType.Scalar)
                return left.Scalar - right.Scalar;
            
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

            return default;
        }

        public static ExpressionVariant operator -(ExpressionVariant left)
        {

            if (left.Type == VariantType.Scalar)
                return -left.Scalar;
            
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

            return default;
        }

        public static ExpressionVariant operator *(ExpressionVariant left, ExpressionVariant right)
        {
            if (left.Type == VariantType.Invalid || right.Type == VariantType.Invalid)
                return default;

            if (left.Type == VariantType.Scalar && right.Type == VariantType.Scalar)
                return left.Scalar * right.Scalar;
            
            if (left.Type == VariantType.Double && right.Type == VariantType.Double)
                return left.Double * right.Double;

            if (left.Type == VariantType.Vector2 && right.Type == VariantType.Vector2)
                return left.Vector2 * right.Vector2;

            if (left.Type == VariantType.Vector && right.Type == VariantType.Vector)
                return Vector.Multiply(left.Vector, right.Vector);

            if (left.Type == VariantType.Vector2 && right.Type == VariantType.Scalar)
                return left.Vector2 * right.Scalar;
            
            if (left.Type == VariantType.Vector && right.Type == VariantType.Scalar)
                return left.Vector * right.Scalar;
            
            if (left.Type == VariantType.Vector && right.Type == VariantType.Double)
                return left.Vector * right.Double;

            if (left.Type == VariantType.Vector3 && right.Type == VariantType.Vector3)
                return left.Vector3 * right.Vector3;
            
            if (left.Type == VariantType.Vector3D && right.Type == VariantType.Vector3D)
                return Vector3D.Multiply(left.Vector3D, right.Vector3D);

            if (left.Type == VariantType.Vector3 && right.Type == VariantType.Scalar)
                return left.Vector3 * right.Scalar;
            
            if (left.Type == VariantType.Vector3D && right.Type == VariantType.Scalar)
                return Vector3D.Multiply(left.Vector3D, right.Scalar);

            if (left.Type == VariantType.Vector4 && right.Type == VariantType.Vector4)
                return left.Vector4 * right.Vector4;

            if (left.Type == VariantType.Vector4 && right.Type == VariantType.Scalar)
                return left.Vector4 * right.Scalar;
            
            if (left.Type == VariantType.Matrix3x2 && right.Type == VariantType.Matrix3x2)
                return left.Matrix3x2 * right.Matrix3x2;

            if (left.Type == VariantType.Matrix3x2 && right.Type == VariantType.Scalar)
                return left.Matrix3x2 * right.Scalar;
            
            if (left.Type == VariantType.AvaloniaMatrix && right.Type == VariantType.AvaloniaMatrix)
                return left.AvaloniaMatrix * right.AvaloniaMatrix;
            
            if (left.Type == VariantType.Matrix4x4 && right.Type == VariantType.Matrix4x4)
                return left.Matrix4x4 * right.Matrix4x4;

            if (left.Type == VariantType.Matrix4x4 && right.Type == VariantType.Scalar)
                return left.Matrix4x4 * right.Scalar;
            
            if (left.Type == VariantType.Quaternion && right.Type == VariantType.Quaternion)
                return left.Quaternion * right.Quaternion;

            if (left.Type == VariantType.Quaternion && right.Type == VariantType.Scalar)
                return left.Quaternion * right.Scalar;

            return default;
        }

        public static ExpressionVariant operator /(ExpressionVariant left, ExpressionVariant right)
        {
            if (left.Type == VariantType.Invalid || right.Type == VariantType.Invalid)
                return default;

            if (left.Type == VariantType.Scalar && right.Type == VariantType.Scalar)
                return left.Scalar / right.Scalar;
            
            if (left.Type == VariantType.Double && right.Type == VariantType.Double)
                return left.Double / right.Double;

            if (left.Type == VariantType.Vector2 && right.Type == VariantType.Vector2)
                return left.Vector2 / right.Vector2;

            if (left.Type == VariantType.Vector && right.Type == VariantType.Vector)
                return Vector.Divide(left.Vector, right.Vector);

            if (left.Type == VariantType.Vector2 && right.Type == VariantType.Scalar)
                return left.Vector2 / right.Scalar;
            
            if (left.Type == VariantType.Vector && right.Type == VariantType.Scalar)
                return left.Vector / right.Scalar;
            
            if (left.Type == VariantType.Vector && right.Type == VariantType.Double)
                return left.Vector / right.Scalar;

            if (left.Type == VariantType.Vector3 && right.Type == VariantType.Vector3)
                return left.Vector3 / right.Vector3;

            if (left.Type == VariantType.Vector3D && right.Type == VariantType.Vector3D)
                return Vector3D.Divide(left.Vector3D, right.Vector3D);

            if (left.Type == VariantType.Vector3 && right.Type == VariantType.Scalar)
                return left.Vector3 / right.Scalar;

            if (left.Type == VariantType.Vector3D && right.Type == VariantType.Scalar)
                return Avalonia.Vector3D.Divide(left.Vector3D, right.Scalar);
            
            if (left.Type == VariantType.Vector3D && right.Type == VariantType.Double)
                return Avalonia.Vector3D.Divide(left.Vector3D, right.Double);

            if (left.Type == VariantType.Vector4 && right.Type == VariantType.Vector4)
                return left.Vector4 / right.Vector4;

            if (left.Type == VariantType.Vector4 && right.Type == VariantType.Scalar)
                return left.Vector4 / right.Scalar;
            
            if (left.Type == VariantType.Quaternion && right.Type == VariantType.Quaternion)
                return left.Quaternion / right.Quaternion;

            return default;
        }

        public ExpressionVariant EqualsTo(ExpressionVariant right)
        {
            if (Type != right.Type || Type == VariantType.Invalid)
                return default;

            if (Type == VariantType.Scalar)
                return Scalar == right.Scalar;
            
            
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
            if (left.Type == VariantType.Scalar && right.Type == VariantType.Scalar)
                return left.Scalar % right.Scalar;
            if (left.Type == VariantType.Double && right.Type == VariantType.Double)
                return left.Double % right.Double;
            return default;
        }

        public static ExpressionVariant operator <(ExpressionVariant left, ExpressionVariant right)
        {
            if (left.Type == VariantType.Scalar && right.Type == VariantType.Scalar)
                return left.Scalar < right.Scalar;
            if (left.Type == VariantType.Double && right.Type == VariantType.Double)
                return left.Double < right.Double;
            return default;
        }

        public static ExpressionVariant operator >(ExpressionVariant left, ExpressionVariant right)
        {
            if (left.Type == VariantType.Scalar && right.Type == VariantType.Scalar)
                return left.Scalar > right.Scalar;
            
            if (left.Type == VariantType.Double && right.Type == VariantType.Double)
                return left.Double > right.Double;
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
                return Boolean && right.Boolean;
            return default;
        }

        public bool TryCast<T>(out T res) where T : struct
        {
            if (typeof(T) == typeof(bool))
            {
                if (Type == VariantType.Boolean)
                {
                    res = (T) (object) Boolean;
                    return true;
                }
            }

            if (typeof(T) == typeof(float))
            {
                if (Type == VariantType.Scalar)
                {
                    res = (T) (object) Scalar;
                    return true;
                }
                if (Type == VariantType.Double)
                {
                    res = (T)(object)Scalar;
                    return true;
                }
            }
            
            if (typeof(T) == typeof(double))
            {
                if (Type == VariantType.Double)
                {
                    res = (T) (object) Double;
                    return true;
                }

                if (Type == VariantType.Scalar)
                {
                    res = (T)(object)(float)Double;
                    return true;
                }
            }

            if (typeof(T) == typeof(Vector2))
            {
                if (Type == VariantType.Vector2)
                {
                    res = (T) (object) Vector2;
                    return true;
                }

                if (Type == VariantType.Vector)
                {
                    res = (T) (object) Vector.ToVector2();
                    return true;
                }
            }
            
            if (typeof(T) == typeof(Vector))
            {
                if (Type == VariantType.Vector)
                {
                    res = (T) (object) Vector;
                    return true;
                }

                if (Type == VariantType.Vector2)
                {
                    res = (T)(object)new Vector(Vector2);
                    return true;
                }
            }

            if (typeof(T) == typeof(Vector3))
            {
                if (Type == VariantType.Vector3)
                {
                    res = (T) (object) Vector3;
                    return true;
                }
                if (Type == VariantType.Vector3D)
                {
                    res = (T) (object) Vector3D.ToVector3();
                    return true;
                }
            }
            
            if (typeof(T) == typeof(Vector3D))
            {
                if (Type == VariantType.Vector3D)
                {
                    res = (T) (object) Vector3D;
                    return true;
                }
                
                if (Type == VariantType.Vector3)
                {
                    res = (T)(object)new Vector3D(Vector3);
                    return true;
                }
            }

            if (typeof(T) == typeof(Vector4))
            {
                if (Type == VariantType.Vector4)
                {
                    res = (T) (object) Vector4;
                    return true;
                }
            }

            if (typeof(T) == typeof(Matrix3x2))
            {
                if (Type == VariantType.Matrix3x2)
                {
                    res = (T) (object) Matrix3x2;
                    return true;
                }
            }
            
            if (typeof(T) == typeof(Matrix))
            {
                if (Type == VariantType.AvaloniaMatrix)
                {
                    res = (T) (object) Matrix3x2;
                    return true;
                }
            }

            if (typeof(T) == typeof(Matrix4x4))
            {
                if (Type == VariantType.Matrix4x4)
                {
                    res = (T) (object) Matrix4x4;
                    return true;
                }
            }

            if (typeof(T) == typeof(Quaternion))
            {
                if (Type == VariantType.Quaternion)
                {
                    res = (T) (object) Quaternion;
                    return true;
                }
            }
            
            if (typeof(T) == typeof(Avalonia.Media.Color))
            {
                if (Type == VariantType.Color)
                {
                    res = (T) (object) Color;
                    return true;
                }
            }

            res = default;
            return false;
        }

        public static ExpressionVariant Create<T>(T v) where T : struct
        {
            if (typeof(T) == typeof(bool))
                return (bool) (object) v;

            if (typeof(T) == typeof(float))
                return (float) (object) v;

            if (typeof(T) == typeof(Vector2))
                return (Vector2) (object) v;
            
            if (typeof(T) == typeof(Vector))
                return (Vector) (object) v;

            if (typeof(T) == typeof(Vector3))
                return (Vector3) (object) v;
            
            if (typeof(T) == typeof(Vector3D))
                return (Vector3D) (object) v;

            if (typeof(T) == typeof(Vector4))
                return (Vector4) (object) v;

            if (typeof(T) == typeof(Matrix3x2))
                return (Matrix3x2) (object) v;
            
            if (typeof(T) == typeof(Matrix))
                return (Matrix) (object) v;

            if (typeof(T) == typeof(Matrix4x4))
                return (Matrix4x4) (object) v;

            if (typeof(T) == typeof(Quaternion))
                return (Quaternion) (object) v;
            
            if (typeof(T) == typeof(Avalonia.Media.Color))
                return (Avalonia.Media.Color) (object) v;

            throw new ArgumentException("Invalid variant type: " + typeof(T));
        }

        public T CastOrDefault<T>() where T : struct
        {
            TryCast<T>(out var r);
            return r;
        }

        public override string ToString()
        {
            if (Type == VariantType.Boolean)
                return Boolean.ToString();
            if (Type == VariantType.Scalar)
                return Scalar.ToString(CultureInfo.InvariantCulture);
            if (Type == VariantType.Double)
                return Double.ToString(CultureInfo.InvariantCulture);
            if (Type == VariantType.Vector2)
                return Vector2.ToString();
            if (Type == VariantType.Vector)
                return Vector.ToString();
            if (Type == VariantType.Vector3)
                return Vector3.ToString();
            if (Type == VariantType.Vector3D)
                return Vector3D.ToString();
            if (Type == VariantType.Vector4)
                return Vector4.ToString();
            if (Type == VariantType.Quaternion)
                return Quaternion.ToString();
            if (Type == VariantType.Matrix3x2)
                return Matrix3x2.ToString();
            if (Type == VariantType.AvaloniaMatrix)
                return AvaloniaMatrix.ToString();
            if (Type == VariantType.Matrix4x4)
                return Matrix4x4.ToString();
            if (Type == VariantType.Color)
                return Color.ToString();
            if (Type == VariantType.Invalid)
                return "Invalid";
            return "Unknown";
        }
    }

}
