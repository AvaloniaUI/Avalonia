using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia;

internal class SKTypefaceWrapper(SKTypeface typeface, FontSimulations fontSimulations)
    : IPlatformTextShapingInterface.IWrappedPlatformTypefaceImpl
{
    public SKTypeface Typeface => typeface;
    public int UnitsPerEm => typeface.UnitsPerEm;
    public bool IsFixedPitch => typeface.IsFixedPitch;
    public int GlyphCount => typeface.GlyphCount;
    public string FamilyName => typeface.FamilyName;
    public FontSimulations FontSimulations => fontSimulations;

    public SKFont CreateSKFont(float size)
        => new(Typeface, size, skewX: (FontSimulations & FontSimulations.Oblique) != 0 ? -0.3f : 0.0f)
        {
            LinearMetrics = true,
            Embolden = (FontSimulations & FontSimulations.Bold) != 0
        };

    
    class TableData : IPlatformTextShapingInterface.IShapingFontTable
    {
        public TableData(int size)
        {
            Length = size;
            Data = Marshal.AllocHGlobal(size);
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(Data);
        }

        public IntPtr Data { get; }
        public int Length { get; }
    }

    public IPlatformTextShapingInterface.IShapingFontTable? TryGetTableData(uint tag)
    {
        var size = typeface.GetTableSize(tag);
        if (size == 0)
            return null;
        var table = new TableData(size);
        if (!typeface.TryGetTableData(tag, 0, size, table.Data))
        {
            table.Dispose();
            return null;
        }

        return table;
    }
    
    public bool TryGetTableData(uint tag, out byte[] table) => typeface.TryGetTableData(tag, out table);
    
    public bool TryGetStream([NotNullWhen(true)] out Stream? stream)
    {
        try
        {
            var asset = typeface.OpenStream();
            var size = asset.Length;
            var buffer = new byte[size];

            asset.Read(buffer, size);

            stream = new MemoryStream(buffer);

            return true;
        }
        catch
        {
            stream = null;

            return false;
        }
    }

    public void Dispose() => typeface.Dispose();
}