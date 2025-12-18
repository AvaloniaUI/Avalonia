using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Visuals
{
    /// <summary>
    /// Benchmarks for Thickness, Size, CornerRadius, and PixelRect/PixelPoint operations.
    /// These structs are heavily used in layout calculations, margins, padding, and borders.
    /// </summary>
    [MemoryDiagnoser]
    public class ThicknessSizeBenchmarks
    {
        private Thickness _thickness1;
        private Thickness _thickness2;
        private Size _size1;
        private Size _size2;
        private Size _constraintSize;
        private CornerRadius _cornerRadius1;
        private CornerRadius _cornerRadius2;
        private PixelRect _pixelRect1;
        private PixelRect _pixelRect2;
        private PixelPoint _pixelPoint;
        private PixelSize _pixelSize;
        private Vector _scaleVector;

        [GlobalSetup]
        public void Setup()
        {
            _thickness1 = new Thickness(5, 10, 5, 10);
            _thickness2 = new Thickness(2, 4, 2, 4);
            _size1 = new Size(800, 600);
            _size2 = new Size(400, 300);
            _constraintSize = new Size(1920, 1080);
            _cornerRadius1 = new CornerRadius(8, 8, 4, 4);
            _cornerRadius2 = new CornerRadius(4);
            _pixelRect1 = new PixelRect(0, 0, 1920, 1080);
            _pixelRect2 = new PixelRect(100, 100, 800, 600);
            _pixelPoint = new PixelPoint(500, 400);
            _pixelSize = new PixelSize(1920, 1080);
            _scaleVector = new Vector(2.0, 2.0);
        }

        #region Thickness Operations

        [Benchmark]
        public Thickness Thickness_Add()
        {
            return _thickness1 + _thickness2;
        }

        [Benchmark]
        public Thickness Thickness_Subtract()
        {
            return _thickness1 - _thickness2;
        }

        [Benchmark]
        public Thickness Thickness_Multiply()
        {
            return _thickness1 * 2.0;
        }

        [Benchmark]
        public bool Thickness_Equals()
        {
            return _thickness1 == _thickness2;
        }

        [Benchmark]
        public bool Thickness_IsUniform()
        {
            return _thickness1.IsUniform;
        }

        [Benchmark]
        public Thickness Thickness_Parse_Uniform()
        {
            return Thickness.Parse("10");
        }

        [Benchmark]
        public Thickness Thickness_Parse_TwoValues()
        {
            return Thickness.Parse("5, 10");
        }

        [Benchmark]
        public Thickness Thickness_Parse_FourValues()
        {
            return Thickness.Parse("5, 10, 15, 20");
        }

        #endregion

        #region Size Operations

        [Benchmark]
        public Size Size_Add()
        {
            return _size1 + _size2;
        }

        [Benchmark]
        public Size Size_Subtract()
        {
            return _size1 - _size2;
        }

        [Benchmark]
        public Size Size_MultiplyScalar()
        {
            return _size1 * 2.0;
        }

        [Benchmark]
        public Size Size_MultiplyVector()
        {
            return _size1 * _scaleVector;
        }

        [Benchmark]
        public Size Size_DivideScalar()
        {
            return _size1 / 2.0;
        }

        [Benchmark]
        public Vector Size_DivideSize()
        {
            return _size1 / _size2;
        }

        [Benchmark]
        public Size Size_Constrain()
        {
            return _size1.Constrain(_constraintSize);
        }

        [Benchmark]
        public Size Size_Deflate()
        {
            return _size1.Deflate(_thickness1);
        }

        [Benchmark]
        public Size Size_Inflate()
        {
            return _size1.Inflate(_thickness1);
        }

        [Benchmark]
        public Size Size_WithWidth()
        {
            return _size1.WithWidth(1000);
        }

        [Benchmark]
        public Size Size_WithHeight()
        {
            return _size1.WithHeight(800);
        }

        [Benchmark]
        public bool Size_Equals()
        {
            return _size1 == _size2;
        }

        [Benchmark]
        public double Size_AspectRatio()
        {
            return _size1.AspectRatio;
        }

        [Benchmark]
        public Size Size_Parse()
        {
            return Size.Parse("800, 600");
        }

        [Benchmark]
        public Size Size_AddThickness()
        {
            return _size1 + _thickness1;
        }

        [Benchmark]
        public Size Size_SubtractThickness()
        {
            return _size1 - _thickness1;
        }

        #endregion

        #region CornerRadius Operations

        [Benchmark]
        public bool CornerRadius_Equals()
        {
            return _cornerRadius1 == _cornerRadius2;
        }

        [Benchmark]
        public bool CornerRadius_IsUniform()
        {
            return _cornerRadius1.IsUniform;
        }

        [Benchmark]
        public CornerRadius CornerRadius_Parse_Uniform()
        {
            return CornerRadius.Parse("8");
        }

        [Benchmark]
        public CornerRadius CornerRadius_Parse_TwoValues()
        {
            return CornerRadius.Parse("8, 4");
        }

        [Benchmark]
        public CornerRadius CornerRadius_Parse_FourValues()
        {
            return CornerRadius.Parse("8, 8, 4, 4");
        }

        [Benchmark]
        public int CornerRadius_GetHashCode()
        {
            return _cornerRadius1.GetHashCode();
        }

        #endregion

        #region PixelRect Operations

        [Benchmark]
        public bool PixelRect_Contains_Point()
        {
            return _pixelRect1.Contains(_pixelPoint);
        }

        [Benchmark]
        public bool PixelRect_ContainsExclusive_Point()
        {
            return _pixelRect1.ContainsExclusive(_pixelPoint);
        }

        [Benchmark]
        public bool PixelRect_Contains_Rect()
        {
            return _pixelRect1.Contains(_pixelRect2);
        }

        [Benchmark]
        public bool PixelRect_Intersects()
        {
            return _pixelRect1.Intersects(_pixelRect2);
        }

        [Benchmark]
        public PixelRect PixelRect_Intersect()
        {
            return _pixelRect1.Intersect(_pixelRect2);
        }

        [Benchmark]
        public PixelRect PixelRect_Union()
        {
            return _pixelRect1.Union(_pixelRect2);
        }

        [Benchmark]
        public PixelRect PixelRect_CenterRect()
        {
            return _pixelRect1.CenterRect(_pixelRect2);
        }

        [Benchmark]
        public PixelRect PixelRect_Translate()
        {
            return _pixelRect1.Translate(_pixelPoint);
        }

        [Benchmark]
        public bool PixelRect_Equals()
        {
            return _pixelRect1 == _pixelRect2;
        }

        [Benchmark]
        public PixelRect PixelRect_WithX()
        {
            return _pixelRect1.WithX(50);
        }

        [Benchmark]
        public PixelRect PixelRect_WithY()
        {
            return _pixelRect1.WithY(50);
        }

        [Benchmark]
        public PixelRect PixelRect_WithWidth()
        {
            return _pixelRect1.WithWidth(800);
        }

        [Benchmark]
        public PixelRect PixelRect_WithHeight()
        {
            return _pixelRect1.WithHeight(600);
        }

        [Benchmark]
        public PixelRect PixelRect_FromSize()
        {
            return new PixelRect(_pixelSize);
        }

        [Benchmark]
        public PixelRect PixelRect_FromPointAndSize()
        {
            return new PixelRect(_pixelPoint, _pixelSize);
        }

        [Benchmark]
        public Rect PixelRect_ToRect()
        {
            return _pixelRect1.ToRect(1.0);
        }

        [Benchmark]
        public Rect PixelRect_ToRectWithDpi()
        {
            return _pixelRect1.ToRect(1.5);
        }

        [Benchmark]
        public PixelRect PixelRect_Parse()
        {
            return PixelRect.Parse("0, 0, 1920, 1080");
        }

        #endregion

        #region PixelPoint Operations

        [Benchmark]
        public PixelPoint PixelPoint_Add()
        {
            return _pixelPoint + new PixelPoint(100, 100);
        }

        [Benchmark]
        public PixelPoint PixelPoint_Subtract()
        {
            return _pixelPoint - new PixelPoint(50, 50);
        }

        [Benchmark]
        public bool PixelPoint_Equals()
        {
            return _pixelPoint == new PixelPoint(500, 400);
        }

        [Benchmark]
        public PixelPoint PixelPoint_Parse()
        {
            return PixelPoint.Parse("100, 200");
        }

        [Benchmark]
        public Point PixelPoint_ToPoint()
        {
            return _pixelPoint.ToPoint(1.0);
        }

        [Benchmark]
        public Point PixelPoint_ToPointWithDpi()
        {
            return _pixelPoint.ToPoint(1.5);
        }

        #endregion

        #region PixelSize Operations

        [Benchmark]
        public bool PixelSize_Equals()
        {
            return _pixelSize == new PixelSize(1920, 1080);
        }

        [Benchmark]
        public PixelSize PixelSize_Parse()
        {
            return PixelSize.Parse("1920, 1080");
        }

        [Benchmark]
        public Size PixelSize_ToSize()
        {
            return _pixelSize.ToSize(1.0);
        }

        [Benchmark]
        public Size PixelSize_ToSizeWithDpi()
        {
            return _pixelSize.ToSizeWithDpi(1.5);
        }

        [Benchmark]
        public double PixelSize_AspectRatio()
        {
            return _pixelSize.AspectRatio;
        }

        #endregion
    }
}
