namespace Avalonia.Input.Navigation;

internal sealed class XYFocusOptions
{
    public InputElement? SearchRoot { get; set; }
    public Rect ExclusionRect { get; set; }
    public Rect? FocusHintRectangle { get; set; }
    public Rect? FocusedElementBounds { get; set; }
    public XYFocusNavigationStrategy? NavigationStrategyOverride { get; set; }
    public bool IgnoreClipping { get; set; }
    public bool IgnoreCone { get; set; }
    public KeyDeviceType? KeyDeviceType { get; set; }
    public bool ConsiderEngagement { get; set; }
    public bool UpdateManifold { get; set; }
    public bool UpdateManifoldsFromFocusHintRect { get; set; }
    public bool IgnoreOcclusivity { get; set; }

    public XYFocusOptions()
    {
        Reset();
    }

    internal void Reset()
    {
        SearchRoot = null;
        ExclusionRect = default;
        FocusHintRectangle = null;
        FocusedElementBounds = null;
        NavigationStrategyOverride = null;
        IgnoreClipping = true;
        IgnoreCone = false;
        KeyDeviceType = null;
        ConsiderEngagement = true;
        UpdateManifold = true;
        UpdateManifoldsFromFocusHintRect = false;
        IgnoreOcclusivity = false;
    }
}
