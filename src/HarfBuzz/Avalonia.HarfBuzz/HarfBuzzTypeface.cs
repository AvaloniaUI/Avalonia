using System;
using System.Runtime.InteropServices;
using Avalonia.Media;
using HarfBuzzSharp;

namespace Avalonia.Harfbuzz
{
    internal class HarfBuzzTypeface : ITextShaperTypeface
    {
        private readonly bool _ownsFace;
        private readonly NormalizedVariationPosition _variationPosition;

        public HarfBuzzTypeface(GlyphTypeface glyphTypeface)
        {
            GlyphTypeface = glyphTypeface;

            HBFace = new Face(GetTable) { UnitsPerEm = glyphTypeface.Metrics.DesignEmHeight };

            HBFont = new Font(HBFace);

            HBFont.SetFunctionsOpenType();

            _ownsFace = true;
        }

        private HarfBuzzTypeface(HarfBuzzTypeface source, Font font, NormalizedVariationPosition variation)
        {
            GlyphTypeface = source.GlyphTypeface;
            HBFace = source.HBFace;
            HBFont = font;

            _variationPosition = variation;
        }

        public GlyphTypeface GlyphTypeface { get; }
        public Face HBFace { get; }
        public Font HBFont { get; }

        NormalizedVariationPosition ITextShaperTypeface.VariationPosition => _variationPosition;

        ITextShaperTypeface ITextShaperTypeface.WithVariation(NormalizedVariationPosition variation)
        {
            if (variation == _variationPosition)
            {
                return this;
            }

            var axes = GlyphTypeface.VariationAxes;

            if (axes.Count == 0)
            {
                return this;
            }

            Span<int> coords = axes.Count <= 16 ? stackalloc int[axes.Count] : new int[axes.Count];

            for (var i = 0; i < axes.Count; i++)
            {
                variation.TryGetCoordinate(axes[i].Tag, out var value);
                coords[i] = (int)MathF.Round(value * 16384f);
            }

            var font = new Font(HBFont);

            // A sub-font delegates glyph advances to its parent, which evaluates HVAR at the
            // parent's coordinates. Installing the OpenType functions on the sub-font itself
            // makes advances and extents follow its own coordinates.
            font.SetFunctionsOpenType();

            if (!HarfBuzzNative.TrySetVarCoordsNormalized(font.Handle, coords))
            {
                font.Dispose();
                return this;
            }

            return new HarfBuzzTypeface(this, font, variation);
        }

        private Blob? GetTable(Face face, Tag tag)
        {
            if (!GlyphTypeface.PlatformTypeface.TryGetTable((uint)tag, out var table))
            {
                return null;
            }

            // If table is backed by managed array, pin it and avoid copy.
            if (MemoryMarshal.TryGetArray(table, out var seg))
            {
                var handle = GCHandle.Alloc(seg.Array!, GCHandleType.Pinned);
                var basePtr = handle.AddrOfPinnedObject();
                var ptr = IntPtr.Add(basePtr, seg.Offset);

                var release = new ReleaseDelegate(() => handle.Free());

                return new Blob(ptr, seg.Count, MemoryMode.ReadOnly, release);
            }

            // Fallback: allocate native memory and copy
            var nativePtr = Marshal.AllocHGlobal(table.Length);

            unsafe
            {
                table.Span.CopyTo(new Span<byte>((void*)nativePtr, table.Length));
            }

            var releaseDelegate = new ReleaseDelegate(() => Marshal.FreeHGlobal(nativePtr));

            return new Blob(nativePtr, table.Length, MemoryMode.ReadOnly, releaseDelegate);
        }

        public void Dispose()
        {
            HBFont.Dispose();

            // A variation instance shares the face with the typeface it was derived from.
            if (_ownsFace)
            {
                HBFace.Dispose();
            }
        }

    }
}
