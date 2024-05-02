namespace Avalonia.LinuxFramebuffer;

/// <summary>
/// specific the type of connector is HDMI-A, DVI, DisplayPort, etc.
/// </summary>
public enum DrmConnectorType : uint
{
    None,
    VGA,
    DVI_I,
    DVI_D,
    DVI_A,
    Composite,
    S_Video,
    LVDS,
    Component,
    DIN,
    DisplayPort,
    HDMI_A,
    HDMI_B,
    TV,
    eDP,
    Virtual,
    DSI,
}
