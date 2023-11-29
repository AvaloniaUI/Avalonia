using System;
using System.IO;
using Avalonia.Platform;

namespace Avalonia.Controls;

/// <summary>
/// Represents two icons, one for use with a light theme and the other for use with a dark theme.
/// </summary>
/// <remarks>
/// A window's icon can appear in multiple locations (e.g. in the window title bar and the platform taskbar), and these locations 
/// can use different themes. This means that both light and dark icons can be visible at the same time.
/// </remarks>
public class ThemeVariantWindowIcon : WindowIcon
{
    private static IWindowIconImpl Extract(WindowIcon icon, string paramName) => icon switch
    {
        ThemeVariantWindowIcon => throw new ArgumentException($"{nameof(ThemeVariantWindowIcon)} may not recursively contain further {nameof(ThemeVariantWindowIcon)}.", paramName),
        null => throw new ArgumentNullException(paramName),
        _ => icon.PlatformImpl,
    };

    /// <inheritdoc cref="ThemeVariantWindowIcon"/>
    public ThemeVariantWindowIcon(WindowIcon light, WindowIcon dark) : base(new Impl(Extract(light, nameof(light)), Extract(dark, nameof(dark))))
    {
    }

    private class Impl : IThemeVariantWindowIconImpl
    {
        public Impl(IWindowIconImpl light, IWindowIconImpl dark)
        {
            Light = light;
            Dark = dark;
        }

        public IWindowIconImpl Light { get; }
        public IWindowIconImpl Dark { get; }

        public void Save(Stream outputStream) => throw new NotSupportedException();

    }
}

public class ThemeVariantWindowIconExtension
{
    public WindowIcon? Light { get; set; }
    public WindowIcon? Dark { get; set; }

    public ThemeVariantWindowIconExtension() { }

    public ThemeVariantWindowIconExtension(WindowIcon light, WindowIcon dark)
    {
        Light = light ?? throw new ArgumentNullException(nameof(light));
        Dark = dark ?? throw new ArgumentNullException(nameof(dark));
    }

    public object ProvideValue(IServiceProvider _) => new ThemeVariantWindowIcon(Light!, Dark!);
}
