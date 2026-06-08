using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Fonts.Tables.Cff;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables.Cff
{
    /// <summary>
    /// Exercises the Type 2 charstring interpreter on hand-built charstrings via a recording geometry
    /// context, asserting the exact emitted path. No font or platform dependency — each test isolates
    /// one operator's math (the curve families and width / hint handling are the usual correctness traps).
    /// </summary>
    public class Type2CharStringInterpreterTests
    {
        [Fact]
        public void RMoveTo_RLineTo_Builds_Closed_Square()
        {
            // 50 50 rmoveto  100 0 rlineto  0 100 rlineto  -100 0 rlineto  endchar
            var cs = new CharStringBuilder()
                .Int(50).Int(50).Op(21)            // rmoveto
                .Int(100).Int(0).Op(5)             // rlineto
                .Int(0).Int(100).Op(5)             // rlineto
                .Int(-100).Int(0).Op(5)            // rlineto
                .Op(14)                            // endchar
                .ToArray();

            var ctx = Run(cs);

            var figure = Assert.Single(ctx.Figures);
            Assert.True(ctx.AllClosed);
            AssertPoints(figure, (50, 50), (150, 50), (150, 150), (50, 150));
        }

        [Fact]
        public void RrCurveTo_Emits_Cubic_With_Cumulative_Control_Points()
        {
            // 0 0 rmoveto  10 20 30 40 50 60 rrcurveto  endchar
            var cs = new CharStringBuilder()
                .Int(0).Int(0).Op(21)
                .Int(10).Int(20).Int(30).Int(40).Int(50).Int(60).Op(8)  // rrcurveto
                .Op(14)
                .ToArray();

            var ctx = Run(cs);

            // c1 = (10,20); c2 = c1+(30,40) = (40,60); end = c2+(50,60) = (90,120).
            AssertPoints(Assert.Single(ctx.Figures), (0, 0), (10, 20), (40, 60), (90, 120));
        }

        [Fact]
        public void HLineTo_Alternates_Horizontal_Then_Vertical()
        {
            // 0 0 rmoveto  10 20 30 hlineto  endchar
            var cs = new CharStringBuilder()
                .Int(0).Int(0).Op(21)
                .Int(10).Int(20).Int(30).Op(6)     // hlineto
                .Op(14)
                .ToArray();

            var ctx = Run(cs);

            // h:10 -> (10,0); v:20 -> (10,20); h:30 -> (40,20).
            AssertPoints(Assert.Single(ctx.Figures), (0, 0), (10, 0), (10, 20), (40, 20));
        }

        [Fact]
        public void VvCurveTo_Honours_Leading_Dx()
        {
            // 0 0 rmoveto  5 10 20 30 40 vvcurveto  endchar  (5 args => leading dx1 = 5)
            var cs = new CharStringBuilder()
                .Int(0).Int(0).Op(21)
                .Int(5).Int(10).Int(20).Int(30).Int(40).Op(26)  // vvcurveto
                .Op(14)
                .ToArray();

            var ctx = Run(cs);

            // c1 = (0+5, 0+10) = (5,10); c2 = (5+20, 10+30) = (25,40); end = (25, 40+40) = (25,80).
            AssertPoints(Assert.Single(ctx.Figures), (0, 0), (5, 10), (25, 40), (25, 80));
        }

        [Fact]
        public void HvCurveTo_Four_Args_Starts_Horizontal_Ends_Vertical()
        {
            // 0 0 rmoveto  10 20 30 40 hvcurveto  endchar
            var cs = new CharStringBuilder()
                .Int(0).Int(0).Op(21)
                .Int(10).Int(20).Int(30).Int(40).Op(31)   // hvcurveto
                .Op(14)
                .ToArray();

            var ctx = Run(cs);

            // c1 = (10,0); c2 = (10+20, 0+30) = (30,30); end = (30, 30+40) = (30,70).
            AssertPoints(Assert.Single(ctx.Figures), (0, 0), (10, 0), (30, 30), (30, 70));
        }

        [Fact]
        public void HvCurveTo_Five_Args_Applies_Final_Delta_To_End_X()
        {
            // 0 0 rmoveto  10 20 30 40 5 hvcurveto  endchar
            var cs = new CharStringBuilder()
                .Int(0).Int(0).Op(21)
                .Int(10).Int(20).Int(30).Int(40).Int(5).Op(31)
                .Op(14)
                .ToArray();

            var ctx = Run(cs);

            // Final df=5 lands on the otherwise-zero end-x: end = (30+5, 70) = (35,70).
            AssertPoints(Assert.Single(ctx.Figures), (0, 0), (10, 0), (30, 30), (35, 70));
        }

        [Fact]
        public void Leading_Width_Before_RMoveTo_Is_Dropped()
        {
            // 100 50 50 rmoveto  10 0 rlineto  endchar  (3 args to rmoveto => 100 is the width)
            var cs = new CharStringBuilder()
                .Int(100).Int(50).Int(50).Op(21)
                .Int(10).Int(0).Op(5)
                .Op(14)
                .ToArray();

            var ctx = Run(cs);

            // Width 100 dropped: move to (50,50), line to (60,50).
            AssertPoints(Assert.Single(ctx.Figures), (50, 50), (60, 50));
        }

        [Fact]
        public void HintMask_Skips_Mask_Bytes_Without_Corrupting_Path()
        {
            // 10 20 30 40 hstemhm  hintmask <1 mask byte>  50 50 rmoveto  100 0 rlineto  endchar
            // Two stems => one mask byte. If the mask byte were misread as an operand the path breaks.
            var cs = new CharStringBuilder()
                .Int(10).Int(20).Int(30).Int(40).Op(18)   // hstemhm (2 stems)
                .Op(19).Raw(0xFF)                          // hintmask + 1 mask byte
                .Int(50).Int(50).Op(21)                    // rmoveto
                .Int(100).Int(0).Op(5)                     // rlineto
                .Op(14)
                .ToArray();

            var ctx = Run(cs);

            AssertPoints(Assert.Single(ctx.Figures), (50, 50), (150, 50));
        }

        [Fact]
        public void CallSubr_Resolves_Index_With_Bias()
        {
            // Local subr 0: 100 0 rlineto return
            var subr0 = new CharStringBuilder().Int(100).Int(0).Op(5).Op(11).ToArray();
            var localSubrs = CffIndex.Read(BuildIndex(subr0), 0);

            // With one subr the bias is 107, so subr 0 is addressed by index (-107).
            // 0 0 rmoveto  -107 callsubr  endchar
            var cs = new CharStringBuilder()
                .Int(0).Int(0).Op(21)
                .Int(-107).Op(10)   // callsubr
                .Op(14)
                .ToArray();

            var ctx = Run(cs, localSubrs: localSubrs);

            AssertPoints(Assert.Single(ctx.Figures), (0, 0), (100, 0));
        }

        // --- helpers ---------------------------------------------------------------------------

        private static FigureRecordingContext Run(byte[] charString, CffIndex globalSubrs = default, CffIndex localSubrs = default)
        {
            var ctx = new FigureRecordingContext();
            Span<double> stack = stackalloc double[48];
            var interpreter = new Type2CharStringInterpreter(ctx, Matrix.Identity, globalSubrs, localSubrs, stack);
            interpreter.Run(charString);
            return ctx;
        }

        private static void AssertPoints(List<Point> figure, params (double X, double Y)[] expected)
        {
            Assert.Equal(expected.Length, figure.Count);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.True(
                    Math.Abs(figure[i].X - expected[i].X) < 0.001 && Math.Abs(figure[i].Y - expected[i].Y) < 0.001,
                    $"Point {i}: expected ({expected[i].X},{expected[i].Y}), got ({figure[i].X},{figure[i].Y})");
            }
        }

        /// <summary>Builds an INDEX blob (offSize 1) for the subr tests.</summary>
        private static ReadOnlyMemory<byte> BuildIndex(params byte[][] entries)
        {
            var blob = new List<byte> { (byte)(entries.Length >> 8), (byte)entries.Length };

            if (entries.Length == 0)
            {
                return blob.ToArray();
            }

            blob.Add(1); // offSize
            int offset = 1;
            blob.Add((byte)offset);
            foreach (var entry in entries)
            {
                offset += entry.Length;
                blob.Add((byte)offset);
            }

            foreach (var entry in entries)
            {
                blob.AddRange(entry);
            }

            return blob.ToArray();
        }

        private sealed class CharStringBuilder
        {
            private readonly List<byte> _bytes = new();

            public CharStringBuilder Int(int value)
            {
                switch (value)
                {
                    case >= -107 and <= 107:
                        _bytes.Add((byte)(value + 139));
                        break;
                    case >= 108 and <= 1131:
                        _bytes.Add((byte)(((value - 108) >> 8) + 247));
                        _bytes.Add((byte)((value - 108) & 0xFF));
                        break;
                    case >= -1131 and <= -108:
                        _bytes.Add((byte)(((-value - 108) >> 8) + 251));
                        _bytes.Add((byte)((-value - 108) & 0xFF));
                        break;
                    default:
                        _bytes.Add(28);
                        _bytes.Add((byte)(value >> 8));
                        _bytes.Add((byte)(value & 0xFF));
                        break;
                }

                return this;
            }

            public CharStringBuilder Op(byte op)
            {
                _bytes.Add(op);
                return this;
            }

            public CharStringBuilder Raw(byte value)
            {
                _bytes.Add(value);
                return this;
            }

            public byte[] ToArray() => _bytes.ToArray();
        }

        private sealed class FigureRecordingContext : IGeometryContext
        {
            private List<Point>? _current;

            public List<List<Point>> Figures { get; } = new();

            public bool AllClosed { get; private set; } = true;

            public void BeginFigure(Point startPoint, bool isFilled = true)
            {
                _current = new List<Point> { startPoint };
                Figures.Add(_current);
            }

            public void LineTo(Point point, bool isStroked = true) => _current?.Add(point);

            public void QuadraticBezierTo(Point controlPoint, Point endPoint, bool isStroked = true)
            {
                _current?.Add(controlPoint);
                _current?.Add(endPoint);
            }

            public void CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint, bool isStroked = true)
            {
                _current?.Add(controlPoint1);
                _current?.Add(controlPoint2);
                _current?.Add(endPoint);
            }

            public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc,
                SweepDirection sweepDirection, bool isStroked = true) => _current?.Add(point);

            public void EndFigure(bool isClosed)
            {
                if (!isClosed)
                {
                    AllClosed = false;
                }

                _current = null;
            }

            public void SetFillRule(FillRule fillRule) { }

            public void Dispose() { }
        }
    }
}
