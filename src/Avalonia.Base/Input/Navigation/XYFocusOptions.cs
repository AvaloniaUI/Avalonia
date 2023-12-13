namespace Avalonia.Input.Navigation;

internal record XYFocusOptions
{
    public InputElement? SearchRoot { get; set; }
    public Rect ExclusionRect { get; set; }
    public Rect? FocusHintRectangle { get; set; }
    public Rect? FocusedElementBoundsOverride { get; set; }
    public XYFocusNavigationStrategy? NavigationStrategyOverride { get; set; }
    public bool IgnoreClipping { get; set; } = true;
    public bool IgnoreCone { get; set; }
    public bool ShouldConsiderXYFocusKeyboardNavigation { get; set; }
    public bool ConsiderEngagement { get; set; } = true;
    public bool UpdateManifold { get; set; } = true;
    public bool UpdateManifoldsFromFocusHintRect { get; set; }
    public bool IgnoreOcclusivity { get; set; }
}
