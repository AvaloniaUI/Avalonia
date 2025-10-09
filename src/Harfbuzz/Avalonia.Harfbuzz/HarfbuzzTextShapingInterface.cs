using System;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Harfbuzz;

class HarfbuzzTextShapingInterface : IPlatformTextShapingInterface
{
    public ITextShaperImpl ShaperImpl { get; } = new TextShaperImpl();
    public IGlyphTypeface CreateTypeface(IPlatformTextShapingInterface.IWrappedPlatformTypefaceImpl typeface) => new GlyphTypefaceImpl(typeface);
}

[PrivateApi]
public static class HarfbuzzTextShaping
{
    public static IPlatformTextShapingInterface Create() => new HarfbuzzTextShapingInterface();
}