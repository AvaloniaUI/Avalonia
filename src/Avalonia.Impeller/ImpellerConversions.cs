using System.Numerics;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using NImpeller;

namespace Avalonia.Impeller;

internal static class ImpellerConversions
{
    public static ImpellerColor ToImpeller(this Color c) =>
        new()
        {
            Alpha = c.A / 255f,
            Red = c.R / 255f,
            Green = c.G / 255f,
            Blue = c.B / 255f,
            Color_space = ImpellerColorSpace.kImpellerColorSpaceSRGB
        };

    public static ImpellerPoint ToImpellerPoint(this Point p) =>
        new() { X = (float)p.X, Y = (float)p.Y };

    public static ImpellerPoint ToImpellerPoint(double x, double y) =>
        new() { X = (float)x, Y = (float)y };

    public static ImpellerRect ToImpeller(this Rect r) =>
        new() { X = (float)r.X, Y = (float)r.Y, Width = (float)r.Width, Height = (float)r.Height };

    // System.Numerics row-major, row-vector layout matches Impeller's column-major, column-vector one.
    public static ImpellerMatrix ToImpeller(this Matrix m) =>
        new Matrix4x4(
            (float)m.M11, (float)m.M12, 0f, (float)m.M13,
            (float)m.M21, (float)m.M22, 0f, (float)m.M23,
            0f, 0f, 1f, 0f,
            (float)m.M31, (float)m.M32, 0f, (float)m.M33);

    public static ImpellerRoundingRadii ToImpeller(this RoundedRect rr) =>
        new()
        {
            Top_left = ToImpellerPoint(rr.RadiiTopLeft.X, rr.RadiiTopLeft.Y),
            Top_right = ToImpellerPoint(rr.RadiiTopRight.X, rr.RadiiTopRight.Y),
            Bottom_left = ToImpellerPoint(rr.RadiiBottomLeft.X, rr.RadiiBottomLeft.Y),
            Bottom_right = ToImpellerPoint(rr.RadiiBottomRight.X, rr.RadiiBottomRight.Y)
        };

    public static ImpellerStrokeCap ToImpeller(this PenLineCap cap) => cap switch
    {
        PenLineCap.Round => ImpellerStrokeCap.kImpellerStrokeCapRound,
        PenLineCap.Square => ImpellerStrokeCap.kImpellerStrokeCapSquare,
        _ => ImpellerStrokeCap.kImpellerStrokeCapButt
    };

    public static ImpellerStrokeJoin ToImpeller(this PenLineJoin join) => join switch
    {
        PenLineJoin.Round => ImpellerStrokeJoin.kImpellerStrokeJoinRound,
        PenLineJoin.Bevel => ImpellerStrokeJoin.kImpellerStrokeJoinBevel,
        _ => ImpellerStrokeJoin.kImpellerStrokeJoinMiter
    };

    public static ImpellerTileMode ToImpeller(this GradientSpreadMethod spread) => spread switch
    {
        GradientSpreadMethod.Reflect => ImpellerTileMode.kImpellerTileModeMirror,
        GradientSpreadMethod.Repeat => ImpellerTileMode.kImpellerTileModeRepeat,
        _ => ImpellerTileMode.kImpellerTileModeClamp
    };

    // The enums line up one-to-one, as in Avalonia.Skia.
    public static ImpellerBlendMode ToImpeller(this BitmapBlendingMode blend) => blend switch
    {
        BitmapBlendingMode.Unspecified => ImpellerBlendMode.kImpellerBlendModeSourceOver,
        BitmapBlendingMode.SourceOver => ImpellerBlendMode.kImpellerBlendModeSourceOver,
        BitmapBlendingMode.Source => ImpellerBlendMode.kImpellerBlendModeSource,
        BitmapBlendingMode.Destination => ImpellerBlendMode.kImpellerBlendModeDestination,
        BitmapBlendingMode.DestinationOver => ImpellerBlendMode.kImpellerBlendModeDestinationOver,
        BitmapBlendingMode.SourceIn => ImpellerBlendMode.kImpellerBlendModeSourceIn,
        BitmapBlendingMode.DestinationIn => ImpellerBlendMode.kImpellerBlendModeDestinationIn,
        BitmapBlendingMode.SourceOut => ImpellerBlendMode.kImpellerBlendModeSourceOut,
        BitmapBlendingMode.DestinationOut => ImpellerBlendMode.kImpellerBlendModeDestinationOut,
        BitmapBlendingMode.SourceAtop => ImpellerBlendMode.kImpellerBlendModeSourceATop,
        BitmapBlendingMode.DestinationAtop => ImpellerBlendMode.kImpellerBlendModeDestinationATop,
        BitmapBlendingMode.Xor => ImpellerBlendMode.kImpellerBlendModeXor,
        BitmapBlendingMode.Plus => ImpellerBlendMode.kImpellerBlendModePlus,
        BitmapBlendingMode.Screen => ImpellerBlendMode.kImpellerBlendModeScreen,
        BitmapBlendingMode.Overlay => ImpellerBlendMode.kImpellerBlendModeOverlay,
        BitmapBlendingMode.Darken => ImpellerBlendMode.kImpellerBlendModeDarken,
        BitmapBlendingMode.Lighten => ImpellerBlendMode.kImpellerBlendModeLighten,
        BitmapBlendingMode.ColorDodge => ImpellerBlendMode.kImpellerBlendModeColorDodge,
        BitmapBlendingMode.ColorBurn => ImpellerBlendMode.kImpellerBlendModeColorBurn,
        BitmapBlendingMode.HardLight => ImpellerBlendMode.kImpellerBlendModeHardLight,
        BitmapBlendingMode.SoftLight => ImpellerBlendMode.kImpellerBlendModeSoftLight,
        BitmapBlendingMode.Difference => ImpellerBlendMode.kImpellerBlendModeDifference,
        BitmapBlendingMode.Exclusion => ImpellerBlendMode.kImpellerBlendModeExclusion,
        BitmapBlendingMode.Multiply => ImpellerBlendMode.kImpellerBlendModeMultiply,
        BitmapBlendingMode.Hue => ImpellerBlendMode.kImpellerBlendModeHue,
        BitmapBlendingMode.Saturation => ImpellerBlendMode.kImpellerBlendModeSaturation,
        BitmapBlendingMode.Color => ImpellerBlendMode.kImpellerBlendModeColor,
        BitmapBlendingMode.Luminosity => ImpellerBlendMode.kImpellerBlendModeLuminosity,
        _ => ImpellerBlendMode.kImpellerBlendModeSourceOver
    };
}
