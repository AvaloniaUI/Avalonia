using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Avalonia.UnitTests
{
    /// <summary>
    /// Serializes a COLR v1 table from a tree of <see cref="ColrPaint"/> nodes, so tests can state a
    /// paint graph instead of hand-computing every Offset24.
    /// </summary>
    /// <remarks>
    /// Layout: header, BaseGlyphList, LayerList, ClipList, then the paints (each paint before its
    /// Offset24 children, so every child offset is positive), then the ItemVariationStore. A
    /// <see cref="ColrPaint.Layers"/> node referenced from several places gets one LayerList slice,
    /// and a paint referenced from several LayerList entries is written once, so DAG-shaped graphs
    /// stay small.
    /// </remarks>
    public sealed class ColrV1Builder
    {
        private readonly List<(ushort Glyph, ColrPaint Paint)> _baseGlyphs = new();
        private readonly List<ClipRecord> _clips = new();
        private byte[]? _itemVariationStore;

        public ColrV1Builder BaseGlyph(ushort glyph, ColrPaint paint)
        {
            _baseGlyphs.Add((glyph, paint));
            return this;
        }

        /// <summary>Adds a ClipBox for <paramref name="glyph"/>; a <paramref name="varIndexBase"/> makes it format 2.</summary>
        public ColrV1Builder Clip(ushort glyph, short xMin, short yMin, short xMax, short yMax, uint? varIndexBase = null)
        {
            _clips.Add(new ClipRecord(glyph, xMin, yMin, xMax, yMax, varIndexBase));
            return this;
        }

        public ColrV1Builder ItemVariationStore(byte[] itemVariationStore)
        {
            _itemVariationStore = itemVariationStore;
            return this;
        }

        public byte[] Build()
        {
            var w = new Writer();

            // Header (v1): v0 fields empty.
            w.UInt16(1).UInt16(0).UInt32(0).UInt32(0).UInt16(0);
            var baseListOffsetPos = w.Reserve(4);
            var layerListOffsetPos = w.Reserve(4);
            var clipListOffsetPos = w.Reserve(4);
            w.UInt32(0); // varIndexMapOffset: implicit (outer, inner) mapping
            var ivsOffsetPos = w.Reserve(4);

            // BaseGlyphList, sorted by glyph id as the binary search requires.
            _baseGlyphs.Sort((a, b) => a.Glyph.CompareTo(b.Glyph));
            var baseListStart = w.Position;
            w.Patch32(baseListOffsetPos, (uint)baseListStart);
            w.UInt32((uint)_baseGlyphs.Count);
            var baseRecordPaintPos = new int[_baseGlyphs.Count];
            for (var i = 0; i < _baseGlyphs.Count; i++)
            {
                w.UInt16(_baseGlyphs[i].Glyph);
                baseRecordPaintPos[i] = w.Reserve(4);
            }

            // LayerList: one slice per distinct Layers node, in discovery order.
            var context = new Context(w);
            foreach (var (_, paint) in _baseGlyphs)
            {
                context.CollectLayers(paint);
            }

            var layerListStart = 0;
            var layerEntryPos = new int[context.LayerEntries.Count];
            if (context.LayerEntries.Count > 0)
            {
                layerListStart = w.Position;
                w.Patch32(layerListOffsetPos, (uint)layerListStart);
                w.UInt32((uint)context.LayerEntries.Count);
                for (var i = 0; i < layerEntryPos.Length; i++)
                {
                    layerEntryPos[i] = w.Reserve(4);
                }
            }

            if (_clips.Count > 0)
            {
                _clips.Sort((a, b) => a.Glyph.CompareTo(b.Glyph));
                var clipListStart = w.Position;
                w.Patch32(clipListOffsetPos, (uint)clipListStart);
                w.UInt8(1).UInt32((uint)_clips.Count);
                var boxOffsetPos = new int[_clips.Count];
                for (var i = 0; i < _clips.Count; i++)
                {
                    w.UInt16(_clips[i].Glyph).UInt16(_clips[i].Glyph);
                    boxOffsetPos[i] = w.Reserve(3);
                }

                for (var i = 0; i < _clips.Count; i++)
                {
                    var clip = _clips[i];
                    w.Patch24(boxOffsetPos[i], w.Position - clipListStart);
                    w.UInt8(clip.VarIndexBase is null ? 1 : 2);
                    w.Int16(clip.XMin).Int16(clip.YMin).Int16(clip.XMax).Int16(clip.YMax);
                    if (clip.VarIndexBase is { } varIndexBase)
                    {
                        w.UInt32(varIndexBase);
                    }
                }
            }

            for (var i = 0; i < _baseGlyphs.Count; i++)
            {
                var paintPos = context.Write(_baseGlyphs[i].Paint);
                w.Patch32(baseRecordPaintPos[i], (uint)(paintPos - baseListStart));
            }

            for (var i = 0; i < context.LayerEntries.Count; i++)
            {
                var paintPos = context.WriteShared(context.LayerEntries[i]);
                w.Patch32(layerEntryPos[i], (uint)(paintPos - layerListStart));
            }

            if (_itemVariationStore is not null)
            {
                w.Patch32(ivsOffsetPos, (uint)w.Position);
                w.Bytes(_itemVariationStore);
            }

            return w.ToArray();
        }

        private readonly record struct ClipRecord(
            ushort Glyph, short XMin, short YMin, short XMax, short YMax, uint? VarIndexBase);

        internal sealed class Context
        {
            private readonly Dictionary<ColrPaint, int> _shared = new(ReferenceEqualityComparer.Instance);
            private readonly Dictionary<ColrPaint, int> _layerSlices = new(ReferenceEqualityComparer.Instance);

            public Context(Writer writer) => Writer = writer;

            public Writer Writer { get; }

            public List<ColrPaint> LayerEntries { get; } = new();

            public int FirstLayerIndex(ColrPaint layers) => _layerSlices[layers];

            public void CollectLayers(ColrPaint paint)
            {
                if (paint is ColrPaint.LayersPaint layers)
                {
                    if (_layerSlices.ContainsKey(layers))
                    {
                        return;
                    }

                    _layerSlices[layers] = LayerEntries.Count;
                    LayerEntries.AddRange(layers.Children);
                }

                foreach (var child in paint.Children)
                {
                    CollectLayers(child);
                }
            }

            /// <summary>Writes a paint referenced through the LayerList, once per node.</summary>
            public int WriteShared(ColrPaint paint)
            {
                if (_shared.TryGetValue(paint, out var pos))
                {
                    return pos;
                }

                pos = Write(paint);
                _shared[paint] = pos;
                return pos;
            }

            /// <summary>Writes a paint and then its Offset24 sub-tables, returning the paint's position.</summary>
            public int Write(ColrPaint paint)
            {
                var start = Writer.Position;
                var pending = new List<(int PatchPos, Func<int> WriteTarget)>();

                paint.WriteFields(this, pending);

                foreach (var (patchPos, writeTarget) in pending)
                {
                    var targetPos = writeTarget();
                    Writer.Patch24(patchPos, targetPos - start);
                }

                return start;
            }
        }

        internal sealed class Writer
        {
            private readonly List<byte> _bytes = new();

            public int Position => _bytes.Count;

            public Writer UInt8(int value)
            {
                _bytes.Add((byte)value);
                return this;
            }

            public Writer UInt16(int value) => UInt8(value >> 8).UInt8(value);

            public Writer Int16(int value) => UInt16((ushort)(short)value);

            public Writer UInt24(int value) => UInt8(value >> 16).UInt8(value >> 8).UInt8(value);

            public Writer UInt32(uint value) => UInt16((int)(value >> 16)).UInt16((int)(value & 0xFFFF));

            public Writer Int32(int value) => UInt32((uint)value);

            public Writer F2Dot14(double value) => Int16((short)Math.Round(value * 16384));

            public Writer Fixed(double value) => Int32((int)Math.Round(value * 65536));

            public Writer Bytes(ReadOnlySpan<byte> bytes)
            {
                foreach (var b in bytes)
                {
                    _bytes.Add(b);
                }

                return this;
            }

            public int Reserve(int size)
            {
                var pos = Position;
                for (var i = 0; i < size; i++)
                {
                    _bytes.Add(0);
                }

                return pos;
            }

            public void Patch24(int position, int value)
            {
                if (value < 0 || value > 0xFFFFFF)
                {
                    throw new InvalidOperationException($"Offset24 out of range: {value}");
                }

                _bytes[position] = (byte)(value >> 16);
                _bytes[position + 1] = (byte)(value >> 8);
                _bytes[position + 2] = (byte)value;
            }

            public void Patch32(int position, uint value)
            {
                Span<byte> tmp = stackalloc byte[4];
                BinaryPrimitives.WriteUInt32BigEndian(tmp, value);
                for (var i = 0; i < 4; i++)
                {
                    _bytes[position + i] = tmp[i];
                }
            }

            public byte[] ToArray() => _bytes.ToArray();
        }
    }

    /// <summary>A COLR v1 color stop: offset and alpha are F2DOT14 values, the color is a CPAL entry.</summary>
    public readonly record struct ColrStop(double Offset, ushort PaletteIndex, double Alpha = 1.0, uint? VarIndexBase = null);

    /// <summary>A node of a COLR v1 paint graph, serialized by <see cref="ColrV1Builder"/>.</summary>
    public abstract class ColrPaint
    {
        private ColrPaint()
        {
        }

        internal virtual IEnumerable<ColrPaint> Children => Array.Empty<ColrPaint>();

        internal abstract void WriteFields(ColrV1Builder.Context context, List<(int, Func<int>)> pending);

        /// <summary>PaintColrLayers (format 1).</summary>
        public static ColrPaint Layers(params ColrPaint[] layers) => new LayersPaint(layers);

        /// <summary>PaintSolid (format 2), or PaintVarSolid (format 3) when <paramref name="varIndexBase"/> is set.</summary>
        public static ColrPaint Solid(ushort paletteIndex, double alpha = 1.0, uint? varIndexBase = null)
            => new Fields(c =>
            {
                c.Writer.UInt8(varIndexBase is null ? 2 : 3).UInt16(paletteIndex).F2Dot14(alpha);
                if (varIndexBase is { } v)
                {
                    c.Writer.UInt32(v);
                }
            });

        /// <summary>PaintLinearGradient (format 4).</summary>
        public static ColrPaint LinearGradient(short x0, short y0, short x1, short y1, short x2, short y2,
            byte extend, params ColrStop[] stops)
            => new Fields((c, pending) =>
            {
                c.Writer.UInt8(4);
                pending.Add((c.Writer.Reserve(3), () => WriteColorLine(c.Writer, extend, stops, isVar: false)));
                c.Writer.Int16(x0).Int16(y0).Int16(x1).Int16(y1).Int16(x2).Int16(y2);
            });

        /// <summary>PaintRadialGradient (format 6).</summary>
        public static ColrPaint RadialGradient(short x0, short y0, ushort r0, short x1, short y1, ushort r1,
            byte extend, params ColrStop[] stops)
            => new Fields((c, pending) =>
            {
                c.Writer.UInt8(6);
                pending.Add((c.Writer.Reserve(3), () => WriteColorLine(c.Writer, extend, stops, isVar: false)));
                c.Writer.Int16(x0).Int16(y0).UInt16(r0).Int16(x1).Int16(y1).UInt16(r1);
            });

        /// <summary>PaintSweepGradient (format 8); angles in F2DOT14 units of 180 degrees.</summary>
        public static ColrPaint SweepGradient(short centerX, short centerY, double startAngle, double endAngle,
            byte extend, params ColrStop[] stops)
            => new Fields((c, pending) =>
            {
                c.Writer.UInt8(8);
                pending.Add((c.Writer.Reserve(3), () => WriteColorLine(c.Writer, extend, stops, isVar: false)));
                c.Writer.Int16(centerX).Int16(centerY).F2Dot14(startAngle).F2Dot14(endAngle);
            });

        /// <summary>PaintGlyph (format 10).</summary>
        public static ColrPaint Glyph(ushort glyph, ColrPaint child)
            => new Fields(new[] { child }, (c, pending) =>
            {
                c.Writer.UInt8(10);
                pending.Add((c.Writer.Reserve(3), () => c.Write(child)));
                c.Writer.UInt16(glyph);
            });

        /// <summary>PaintColrGlyph (format 11).</summary>
        public static ColrPaint ColrGlyph(ushort glyph) => new Fields(c => c.Writer.UInt8(11).UInt16(glyph));

        /// <summary>
        /// PaintTransform (format 12), or PaintVarTransform (format 13) when
        /// <paramref name="varIndexBase"/> is set (the VarIndexBase is the last field of the VarAffine2x3).
        /// </summary>
        public static ColrPaint Transform(double xx, double yx, double xy, double yy, double dx, double dy,
            ColrPaint child, uint? varIndexBase = null)
            => new Fields(new[] { child }, (c, pending) =>
            {
                c.Writer.UInt8(varIndexBase is null ? 12 : 13);
                pending.Add((c.Writer.Reserve(3), () => c.Write(child)));
                pending.Add((c.Writer.Reserve(3), () =>
                {
                    var pos = c.Writer.Position;
                    c.Writer.Fixed(xx).Fixed(yx).Fixed(xy).Fixed(yy).Fixed(dx).Fixed(dy);
                    if (varIndexBase is { } v)
                    {
                        c.Writer.UInt32(v);
                    }

                    return pos;
                }));
            });

        /// <summary>PaintTranslate (format 14).</summary>
        public static ColrPaint Translate(short dx, short dy, ColrPaint child)
            => new Fields(new[] { child }, (c, pending) =>
            {
                c.Writer.UInt8(14);
                pending.Add((c.Writer.Reserve(3), () => c.Write(child)));
                c.Writer.Int16(dx).Int16(dy);
            });

        /// <summary>PaintSkew (format 28); angles in F2DOT14 units of 180 degrees.</summary>
        public static ColrPaint Skew(double xSkewAngle, double ySkewAngle, ColrPaint child)
            => new Fields(new[] { child }, (c, pending) =>
            {
                c.Writer.UInt8(28);
                pending.Add((c.Writer.Reserve(3), () => c.Write(child)));
                c.Writer.F2Dot14(xSkewAngle).F2Dot14(ySkewAngle);
            });

        /// <summary>PaintComposite (format 32).</summary>
        public static ColrPaint Composite(byte mode, ColrPaint source, ColrPaint backdrop)
            => new Fields(new[] { source, backdrop }, (c, pending) =>
            {
                c.Writer.UInt8(32);
                pending.Add((c.Writer.Reserve(3), () => c.Write(source)));
                c.Writer.UInt8(mode);
                pending.Add((c.Writer.Reserve(3), () => c.Write(backdrop)));
            });

        /// <summary>Verbatim paint bytes with no sub-tables, for malformed-input tests.</summary>
        public static ColrPaint Raw(params byte[] bytes) => new Fields(c => c.Writer.Bytes(bytes));

        private static int WriteColorLine(ColrV1Builder.Writer w, byte extend, ColrStop[] stops, bool isVar)
        {
            var pos = w.Position;
            w.UInt8(extend).UInt16(stops.Length);
            foreach (var stop in stops)
            {
                w.F2Dot14(stop.Offset).UInt16(stop.PaletteIndex).F2Dot14(stop.Alpha);
                if (isVar)
                {
                    w.UInt32(stop.VarIndexBase ?? 0xFFFFFFFF);
                }
            }

            return pos;
        }

        internal sealed class LayersPaint : ColrPaint
        {
            public LayersPaint(ColrPaint[] children) => LayerChildren = children;

            public ColrPaint[] LayerChildren { get; }

            internal override IEnumerable<ColrPaint> Children => LayerChildren;

            internal override void WriteFields(ColrV1Builder.Context context, List<(int, Func<int>)> pending)
            {
                context.Writer.UInt8(1).UInt8(LayerChildren.Length).UInt32((uint)context.FirstLayerIndex(this));
            }
        }

        private sealed class Fields : ColrPaint
        {
            private readonly ColrPaint[] _children;
            private readonly Action<ColrV1Builder.Context, List<(int, Func<int>)>> _write;

            public Fields(Action<ColrV1Builder.Context> write)
                : this(Array.Empty<ColrPaint>(), (c, _) => write(c))
            {
            }

            public Fields(Action<ColrV1Builder.Context, List<(int, Func<int>)>> write)
                : this(Array.Empty<ColrPaint>(), write)
            {
            }

            public Fields(ColrPaint[] children, Action<ColrV1Builder.Context, List<(int, Func<int>)>> write)
            {
                _children = children;
                _write = write;
            }

            internal override IEnumerable<ColrPaint> Children => _children;

            internal override void WriteFields(ColrV1Builder.Context context, List<(int, Func<int>)> pending)
                => _write(context, pending);
        }
    }
}
