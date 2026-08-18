using System;
using System.Runtime.InteropServices;
using Avalonia.Media;
using HarfBuzzSharp;

namespace Avalonia.Harfbuzz
{
    internal class HarfBuzzTypeface : ITextShaperTypeface
    {
        public HarfBuzzTypeface(GlyphTypeface glyphTypeface)
        {
            GlyphTypeface = glyphTypeface;

            HBFace = new Face(GetTable) { UnitsPerEm = glyphTypeface.Metrics.DesignEmHeight };

            HBFont = new Font(HBFace);

            HBFont.SetFunctionsOpenType();
        }

        public GlyphTypeface GlyphTypeface { get; }
        public Face HBFace { get; }
        public Font HBFont { get; }

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
            HBFace.Dispose();
        }

    }
}
