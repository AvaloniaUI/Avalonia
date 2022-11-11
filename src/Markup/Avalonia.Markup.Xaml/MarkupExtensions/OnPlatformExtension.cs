#nullable enable
using System;
using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Markup.Xaml.MarkupExtensions;

public class OnPlatformExtension : OnPlatformExtensionBase<object, On>
{
    public OnPlatformExtension()
    {

    }

    public OnPlatformExtension(object defaultValue)
    {
        Default = defaultValue;
    }

    public static bool ShouldProvideOption(IServiceProvider serviceProvider, OperatingSystemType option)
    {
        return serviceProvider.GetService<IRuntimePlatform>().GetRuntimeInfo().OperatingSystem == option;
    }
}

public class OnPlatformExtension<TReturn> : OnPlatformExtensionBase<TReturn, On<TReturn>>
{
    public OnPlatformExtension()
    {

    }

    public OnPlatformExtension(TReturn defaultValue)
    {
        Default = defaultValue;
    }

    public static bool ShouldProvideOption(IServiceProvider serviceProvider, OperatingSystemType option)
    {
        return serviceProvider.GetService<IRuntimePlatform>().GetRuntimeInfo().OperatingSystem == option;
    }
}

public abstract class OnPlatformExtensionBase<TReturn, TOn> : IAddChild<TOn>
    where TOn : On<TReturn>
{
    [MarkupExtensionDefaultOption]
    public TReturn? Default { get; set; }

    [MarkupExtensionOption(OperatingSystemType.WinNT)]
    public TReturn? Windows { get; set; }

    [MarkupExtensionOption(OperatingSystemType.OSX)]
    // ReSharper disable once InconsistentNaming
    public TReturn? macOS { get; set; }

    [MarkupExtensionOption(OperatingSystemType.Linux)]
    public TReturn? Linux { get; set; }

    [MarkupExtensionOption(OperatingSystemType.Android)]
    public TReturn? Android { get; set; }

    [MarkupExtensionOption(OperatingSystemType.iOS)]
    // ReSharper disable once InconsistentNaming
    public TReturn? iOS { get; set; }

    [MarkupExtensionOption(OperatingSystemType.Browser)]
    public TReturn? Browser { get; set; }

    // Required for the compiler, will be replaced with actual method compile time.
    public object ProvideValue() { return this; }
    void IAddChild<TOn>.AddChild(TOn child) {}
}
