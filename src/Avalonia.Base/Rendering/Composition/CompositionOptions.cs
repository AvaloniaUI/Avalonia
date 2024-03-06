namespace Avalonia.Rendering.Composition;

public class CompositionOptions
{
    /// <summary>
    /// Enables more accurate tracking of dirty rects by utilizing regions if supported by the underlying
    /// drawing context
    /// </summary>
    public bool? UseRegionDirtyRectClipping { get; set; }
    /// <summary>
    /// Enforces dirty contents to be rendered into an extra intermediate surface before being applied onto the
    /// saved frame.
    /// Required as a workaround for Skia bug https://issues.skia.org/issues/327877721
    /// </summary>
    public bool? UseSaveLayerRootClip { get; set; }
}