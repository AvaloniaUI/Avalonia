using Avalonia.Metadata;

namespace Avalonia.Rendering.Composition;

public class CompositionOptions
{
    /// <summary>
    /// Enables more accurate tracking of dirty rects by utilizing regions if supported by the underlying
    /// drawing context
    /// </summary>
    public bool? UseRegionDirtyRectClipping { get; set; }

    /// <summary>
    /// The maximum number of dirty rects to track when region clip is in use. Setting this to zero or negative
    /// value will remove the smarter algorithm and will use underlying drawing context region support directly.
    /// Default value is 8.
    /// </summary>
    public int? MaxDirtyRects { get; set; }

    [Unstable]
    public double? DirtyRectMergeEagerness { get; set; }
    
    /// <summary>
    /// Enforces dirty contents to be rendered into an extra intermediate surface before being applied onto the
    /// saved frame.
    /// Required as a workaround for Skia bug https://issues.skia.org/issues/327877721
    /// </summary>
    public bool? UseSaveLayerRootClip { get; set; }
}