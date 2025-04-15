namespace Avalonia.Skia;

public enum SurfaceOrientation
{
    Normal,
    Rotated90,
    Rotated180,
    Rotated270,
    Unknown,
}

public interface ISurfaceOrientation
{
    SurfaceOrientation Orientation { get; set; }
}
