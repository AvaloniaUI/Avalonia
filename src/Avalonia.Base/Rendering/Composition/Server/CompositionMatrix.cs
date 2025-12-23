//#define DEBUG_COMPOSITION_MATRIX
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Metadata;
// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Avalonia.Rendering.Composition.Server;
[PrivateApi]
public struct CompositionMatrix : IEquatable<CompositionMatrix>
{
    enum Type
    {
        Identity = 0,
        TranslateAndScale = 1,
        Full = 2
    }

    private double _scaleX_11;
    private double _skewY_12;
    private double _perspX_13;
    private double _skewX_21;
    private double _scaleY_22;
    private double _perspY_23;
    private double _offsetX_31;
    private double _offsetY_32;
    private double _perspZ_33;
    private Type _type;
    
    public bool GuaranteedIdentity => _type == Type.Identity;
    public bool GuaranteedTranslateAndScaleOnly => _type == Type.TranslateAndScale;
    public bool GuaranteedTranslateAndScaleOnlyOrIdentity => _type is Type.TranslateAndScale or Type.Identity;

    public double ScaleX => _type != Type.Identity ? _scaleX_11 : 1;
    public double SkewY => _skewY_12;
    public double PerspX => _perspX_13;

    public double SkewX => _skewX_21;
    public double ScaleY => _type != Type.Identity ? _scaleY_22 : 1;
    public double PerspY => _perspY_23;
    
    public double OffsetX => _offsetX_31;
    public double OffsetY => _offsetY_32;
    public double PerspZ => _type == Type.Full ? _perspZ_33 : 1;

    public static CompositionMatrix Identity { get; } = default;

    public static CompositionMatrix CreateTranslation(Vector offset) => CreateTranslation(offset.X, offset.Y);
    
    public static CompositionMatrix CreateTranslation(double offsetX, double offsetY)
    {
        if(offsetX == 0 && offsetY == 0)
            return default;
        
        return new CompositionMatrix
        {
            _type = Type.TranslateAndScale,
            _offsetX_31 = offsetX,
            _offsetY_32 = offsetY,
            _scaleX_11 = 1,
            _scaleY_22 = 1,
            
        };
    }
    
    public static CompositionMatrix CreateScale(in Vector3D scales, in Vector3D centerPoint)
    {
        var cp = centerPoint * (Vector3D.One - scales);
        return new CompositionMatrix()
        {
            _type = Type.TranslateAndScale,
            _scaleX_11 = scales.X,
            _scaleY_22 = scales.Y,
            _offsetX_31 = cp.X,
            _offsetY_32 = cp.Y,
            _perspZ_33 = 1
        };
    }

    public static CompositionMatrix CreateScale(Vector scale) => CreateScale(scale.X, scale.Y);
    
    public static CompositionMatrix CreateScale(double scaleX, double scaleY)
    {
        if (scaleX == 1 && scaleY == 1)
            return default;
        return new CompositionMatrix()
        {
            _type = Type.TranslateAndScale,
            _scaleX_11 = scaleX,
            _scaleY_22 = scaleY,
            _perspZ_33 = 1
        };
    }
    
    
    [SkipLocalsInit]
    public static unsafe CompositionMatrix FromMatrix(in Matrix m)
    {
        CompositionMatrix rv;
        if(m.M13 != 0 || m.M23 != 0 || m.M33 != 1 || m.M12 != 0 || m.M21 != 0)
            rv._type = Type.Full;
        else if (m.M11 != 1 || m.M22 != 1 || m.M31 != 0 || m.M32 != 0 )
            rv._type = Type.TranslateAndScale;
        else
            return default;
        *(Matrix*)&rv = m;
#if DEBUG_COMPOSITION_MATRIX
        var back = rv.ToMatrix();
        if(back != m)
        {
            throw new InvalidOperationException("BUG");
        }
#endif
        return rv;
    }
    
    [SkipLocalsInit]
    public static unsafe CompositionMatrix FromMatrix(double m11, double m12, double m13,
        double m21, double m22, double m23,
        double m31, double m32, double m33)
    {

        CompositionMatrix rv;
        if(m13 != 0 || m23 != 0 || m33 != 1 || m12 != 0 || m21 != 0)
            rv._type = Type.Full;
        else if (m11 != 1 || m22 != 1 || m31 != 0 || m32 != 0 )
            rv._type = Type.TranslateAndScale;
        else
            return default;
        // TODO: opt
        *(Matrix*)&rv = new Matrix(m11, m12, m13, m21, m22, m23, m31, m32, m33);
        return rv;
    }
    
    
    public CompositionMatrix(double m11, double m12, double m13,
        double m21, double m22, double m23,
        double m31, double m32, double m33)
    {
        // TODO: opt
        this = FromMatrix(m11, m12, m13, m21, m22, m23, m31, m32, m33);
    }

    
    public Matrix ToMatrix()
    {
        if (_type == Type.Identity)
            return Matrix.Identity;

        if (_type == Type.TranslateAndScale)
            return new Matrix(_scaleX_11, 0, 0, _scaleY_22, _offsetX_31, _offsetY_32);
        
        return new Matrix(_scaleX_11, _skewY_12, _perspX_13, _skewX_21, _scaleY_22, _perspY_23, _offsetX_31, _offsetY_32, _perspZ_33);
    }
    
    //public static implicit operator Matrix(CompositionMatrix m) => m.ToMatrix();
    //public static implicit operator CompositionMatrix(Matrix m) => FromMatrix(m);

    public bool Equals(CompositionMatrix m)
    {
        if(m._type == Type.Identity && _type == Type.Identity)
            return true;
        if(m._type == Type.TranslateAndScale && _type == Type.TranslateAndScale)
        {
            return m._scaleX_11 == _scaleX_11 &&
                   m._scaleY_22 == _scaleY_22 &&
                   m._offsetX_31 == _offsetX_31 &&
                   m._offsetY_32 == _offsetY_32;
        }
        
        return m.ScaleX == ScaleX &&
               m._skewY_12 == _skewY_12 &&
               m._perspX_13 == _perspX_13 &&
               m._skewX_21 == _skewX_21 &&
               m.ScaleY == ScaleY &&
               m._perspY_23 == _perspY_23 &&
               m._offsetX_31 == _offsetX_31 &&
               m._offsetY_32 == _offsetY_32 &&
               m.PerspZ == PerspZ;
    }
    
    public static bool operator ==(CompositionMatrix left, CompositionMatrix right) => left.Equals(right);
    public static bool operator !=(CompositionMatrix left, CompositionMatrix right) => !left.Equals(right);


    [SkipLocalsInit]
    private static CompositionMatrix Concat(ref CompositionMatrix left, ref CompositionMatrix right)
#if DEBUG_COMPOSITION_MATRIX
    {
        var res = ConcatCore(ref left, ref right);
        var expected = left.ToMatrix() * right.ToMatrix();
        var actual = res.ToMatrix();
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException("BUG");
        }

        return res;
    }
    
    static CompositionMatrix ConcatCore(ref CompositionMatrix left, ref CompositionMatrix right)
#endif
    {
        if (left._type == Type.Identity)
            return right;
        if (right._type == Type.Identity)
            return left;
        
        if(left._type == Type.TranslateAndScale && right._type == Type.TranslateAndScale)
        {
            return new CompositionMatrix
            {
                _type = Type.TranslateAndScale,
                _scaleX_11 = left._scaleX_11 * right._scaleX_11,
                _scaleY_22 = left._scaleY_22 * right._scaleY_22,
                _offsetX_31 = (left._offsetX_31 * right._scaleX_11) + right._offsetX_31,
                _offsetY_32 = (left._offsetY_32 * right._scaleY_22) + right._offsetY_32,
                _perspZ_33 = 1
            };
        }

        return new CompositionMatrix
        {
            _type = Type.Full,
            _scaleX_11 =
                (left.ScaleX * right.ScaleX) + (left._skewY_12 * right._skewX_21) +
                (left._perspX_13 * right._offsetX_31),
            _skewY_12 =
                (left.ScaleX * right._skewY_12) + (left._skewY_12 * right.ScaleY) +
                (left._perspX_13 * right._offsetY_32),
            _perspX_13 =
                (left.ScaleX * right._perspX_13) + (left._skewY_12 * right._perspY_23) +
                (left._perspX_13 * right.PerspZ),
            _skewX_21 =
                (left._skewX_21 * right.ScaleX) + (left.ScaleY * right._skewX_21) +
                (left._perspY_23 * right._offsetX_31),
            _scaleY_22 =
                (left._skewX_21 * right._skewY_12) + (left.ScaleY * right.ScaleY) +
                (left._perspY_23 * right._offsetY_32),
            _perspY_23 =
                (left._skewX_21 * right._perspX_13) + (left.ScaleY * right._perspY_23) +
                (left._perspY_23 * right.PerspZ),
            _offsetX_31 =
                (left._offsetX_31 * right.ScaleX) + (left._offsetY_32 * right._skewX_21) +
                (left.PerspZ * right._offsetX_31),
            _offsetY_32 =
                (left._offsetX_31 * right._skewY_12) + (left._offsetY_32 * right.ScaleY) +
                (left.PerspZ * right._offsetY_32),
            _perspZ_33 =
                (left._offsetX_31 * right._perspX_13) + (left._offsetY_32 * right._perspY_23) +
                (left.PerspZ * right.PerspZ)
        };
    }
    
    public static CompositionMatrix operator*(CompositionMatrix left, CompositionMatrix right)
    {
        return Concat(ref left, ref right);
    }

    public Point Transform(Point p)
#if DEBUG_COMPOSITION_MATRIX
    {
        var res = TransformCore(p);
        var expected = p.Transform(ToMatrix());

        static void Compare(double res, double expected)
        {
            var diff = Math.Abs(res - expected);
            if (diff > 0.00000153)
                throw new InvalidProgramException("BUG");
        }
        Compare(res.X, res.X);
        Compare(res.Y, res.Y);

        return res;
    }
    
    public Point TransformCore(Point p)
#endif
    {
        if (_type == Type.Identity)
            return p;
        if (_type == Type.TranslateAndScale)
        {
            return new Point(
                (p.X * _scaleX_11) + _offsetX_31,
                (p.Y * _scaleY_22) + _offsetY_32);
        }

        var m44 = new Matrix4x4(
            (float)_scaleX_11, (float)_skewY_12, (float)_perspX_13, 0,
            (float)_skewX_21, (float)_scaleY_22, (float)_perspY_23, 0,
            (float)_offsetX_31, (float)_offsetY_32, (float)_perspZ_33, 0,
            0, 0, 0, 1
        );

        var vector = new Vector3((float)p.X, (float)p.Y, 1);
        var transformedVector = Vector3.Transform(vector, m44);
        var z = 1 / transformedVector.Z;

        return new Point(transformedVector.X * z, transformedVector.Y * z);
    }

    public override string ToString()
    {
        if (_type == Type.Identity)
            return "Identity";
        var m = ToMatrix();
        if(m.IsIdentity)
            return "Identity";
        return m.ToString();
    }

    
    // TODO: Optimize for simple translate+scale
    public bool HasInverse => ToMatrix().HasInverse;
    
    // TODO: Optimize for simple translate+scale
    public CompositionMatrix Invert() => FromMatrix(ToMatrix().Invert());
    
    // TODO: Optimize for simple translate+scale
    public bool TryInvert(out CompositionMatrix inverted)
    {
        var m = ToMatrix();
        if (m.TryInvert(out var inv))
        {
            inverted = FromMatrix(inv);
            return true;
        }

        inverted = default;
        return false;
    }
}