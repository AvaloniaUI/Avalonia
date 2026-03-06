using Avalonia.Metadata;

namespace Avalonia.Skia;
[PrivateApi]
public interface IGlSkiaSpecificOptionsFeature
{
    public bool UseNativeSkiaGrGlInterface { get; }
}