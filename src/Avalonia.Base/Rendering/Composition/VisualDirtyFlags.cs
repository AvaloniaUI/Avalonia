using System;

namespace Avalonia.Rendering.Composition;

/// <summary>
/// The parts of a <see cref="Visual"/> that the renderer must copy to its composition visual in the next update.
/// </summary>
[Flags]
internal enum VisualDirtyFlags
{
    None = 0,
    Offset = 1 << 0,
    Size = 1 << 1,
    Visible = 1 << 2,
    Opacity = 1 << 3,
    ClipToBounds = 1 << 4,
    Clip = 1 << 5,
    OpacityMask = 1 << 6,
    CacheMode = 1 << 7,
    Effect = 1 << 8,
    BackdropEffect = 1 << 9,
    RenderOptions = 1 << 10,
    TextOptions = 1 << 11,
    Transform = 1 << 12,

    /// <summary>
    /// The draw list, which <see cref="Visual.Render"/> records.
    /// </summary>
    Content = 1 << 13,
    Children = 1 << 14,

    CompositionProperties = Offset | Size | Visible | Opacity | ClipToBounds | Clip | OpacityMask | CacheMode
                            | Effect | BackdropEffect | RenderOptions | TextOptions | Transform,
    All = CompositionProperties | Content | Children
}
