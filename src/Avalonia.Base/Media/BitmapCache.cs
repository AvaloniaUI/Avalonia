using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Media;

public class BitmapCache : CacheMode
{
    private CompositionBitmapCache? _current;
    
    public static readonly StyledProperty<double> RenderAtScaleProperty = AvaloniaProperty.Register<BitmapCache, double>(
        "RenderAtScale", 1);

    /// <summary>
    /// Use the RenderAtScale property to render the BitmapCache at a multiple of the normal bitmap size.
    /// The normal size is determined by the local size of the element.
    ///
    /// Values greater than 1 increase the resolution of the bitmap relative to the native resolution of the element,
    /// and values less than 1 decrease the resolution.
    /// For example, if the RenderAtScale property is set to 2.0, and you apply a scale transform that
    /// enlarges the content by a factor of 2, the content will have the same visual quality as the same content
    /// with RenderAtScale set to 1.0 and a transform scale of 1.
    ///
    /// When RenderAtScale is set to 0, no bitmap is rendered. Negative values are clamped to 0.
    /// 
    /// If you change this value, the cache is regenerated at the appropriate new resolution.
    /// </summary>
    public double RenderAtScale
    {
        get => GetValue(RenderAtScaleProperty);
        set => SetValue(RenderAtScaleProperty, value);
    }
    
    public static readonly StyledProperty<bool> SnapsToDevicePixelsProperty = AvaloniaProperty.Register<BitmapCache, bool>(
        "SnapsToDevicePixels");
    
    /// <summary>
    /// Set the SnapsToDevicePixels property when the cache displays content that requires pixel-alignment to render correctly.
    /// This is the case for text with subpixel antialiasing. If you set the EnableClearType property to true,
    /// consider setting SnapsToDevicePixels to true to ensure proper rendering.
    ///
    /// When the SnapsToDevicePixels property is set to false,
    /// you can move and scale the cached element by a fraction of a pixel.
    /// 
    /// When the SnapsToDevicePixels property is set to true,
    /// the bitmap cache is aligned with pixel boundaries of the destination.
    /// If you move or scale the cached element by a fraction of a pixel,
    /// the bitmap snaps to the pixel grid
    /// . In this case, the top-left corner of the bitmap is rounded up and snapped to the pixel grid,
    /// but the bottom-right corner is on a fractional pixel boundary.
    /// </summary>
    public bool SnapsToDevicePixels
    {
        get => GetValue(SnapsToDevicePixelsProperty);
        set => SetValue(SnapsToDevicePixelsProperty, value);
    }

    public static readonly StyledProperty<bool> EnableClearTypeProperty = AvaloniaProperty.Register<BitmapCache, bool>(
        "EnableClearType");

    public bool EnableClearType
    {
        get => GetValue(EnableClearTypeProperty);
        set => SetValue(EnableClearTypeProperty, value);
    }


    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.IsEffectiveValueChange && _current != null)
        {
            if (change.Property == RenderAtScaleProperty)
                _current.RenderAtScale = RenderAtScale;
            else if (change.Property == SnapsToDevicePixelsProperty)
                _current.SnapsToDevicePixels = SnapsToDevicePixels;
            else if (change.Property == EnableClearTypeProperty)
                _current.EnableClearType = EnableClearType;
        }

        base.OnPropertyChanged(change);
    }

    // We currently only allow visual to be attached to one compositor at a time, so keep it simple for now
    internal override CompositionCacheMode GetForCompositor(Compositor c)
    {
        // TODO: Make it to be a multi-compositor resource once we support visuals being attached to multiple
        // compositor instances (e. g. referenced via visual brush from a different WASM toplevel).
        if(_current?.Compositor != c)
        {
            _current = new CompositionBitmapCache(c, new ServerCompositionBitmapCache(c.Server));
            _current.EnableClearType = EnableClearType;
            _current.RenderAtScale = RenderAtScale;
            _current.SnapsToDevicePixels = SnapsToDevicePixels;
        }

        return _current;
    }
}