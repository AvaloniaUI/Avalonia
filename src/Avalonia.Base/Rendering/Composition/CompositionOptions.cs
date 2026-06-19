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

    
    /// <summary>
    /// Controls the eagerness of merging dirty rects. WPF uses 50000, Avalonia currently has a different default
    /// that's a subject to change. You can play with this property to find the best value for your application.
    /// </summary>
    [Unstable]
    public double? DirtyRectMergeEagerness { get; set; }
    
    /// <summary>
    /// Enforces dirty contents to be rendered into an extra intermediate surface before being applied onto the
    /// saved frame.
    /// Required as a workaround for Skia bug https://issues.skia.org/issues/327877721
    /// </summary>
    public bool? UseSaveLayerRootClip { get; set; }
}