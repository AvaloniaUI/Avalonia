namespace Avalonia.Controls;

/// <summary>
/// Represents an object which particpates in platform text scaling. This is an accessibility feature
/// which allows the user to request that text be drawn larger (or on some platforms smaller) than normal,
/// without altering other UI elements.
/// </summary>
public interface IPlatformTextScaleable
{
    bool IsPlatformTextScalingEnabled { get; }
    void OnPlatformTextScalingChanged();

    /// <summary>
    /// Scales a font size according to the current system text scaling rules and the value of <see cref="IsPlatformTextScalingEnabled"/>.
    /// </summary>
    double GetScaledFontSize(double baseFontSize);
}
