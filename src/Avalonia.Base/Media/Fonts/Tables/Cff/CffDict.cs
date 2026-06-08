using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Avalonia.Media.Fonts.Tables.Cff
{
    /// <summary>
    /// A parsed CFF / CFF2 DICT: a sequence of operands followed by an operator, decoded into an
    /// operator → operand-array map. Used for the Top DICT, Private DICT and (CID) Font DICTs.
    /// DICTs are parsed once at table-load time, so a small dictionary allocation is acceptable here.
    /// </summary>
    internal sealed class CffDict
    {
        // Two-byte operators (escape 12 xx) are keyed as 1200 + xx to keep a flat integer key space.
        internal const int TwoByteOperatorBase = 1200;

        private readonly Dictionary<int, double[]> _entries;

        private CffDict(Dictionary<int, double[]> entries) => _entries = entries;

        public static CffDict Parse(ReadOnlySpan<byte> data)
        {
            var entries = new Dictionary<int, double[]>();

            // CFF2 DICTs can hold long operand lists for blended values (e.g. BlueValues blended across
            // many regions), well beyond CFF's 48. A DICT is bounded in size, so 513 (the CFF2 stack
            // limit) covers any real DICT; an over-long pathological list is caught by the caller's
            // try/catch. The keys we actually read (CharStrings / FDArray / Private / vstore) are short.
            Span<double> operands = stackalloc double[513];
            int sp = 0;
            int i = 0;

            while (i < data.Length)
            {
                byte b0 = data[i];

                // Operands begin at 28, so bytes 0..27 are operator codes. CFF uses 0..21 (with 12 as
                // the two-byte escape); CFF2 additionally uses 24 (vstore) in the Top DICT and 22 / 23
                // (vsindex / blend) in Private DICTs. 22..27 never occur in a CFF1 DICT.
                if (b0 <= 27)
                {
                    // Operator.
                    int op = b0;
                    i++;
                    if (b0 == 12)
                    {
                        op = TwoByteOperatorBase + data[i];
                        i++;
                    }

                    entries[op] = operands.Slice(0, sp).ToArray();
                    sp = 0;
                }
                else if (b0 == 28)
                {
                    operands[sp++] = (short)((data[i + 1] << 8) | data[i + 2]);
                    i += 3;
                }
                else if (b0 == 29)
                {
                    operands[sp++] = (data[i + 1] << 24) | (data[i + 2] << 16) | (data[i + 3] << 8) | data[i + 4];
                    i += 5;
                }
                else if (b0 == 30)
                {
                    operands[sp++] = ParseReal(data, ref i);
                }
                else if (b0 <= 246)
                {
                    operands[sp++] = b0 - 139;
                    i++;
                }
                else if (b0 <= 250)
                {
                    operands[sp++] = ((b0 - 247) * 256) + data[i + 1] + 108;
                    i += 2;
                }
                else if (b0 <= 254)
                {
                    operands[sp++] = (-(b0 - 251) * 256) - data[i + 1] - 108;
                    i += 2;
                }
                else
                {
                    // 255 is reserved in a DICT; skip defensively.
                    i++;
                }
            }

            return new CffDict(entries);
        }

        /// <summary>Real (floating-point) operand: nibble-encoded, terminated by the 0xf nibble.</summary>
        private static double ParseReal(ReadOnlySpan<byte> data, ref int i)
        {
            i++; // consume the 0x1e marker
            var sb = new StringBuilder(16);
            bool done = false;

            while (!done && i < data.Length)
            {
                byte b = data[i++];
                for (int half = 0; half < 2; half++)
                {
                    int nibble = half == 0 ? (b >> 4) : (b & 0x0f);
                    switch (nibble)
                    {
                        case <= 9: sb.Append((char)('0' + nibble)); break;
                        case 0xa: sb.Append('.'); break;
                        case 0xb: sb.Append('E'); break;
                        case 0xc: sb.Append("E-"); break;
                        case 0xe: sb.Append('-'); break;
                        case 0xf: done = true; break;
                        default: break; // 0xd reserved
                    }

                    if (done)
                    {
                        break;
                    }
                }
            }

            return double.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : 0.0;
        }

        public bool TryGetOperands(int op, out double[] operands) => _entries.TryGetValue(op, out operands!);

        /// <summary>First operand of <paramref name="op"/> as an int, or <paramref name="fallback"/> if absent.</summary>
        public int GetInt(int op, int fallback = 0)
            => _entries.TryGetValue(op, out var v) && v.Length > 0 ? (int)v[0] : fallback;

        /// <summary>Whether the DICT contains <paramref name="op"/>.</summary>
        public bool Contains(int op) => _entries.ContainsKey(op);
    }
}
