namespace Avalonia.Rendering.Composition.Drawing;

internal enum RenderDataOpcode : byte
{
    Invalid = 0,
    DrawLine,
    DrawRectangle,
    DrawEllipse,
    DrawGeometry,
    DrawGlyphRun,
    DrawBitmap,
    DrawCustom,
    PushClip,
    PushGeometryClip,
    PushOpacity,
    PushOpacityMask,
    PushTransform,
    PushRenderOptions,
    PushTextOptions,
    Pop
}
