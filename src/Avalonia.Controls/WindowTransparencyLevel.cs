using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Avalonia.Controls;

public readonly record struct WindowTransparencyLevel
{
    private readonly string _value;

    private WindowTransparencyLevel(string value)
    {
        _value = value;
    }

    /// <summary>
    /// The window background is Black where nothing is drawn in the window.
    /// </summary>
    public static WindowTransparencyLevel None { get; } = new(nameof(None));

    /// <summary>
    /// The window background is Transparent where nothing is drawn in the window.
    /// </summary>
    public static WindowTransparencyLevel Transparent { get; } = new(nameof(Transparent));

    /// <summary>
    /// The window background is a blur-behind where nothing is drawn in the window.
    /// </summary>
    public static WindowTransparencyLevel Blur { get; } = new(nameof(Blur));

    /// <summary>
    /// The window background is a blur-behind with a high blur radius. This level may fallback to Blur.
    /// </summary>
    public static WindowTransparencyLevel AcrylicBlur { get; } = new(nameof(AcrylicBlur));

    /// <summary>
    /// The window background is based on desktop wallpaper tint with a light blur. This will only work on Windows 11 
    /// </summary>
    public static WindowTransparencyLevel MicaLight { get; } = new(nameof(MicaLight));
    
    /// <summary>
    /// The window background is based on desktop wallpaper tint with a dark blur. This will only work on Windows 11
    /// </summary>
    public static WindowTransparencyLevel MicaDark { get; } = new(nameof(MicaDark));

    public override string ToString()
    {
        return _value;
    }
}

public class WindowTransparencyLevelCollection : ReadOnlyCollection<WindowTransparencyLevel>
{
    public WindowTransparencyLevelCollection(IList<WindowTransparencyLevel> list) : base(list)
    {
    }
} 
