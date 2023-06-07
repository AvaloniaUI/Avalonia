using Avalonia.Compatibility;
using Avalonia.Metadata;

namespace Avalonia.Markup.Xaml.MarkupExtensions;

public sealed class OnPlatformExtension : OnPlatformExtensionBase<object, On>
{
    public OnPlatformExtension()
    {

    }

    public OnPlatformExtension(object defaultValue)
    {
        Default = defaultValue;
    }

    public static bool ShouldProvideOption(string option)
    {
        return ShouldProvideOptionInternal(option);
    }
}

public sealed class OnPlatformExtension<TReturn> : OnPlatformExtensionBase<TReturn, On<TReturn>>
{
    public OnPlatformExtension()
    {

    }

    public OnPlatformExtension(TReturn defaultValue)
    {
        Default = defaultValue;
    }

    public static bool ShouldProvideOption(string option)
    {
        return ShouldProvideOptionInternal(option);
    }
}

public abstract class OnPlatformExtensionBase<TReturn, TOn> : IAddChild<TOn>
    where TOn : On<TReturn>
{
    [MarkupExtensionDefaultOption]
    public TReturn? Default { get; set; }

    [MarkupExtensionOption("WINDOWS")]
    public TReturn? Windows { get; set; }

    [MarkupExtensionOption("OSX")]
    // ReSharper disable once InconsistentNaming
    public TReturn? macOS { get; set; }

    [MarkupExtensionOption("LINUX")]
    public TReturn? Linux { get; set; }

    [MarkupExtensionOption("ANDROID")]
    public TReturn? Android { get; set; }

    [MarkupExtensionOption("IOS")]
    // ReSharper disable once InconsistentNaming
    public TReturn? iOS { get; set; }

    [MarkupExtensionOption("BROWSER")]
    public TReturn? Browser { get; set; }

    // Required for the compiler, will be replaced with actual method compile time.
    public object ProvideValue() { return this; }
    void IAddChild<TOn>.AddChild(TOn child) {}

    private protected static bool ShouldProvideOptionInternal(string option)
    {
        // Instead of using OperatingSystem.IsOSPlatform(string) we use specific "Is***" methods so whole method can be trimmed by the mono linked.
        // Keep in mind it works only with const "option" parameter.
        // IsOSPlatform might work better with trimming in the future, so it should be re-visited after .NET 8/9.
        return option switch
        {
            "WINDOWS" => OperatingSystemEx.IsWindows(),
            "OSX" => OperatingSystemEx.IsMacOS(),
            "LINUX" => OperatingSystemEx.IsLinux(),
            "ANDROID" => OperatingSystemEx.IsAndroid(),
            "IOS" => OperatingSystemEx.IsIOS(),
            "BROWSER" => OperatingSystemEx.IsBrowser(),
            _ => OperatingSystemEx.IsOSPlatform(option)
        };
    }
}
