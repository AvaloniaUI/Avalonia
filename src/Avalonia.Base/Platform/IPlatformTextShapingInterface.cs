using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Avalonia.Media;
using Avalonia.Metadata;

namespace Avalonia.Platform;

[PrivateApi]
public interface IPlatformTextShapingInterface
{
    [PrivateApi]
    public interface IWrappingGlyphTypefaceImpl : IGlyphTypeface
    {
        IWrappedPlatformTypefaceImpl RenderPlatformTypeface {get;}
    }
    
    [PrivateApi]
    public interface IWrappedPlatformTypefaceImpl : IDisposable
    {
        IShapingFontTable? TryGetTableData(uint tag);
        bool TryGetTableData(uint tag, out byte[] table);
        int UnitsPerEm {get;}
        bool IsFixedPitch { get; }
        int GlyphCount { get; }
        string FamilyName { get; }
        public FontSimulations FontSimulations { get; }
        
        bool TryGetStream([NotNullWhen(true)] out Stream? stream);
    }
    
    [PrivateApi]
    public interface IShapingFontTable : IDisposable
    {
        public IntPtr Data { get; }
        public int Length { get; }
    }
    
    public ITextShaperImpl ShaperImpl { get; }

    
    IGlyphTypeface CreateTypeface(IWrappedPlatformTypefaceImpl typeface);
    
}