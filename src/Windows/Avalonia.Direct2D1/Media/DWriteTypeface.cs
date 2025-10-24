using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using HarfBuzzSharp;
using SharpDX.DirectWrite;

namespace Avalonia.Direct2D1.Media
{
    internal class DWriteTypeface : IPlatformTypeface
    {
        private bool _isDisposed;

        public DWriteTypeface(SharpDX.DirectWrite.Font font)
        {
            DWFont = font;

            FontFace = new FontFace(DWFont).QueryInterface<FontFace1>();

            Weight = (Avalonia.Media.FontWeight)DWFont.Weight;

            Style = (Avalonia.Media.FontStyle)DWFont.Style;

            Stretch = (Avalonia.Media.FontStretch)DWFont.Stretch;
        }

        private static uint SwapBytes(uint x)
        {
            x = (x >> 16) | (x << 16);

            return ((x & 0xFF00FF00) >> 8) | ((x & 0x00FF00FF) << 8);
        }

        public SharpDX.DirectWrite.Font DWFont { get; }

        public FontFace1 FontFace { get; }

        public Face Face { get; }

        public HarfBuzzSharp.Font Font { get; }

        public Avalonia.Media.FontWeight Weight { get; }

        public Avalonia.Media.FontStyle Style { get; }

        public Avalonia.Media.FontStretch Stretch { get; }

        private void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (!disposing)
            {
                return;
            }

            Font?.Dispose();
            Face?.Dispose();
            FontFace?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool TryGetTable(OpenTypeTag tag, out ReadOnlyMemory<byte> table)
        {
            table = default;

            var dwTag = (int)SwapBytes((uint)tag);

            if (FontFace.TryGetFontTable(dwTag, out var tableData, out _))
            {
                table = tableData.ToArray();

                return true;
            }

            return false;
        }

        public bool TryGetStream([NotNullWhen(true)] out Stream stream)
        {
            stream = default;

            var files = FontFace.GetFiles();

            if (files.Length > 0)
            {
                var file = files[0];

                var referenceKey = file.GetReferenceKey();

                stream = referenceKey.ToDataStream();

                return true;
            }

            return false;
        }
    }
}

