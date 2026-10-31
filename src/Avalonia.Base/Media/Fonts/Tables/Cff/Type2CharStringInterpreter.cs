using System;
using Avalonia.Media.Fonts.Tables.Variation;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts.Tables.Cff
{
    /// <summary>
    /// Interprets a Type 2 charstring (CFF / CFF2 glyph program) on an operand stack, emitting the
    /// resulting path into an <see cref="IGeometryContext"/>. Hints are parsed only far enough to
    /// skip them (we render unhinted). The same walker backs CFF and CFF2; CFF2 enables the
    /// <c>blend</c> / <c>vsindex</c> operators and omits <c>endchar</c> / width / <c>seac</c>.
    /// </summary>
    internal ref struct Type2CharStringInterpreter
    {
        private const int MaxDepth = 10;

        // Type 2 implementation limit for the transient array that put / get address.
        private const int TransientArraySize = 32;

        // Total-operation budget across the whole charstring (shared through the recursive
        // Execute calls). MaxDepth bounds call *nesting*, but each level can fan out to
        // thousands of callsubr/callgsubr ops, so depth alone allows ~fanout^MaxDepth executions:
        // an effectively unbounded CPU loop on a hostile font that throws no exception. This is
        // a DoS backstop, not a spec limit: it sits far above any legitimate glyph (the most
        // detailed real glyphs interpret well under a million ops) and a charstring that exceeds
        // it throws, caught by the per-glyph try/catch, which yields a blank glyph.
        private const int MaxOperations = 10_000_000;

        private readonly IGeometryContext _context;
        private readonly Matrix _transform;
        private readonly CffIndex _globalSubrs;
        private readonly CffIndex _localSubrs;
        private readonly int _globalBias;
        private readonly int _localBias;

        // Resolves seac components (CFF1, non-CID only); null disables seac, which is also how a
        // seac component is kept from composing again.
        private readonly CffTable? _seacSource;

        private readonly Span<double> _stack;
        private int _sp;

        private double _x;
        private double _y;
        private bool _open;
        private bool _widthParsed;
        private int _stemCount;
        private bool _done;
        private int _depth;
        private int _ops;

        // CFF1 arithmetic / storage state. CFF2 removes those operators, so it never touches either.
        private readonly bool _isCff2;
        private double[]? _transient;
        private uint _randomState;

        // CFF2 variation state. _vstore + _activeCoords drive blend's region scalers (cached per
        // vsindex into _blendScalers); null / empty for CFF1, where blend / vsindex never appear.
        private ItemVariationStore? _vstore;
        private ReadOnlySpan<float> _activeCoords;
        private Span<float> _blendScalers;
        private int _vsindex;
        private int _blendRegionCount;
        private bool _blendScalersValid;

        public Type2CharStringInterpreter(
            IGeometryContext context,
            Matrix transform,
            CffIndex globalSubrs,
            CffIndex localSubrs,
            Span<double> stack,
            CffTable? seacSource = null)
        {
            _context = context;
            _seacSource = seacSource;
            _transform = transform;
            _globalSubrs = globalSubrs;
            _localSubrs = localSubrs;
            _globalBias = Bias(globalSubrs.Count);
            _localBias = Bias(localSubrs.Count);
            _stack = stack;
            _sp = 0;
            _x = 0;
            _y = 0;
            _open = false;
            _widthParsed = false;
            _stemCount = 0;
            _done = false;
            _depth = 0;
            _ops = 0;
            _isCff2 = false;
            _transient = null;
            _randomState = 0x9E3779B9;
            _vstore = null;
            _activeCoords = default;
            _blendScalers = default;
            _vsindex = 0;
            _blendRegionCount = -1;
            _blendScalersValid = false;
        }

        /// <summary>
        /// Creates an interpreter for a CFF2 charstring, enabling the blend / vsindex operators.
        /// <paramref name="defaultVsIndex"/> is the glyph's Font DICT Private <c>vsindex</c>, which
        /// blends use until the charstring selects another with the <c>vsindex</c> operator.
        /// </summary>
        public Type2CharStringInterpreter(
            IGeometryContext context,
            Matrix transform,
            CffIndex globalSubrs,
            CffIndex localSubrs,
            Span<double> stack,
            ItemVariationStore? vstore,
            int defaultVsIndex,
            ReadOnlySpan<float> activeCoords,
            Span<float> blendScalers)
            : this(context, transform, globalSubrs, localSubrs, stack)
        {
            _isCff2 = true;
            _vstore = vstore;
            _vsindex = defaultVsIndex;
            _activeCoords = activeCoords;
            _blendScalers = blendScalers;
            _widthParsed = true; // CFF2 charstrings carry no leading width
        }

        /// <summary>Runs the top-level charstring and closes the final contour.</summary>
        public void Run(ReadOnlyMemory<byte> charString)
        {
            Execute(charString);

            if (_open)
            {
                _context.EndFigure(true);
                _open = false;
            }
        }

        private static int Bias(int subrCount)
            => subrCount < 1240 ? 107 : subrCount < 33900 ? 1131 : 32768;

        private enum WidthRule { Stem, RMove, HVMove, EndChar }

        private void Execute(ReadOnlyMemory<byte> codeMemory)
        {
            if (++_depth > MaxDepth)
            {
                throw new InvalidOperationException("CFF subroutine recursion limit exceeded.");
            }

            var code = codeMemory.Span;
            int i = 0;

            while (i < code.Length && !_done)
            {
                // Charge every token (operand or operator) against the shared budget. The cost
                // of a callsubr/callgsubr is the work inside it, which is charged the same way,
                // so a subr that re-calls itself burns budget and is stopped here.
                if (++_ops > MaxOperations)
                {
                    throw new InvalidOperationException("CFF charstring operation budget exceeded.");
                }

                byte b0 = code[i];

                // Operands (numbers).
                if (b0 == 28)
                {
                    _stack[_sp++] = (short)((code[i + 1] << 8) | code[i + 2]);
                    i += 3;
                    continue;
                }

                if (b0 >= 32)
                {
                    if (b0 <= 246)
                    {
                        _stack[_sp++] = b0 - 139;
                        i += 1;
                    }
                    else if (b0 <= 250)
                    {
                        _stack[_sp++] = ((b0 - 247) * 256) + code[i + 1] + 108;
                        i += 2;
                    }
                    else if (b0 <= 254)
                    {
                        _stack[_sp++] = (-(b0 - 251) * 256) - code[i + 1] - 108;
                        i += 2;
                    }
                    else // 255: 16.16 fixed
                    {
                        int fixedValue = (code[i + 1] << 24) | (code[i + 2] << 16) | (code[i + 3] << 8) | code[i + 4];
                        _stack[_sp++] = fixedValue / 65536.0;
                        i += 5;
                    }

                    continue;
                }

                // Operators.
                i++;

                switch (b0)
                {
                    case 1: // hstem
                    case 3: // vstem
                    case 18: // hstemhm
                    case 23: // vstemhm
                        CountStems();
                        break;

                    case 19: // hintmask
                    case 20: // cntrmask
                        CountStems();
                        i += (_stemCount + 7) / 8;
                        break;

                    case 21: // rmoveto
                        MoveTo(WidthRule.RMove);
                        break;
                    case 22: // hmoveto
                        HMoveTo();
                        break;
                    case 4: // vmoveto
                        VMoveTo();
                        break;

                    case 5: RLineTo(); break;
                    case 6: AlternatingLineTo(startHorizontal: true); break;
                    case 7: AlternatingLineTo(startHorizontal: false); break;
                    case 8: RrCurveTo(); break;
                    case 24: RCurveLine(); break;
                    case 25: RLineCurve(); break;
                    case 26: VvCurveTo(); break;
                    case 27: HhCurveTo(); break;
                    case 30: AlternatingCurveTo(startHorizontal: false); break;
                    case 31: AlternatingCurveTo(startHorizontal: true); break;

                    case 10: // callsubr
                    {
                        int index = (int)_stack[--_sp] + _localBias;
                        if ((uint)index < (uint)_localSubrs.Count)
                        {
                            Execute(_localSubrs[index]);
                        }
                        break;
                    }
                    case 29: // callgsubr
                    {
                        int index = (int)_stack[--_sp] + _globalBias;
                        if ((uint)index < (uint)_globalSubrs.Count)
                        {
                            Execute(_globalSubrs[index]);
                        }
                        break;
                    }
                    case 11: // return
                        _depth--;
                        return;

                    case 14: // endchar
                        EndChar();
                        _depth--;
                        return;

                    case 15: // vsindex (CFF2): select the active ItemVariationData for blends
                        _vsindex = (int)_stack[--_sp];
                        _blendScalersValid = false;
                        break;

                    case 16: // blend (CFF2)
                        Blend();
                        break;

                    case 12: // escape: two-byte operator
                    {
                        byte b1 = code[i];
                        i++;
                        switch (b1)
                        {
                            case 34: HFlex(); break;
                            case 35: Flex(); break;
                            case 36: HFlex1(); break;
                            case 37: Flex1(); break;
                            default:
                                if (_isCff2 || !TryArithmetic(b1))
                                {
                                    _sp = 0; // reserved operator: drop the operands
                                }

                                break;
                        }
                        break;
                    }

                    default:
                        _sp = 0;
                        break;
                }
            }

            _depth--;
        }

        // The CFF1 arithmetic, storage and conditional operators (Type 2 spec, sections 4.4 to 4.6).
        // Each pops its arguments and pushes its result in place. An operator with too few operands
        // clears the stack, as a reserved operator does. Returns false for an operator not in this set.
        private bool TryArithmetic(byte op)
        {
            int arity = op switch
            {
                5 or 9 or 14 or 18 or 21 or 26 or 27 or 29 => 1, // not abs neg drop get sqrt dup index
                3 or 4 or 10 or 11 or 12 or 15 or 20 or 24 or 28 or 30 => 2, // and or add sub div eq put mul exch roll
                22 => 4, // ifelse
                23 => 0, // random
                _ => -1
            };

            if (arity < 0)
            {
                return false;
            }

            if (_sp < arity)
            {
                _sp = 0;
                return true;
            }

            switch (op)
            {
                case 3: Replace2(_stack[_sp - 2] != 0 && _stack[_sp - 1] != 0 ? 1 : 0); break; // and
                case 4: Replace2(_stack[_sp - 2] != 0 || _stack[_sp - 1] != 0 ? 1 : 0); break; // or
                case 5: _stack[_sp - 1] = _stack[_sp - 1] == 0 ? 1 : 0; break; // not
                case 9: _stack[_sp - 1] = Math.Abs(_stack[_sp - 1]); break; // abs
                case 10: Replace2(_stack[_sp - 2] + _stack[_sp - 1]); break; // add
                case 11: Replace2(_stack[_sp - 2] - _stack[_sp - 1]); break; // sub
                case 12: // div
                    Replace2(_stack[_sp - 1] != 0 ? _stack[_sp - 2] / _stack[_sp - 1] : 0);
                    break;
                case 14: _stack[_sp - 1] = -_stack[_sp - 1]; break; // neg
                case 15: Replace2(_stack[_sp - 2] == _stack[_sp - 1] ? 1 : 0); break; // eq
                case 18: _sp--; break; // drop
                case 20: // put: val i put
                {
                    int index = (int)_stack[_sp - 1];
                    if ((uint)index < TransientArraySize)
                    {
                        (_transient ??= new double[TransientArraySize])[index] = _stack[_sp - 2];
                    }

                    _sp -= 2;
                    break;
                }
                case 21: // get: i get
                {
                    int index = (int)_stack[_sp - 1];
                    _stack[_sp - 1] = (uint)index < TransientArraySize && _transient is not null
                        ? _transient[index]
                        : 0;
                    break;
                }
                case 22: // ifelse: s1 s2 v1 v2 ifelse -> s1 when v1 <= v2, else s2
                {
                    double result = _stack[_sp - 2] <= _stack[_sp - 1] ? _stack[_sp - 4] : _stack[_sp - 3];
                    _sp -= 3;
                    _stack[_sp - 1] = result;
                    break;
                }
                case 23: Push(NextRandom()); break; // random
                case 24: Replace2(_stack[_sp - 2] * _stack[_sp - 1]); break; // mul
                case 26: _stack[_sp - 1] = _stack[_sp - 1] > 0 ? Math.Sqrt(_stack[_sp - 1]) : 0; break; // sqrt
                case 27: Push(_stack[_sp - 1]); break; // dup
                case 28: (_stack[_sp - 2], _stack[_sp - 1]) = (_stack[_sp - 1], _stack[_sp - 2]); break; // exch
                case 29: Index(); break;
                case 30: Roll(); break;
            }

            return true;
        }

        private void Replace2(double result)
        {
            _sp--;
            _stack[_sp - 1] = result;
        }

        private void Push(double value)
        {
            if (_sp >= _stack.Length)
            {
                throw new InvalidOperationException("CFF charstring operand stack overflow.");
            }

            _stack[_sp++] = value;
        }

        // num(n-1) ... num0 i index: replaces i with a copy of num_i, counting from the top. A
        // negative i copies num0.
        private void Index()
        {
            int i = (int)_stack[--_sp];

            if (i < 0)
            {
                i = 0;
            }

            if (i >= _sp)
            {
                _sp = 0;
                return;
            }

            Push(_stack[_sp - 1 - i]);
        }

        // num(n-1) ... num0 n j roll: rotates the top n operands by j, positive j moving them toward
        // the top of the stack.
        private void Roll()
        {
            int j = (int)_stack[--_sp];
            int n = (int)_stack[--_sp];

            if (n <= 0 || n > _sp)
            {
                _sp = 0;
                return;
            }

            var window = _stack.Slice(_sp - n, n);
            int shift = ((j % n) + n) % n;

            // Rotating right by `shift` is the three-reversal rotation.
            window.Reverse();
            window.Slice(0, shift).Reverse();
            window.Slice(shift).Reverse();
        }

        // random: a value in (0, 1]. A fixed-seed xorshift keeps outlines reproducible across runs.
        private double NextRandom()
        {
            uint x = _randomState;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _randomState = x;

            return (x + 1.0) / (uint.MaxValue + 1.0);
        }

        private int ConsumeWidth(WidthRule rule)
        {
            int skip = 0;
            if (!_widthParsed)
            {
                skip = rule switch
                {
                    WidthRule.Stem => (_sp & 1) == 1 ? 1 : 0,
                    WidthRule.RMove => _sp > 2 ? 1 : 0,
                    WidthRule.HVMove => _sp > 1 ? 1 : 0,
                    WidthRule.EndChar => _sp == 1 || _sp == 5 ? 1 : 0,
                    _ => 0
                };
                _widthParsed = true;
            }

            return skip;
        }

        private void CountStems()
        {
            int skip = ConsumeWidth(WidthRule.Stem);
            _stemCount += (_sp - skip) / 2;
            _sp = 0;
        }

        private Point T(double x, double y) => _transform.Transform(new Point(x, y));

        private void StartContour()
        {
            if (_open)
            {
                _context.EndFigure(true);
            }

            _context.BeginFigure(T(_x, _y), true);
            _open = true;
        }

        private void LineSegment(double x, double y)
        {
            _x = x;
            _y = y;
            _context.LineTo(T(x, y));
        }

        private void CurveSegment(double c1x, double c1y, double c2x, double c2y, double ex, double ey)
        {
            _context.CubicBezierTo(T(c1x, c1y), T(c2x, c2y), T(ex, ey));
            _x = ex;
            _y = ey;
        }

        private void MoveTo(WidthRule rule)
        {
            int s = ConsumeWidth(rule);
            _x += _stack[s];
            _y += _stack[s + 1];
            StartContour();
            _sp = 0;
        }

        private void HMoveTo()
        {
            int s = ConsumeWidth(WidthRule.HVMove);
            _x += _stack[s];
            StartContour();
            _sp = 0;
        }

        private void VMoveTo()
        {
            int s = ConsumeWidth(WidthRule.HVMove);
            _y += _stack[s];
            StartContour();
            _sp = 0;
        }

        private void RLineTo()
        {
            for (int k = 0; k + 2 <= _sp; k += 2)
            {
                LineSegment(_x + _stack[k], _y + _stack[k + 1]);
            }

            _sp = 0;
        }

        private void AlternatingLineTo(bool startHorizontal)
        {
            bool horizontal = startHorizontal;
            for (int k = 0; k < _sp; k++)
            {
                if (horizontal)
                {
                    LineSegment(_x + _stack[k], _y);
                }
                else
                {
                    LineSegment(_x, _y + _stack[k]);
                }

                horizontal = !horizontal;
            }

            _sp = 0;
        }

        private void RrCurveTo()
        {
            for (int k = 0; k + 6 <= _sp; k += 6)
            {
                double c1x = _x + _stack[k];
                double c1y = _y + _stack[k + 1];
                double c2x = c1x + _stack[k + 2];
                double c2y = c1y + _stack[k + 3];
                double ex = c2x + _stack[k + 4];
                double ey = c2y + _stack[k + 5];
                CurveSegment(c1x, c1y, c2x, c2y, ex, ey);
            }

            _sp = 0;
        }

        private void RCurveLine()
        {
            int k = 0;
            for (; k + 6 <= _sp - 2; k += 6)
            {
                double c1x = _x + _stack[k];
                double c1y = _y + _stack[k + 1];
                double c2x = c1x + _stack[k + 2];
                double c2y = c1y + _stack[k + 3];
                double ex = c2x + _stack[k + 4];
                double ey = c2y + _stack[k + 5];
                CurveSegment(c1x, c1y, c2x, c2y, ex, ey);
            }

            LineSegment(_x + _stack[k], _y + _stack[k + 1]);
            _sp = 0;
        }

        private void RLineCurve()
        {
            int k = 0;
            for (; k + 2 <= _sp - 6; k += 2)
            {
                LineSegment(_x + _stack[k], _y + _stack[k + 1]);
            }

            double c1x = _x + _stack[k];
            double c1y = _y + _stack[k + 1];
            double c2x = c1x + _stack[k + 2];
            double c2y = c1y + _stack[k + 3];
            double ex = c2x + _stack[k + 4];
            double ey = c2y + _stack[k + 5];
            CurveSegment(c1x, c1y, c2x, c2y, ex, ey);
            _sp = 0;
        }

        private void HhCurveTo()
        {
            int k = 0;
            double dy1 = 0;
            if (((_sp) % 4) == 1)
            {
                dy1 = _stack[k];
                k++;
            }

            for (; k + 4 <= _sp; k += 4)
            {
                double c1x = _x + _stack[k];
                double c1y = _y + dy1;
                double c2x = c1x + _stack[k + 1];
                double c2y = c1y + _stack[k + 2];
                double ex = c2x + _stack[k + 3];
                double ey = c2y;
                CurveSegment(c1x, c1y, c2x, c2y, ex, ey);
                dy1 = 0;
            }

            _sp = 0;
        }

        private void VvCurveTo()
        {
            int k = 0;
            double dx1 = 0;
            if (((_sp) % 4) == 1)
            {
                dx1 = _stack[k];
                k++;
            }

            for (; k + 4 <= _sp; k += 4)
            {
                double c1x = _x + dx1;
                double c1y = _y + _stack[k];
                double c2x = c1x + _stack[k + 1];
                double c2y = c1y + _stack[k + 2];
                double ex = c2x;
                double ey = c2y + _stack[k + 3];
                CurveSegment(c1x, c1y, c2x, c2y, ex, ey);
                dx1 = 0;
            }

            _sp = 0;
        }

        // hvcurveto (startHorizontal = true) / vhcurveto (startHorizontal = false): a run of curves
        // whose start tangent alternates horizontal/vertical, with an optional 5th delta on the final
        // curve applied to the otherwise-zero coordinate of its end point.
        private void AlternatingCurveTo(bool startHorizontal)
        {
            int k = 0;
            int remaining = _sp;
            bool horizontal = startHorizontal;

            while (remaining >= 4)
            {
                double df = remaining == 5 ? _stack[k + 4] : 0;

                if (horizontal)
                {
                    double c1x = _x + _stack[k];
                    double c1y = _y;
                    double c2x = c1x + _stack[k + 1];
                    double c2y = c1y + _stack[k + 2];
                    double ey = c2y + _stack[k + 3];
                    double ex = c2x + df;
                    CurveSegment(c1x, c1y, c2x, c2y, ex, ey);
                }
                else
                {
                    double c1x = _x;
                    double c1y = _y + _stack[k];
                    double c2x = c1x + _stack[k + 1];
                    double c2y = c1y + _stack[k + 2];
                    double ex = c2x + _stack[k + 3];
                    double ey = c2y + df;
                    CurveSegment(c1x, c1y, c2x, c2y, ex, ey);
                }

                horizontal = !horizontal;
                k += 4;
                remaining -= 4;
            }

            _sp = 0;
        }

        private void Flex()
        {
            double c1x = _x + _stack[0];
            double c1y = _y + _stack[1];
            double c2x = c1x + _stack[2];
            double c2y = c1y + _stack[3];
            double jx = c2x + _stack[4];
            double jy = c2y + _stack[5];
            CurveSegment(c1x, c1y, c2x, c2y, jx, jy);

            double c4x = jx + _stack[6];
            double c4y = jy + _stack[7];
            double c5x = c4x + _stack[8];
            double c5y = c4y + _stack[9];
            double ex = c5x + _stack[10];
            double ey = c5y + _stack[11];
            CurveSegment(c4x, c4y, c5x, c5y, ex, ey);
            _sp = 0;
        }

        private void HFlex()
        {
            double y0 = _y;
            double c1x = _x + _stack[0];
            double c1y = y0;
            double c2x = c1x + _stack[1];
            double c2y = y0 + _stack[2];
            double jx = c2x + _stack[3];
            double jy = c2y;
            CurveSegment(c1x, c1y, c2x, c2y, jx, jy);

            double c4x = jx + _stack[4];
            double c4y = jy;
            double c5x = c4x + _stack[5];
            double c5y = y0;
            double ex = c5x + _stack[6];
            double ey = y0;
            CurveSegment(c4x, c4y, c5x, c5y, ex, ey);
            _sp = 0;
        }

        private void HFlex1()
        {
            double y0 = _y;
            double c1x = _x + _stack[0];
            double c1y = _y + _stack[1];
            double c2x = c1x + _stack[2];
            double c2y = c1y + _stack[3];
            double jx = c2x + _stack[4];
            double jy = c2y;
            CurveSegment(c1x, c1y, c2x, c2y, jx, jy);

            double c4x = jx + _stack[5];
            double c4y = jy;
            double c5x = c4x + _stack[6];
            double c5y = c4y + _stack[7];
            double ex = c5x + _stack[8];
            double ey = y0;
            CurveSegment(c4x, c4y, c5x, c5y, ex, ey);
            _sp = 0;
        }

        private void Flex1()
        {
            double startX = _x;
            double startY = _y;

            double c1x = _x + _stack[0];
            double c1y = _y + _stack[1];
            double c2x = c1x + _stack[2];
            double c2y = c1y + _stack[3];
            double jx = c2x + _stack[4];
            double jy = c2y + _stack[5];
            CurveSegment(c1x, c1y, c2x, c2y, jx, jy);

            double c4x = jx + _stack[6];
            double c4y = jy + _stack[7];
            double c5x = c4x + _stack[8];
            double c5y = c4y + _stack[9];

            double dx = (_stack[0] + _stack[2] + _stack[4] + _stack[6] + _stack[8]);
            double dy = (_stack[1] + _stack[3] + _stack[5] + _stack[7] + _stack[9]);

            double ex;
            double ey;
            if (Math.Abs(dx) > Math.Abs(dy))
            {
                ex = c5x + _stack[10];
                ey = startY;
            }
            else
            {
                ex = startX;
                ey = c5y + _stack[10];
            }

            CurveSegment(c4x, c4y, c5x, c5y, ex, ey);
            _sp = 0;
        }

        private void EndChar()
        {
            int s = ConsumeWidth(WidthRule.EndChar);

            if (_open)
            {
                _context.EndFigure(true);
                _open = false;
            }

            // endchar with four arguments is the seac accent composition: adx ady bchar achar. The
            // base glyph is drawn at the origin and the accent offset by (adx, ady), both addressed
            // by Standard Encoding code.
            if (_sp - s == 4 && _seacSource is not null)
            {
                double adx = _stack[s];
                double ady = _stack[s + 1];
                int baseCode = (int)_stack[s + 2];
                int accentCode = (int)_stack[s + 3];

                if (_seacSource.TryGetSeacComponent(baseCode, out var baseCharString) &&
                    _seacSource.TryGetSeacComponent(accentCode, out var accentCharString))
                {
                    RunSeacComponent(baseCharString, _transform);
                    RunSeacComponent(accentCharString, Matrix.CreateTranslation(adx, ady) * _transform);
                }
            }

            _sp = 0;
            _done = true;
        }

        // Interprets a seac component as a glyph of its own: fresh operand stack, origin, width and
        // hint state, sharing this charstring's subroutines and operation budget.
        private void RunSeacComponent(ReadOnlyMemory<byte> charString, Matrix transform)
        {
            Span<double> stack = stackalloc double[48];
            var component = new Type2CharStringInterpreter(_context, transform, _globalSubrs, _localSubrs, stack);
            component._ops = _ops;
            component.Run(charString);
            _ops = component._ops;
        }

        // CFF2 blend: the stack holds n default values, then n*k deltas (k = the region count for the
        // current vsindex; value-major: all k deltas for value 0, then value 1, ...), then the count n
        // on top. Each default is replaced in place by default + sum_j(delta_j * scaler_j); the deltas
        // and the count are dropped, leaving the n blended values for the following operator.
        private void Blend()
        {
            if (_sp <= 0)
            {
                return;
            }

            int n = (int)_stack[--_sp];

            if (!_blendScalersValid)
            {
                _blendRegionCount = _vstore?.ComputeBlendScalers(_vsindex, _activeCoords, _blendScalers) ?? 0;
                _blendScalersValid = true;
            }

            int k = _blendRegionCount;

            // Validate the operand count in long: n and k are both attacker-influenced, so
            // `n * k` (and `n * (k + 1)`) can overflow int to a negative value that slips a
            // `baseIndex < 0` check and then throws on the stack index. baseIndex = _sp - n*(k+1).
            if (n < 0 || k < 0 || (long)n * (k + 1) > _sp)
            {
                _sp = 0;
                return;
            }

            int baseIndex = _sp - n - (n * k);

            for (int valueIndex = 0; valueIndex < n; valueIndex++)
            {
                double value = _stack[baseIndex + valueIndex];
                int deltaBase = baseIndex + n + (valueIndex * k);

                for (int region = 0; region < k; region++)
                {
                    value += _stack[deltaBase + region] * _blendScalers[region];
                }

                _stack[baseIndex + valueIndex] = value;
            }

            // Keep the n blended values; drop the deltas and the count.
            _sp = baseIndex + n;
        }
    }
}
