using System;
using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Markup.Xaml.MarkupExtensions;

public sealed class OnFormFactorExtension : OnFormFactorExtensionBase<object, On>
{
    public OnFormFactorExtension()
    {

    }

    public OnFormFactorExtension(object defaultValue)
    {
        Default = defaultValue;
    }

    public static bool ShouldProvideOption(IServiceProvider serviceProvider, FormFactorType option)
    {
        return serviceProvider.GetService<IRuntimePlatform>()?.GetRuntimeInfo().FormFactor == option;
    }
}

public sealed class OnFormFactorExtension<TReturn> : OnFormFactorExtensionBase<TReturn, On<TReturn>>
{
    public OnFormFactorExtension()
    {

    }

    public OnFormFactorExtension(TReturn defaultValue)
    {
        Default = defaultValue;
    }

    public static bool ShouldProvideOption(IServiceProvider serviceProvider, FormFactorType option)
    {
        return serviceProvider.GetService<IRuntimePlatform>()?.GetRuntimeInfo().FormFactor == option;
    }
}

public abstract class OnFormFactorExtensionBase<TReturn, TOn> : IAddChild<TOn>
    where TOn : On<TReturn>
{
    [MarkupExtensionDefaultOption]
    public TReturn? Default { get; set; }

    [MarkupExtensionOption(FormFactorType.Desktop)]
    public TReturn? Desktop { get; set; }

    [MarkupExtensionOption(FormFactorType.Mobile)]
    public TReturn? Mobile { get; set; }

    // Required for the compiler, will be replaced with actual method compile time.
    public object ProvideValue() { return this; }
    void IAddChild<TOn>.AddChild(TOn child) {}
}
