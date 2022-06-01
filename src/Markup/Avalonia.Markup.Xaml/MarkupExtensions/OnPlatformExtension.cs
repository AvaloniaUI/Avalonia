#nullable enable
using System;
using System.Globalization;
using System.Reflection;
using Avalonia.Data.Converters;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Styling;

namespace Avalonia.Markup.Xaml.MarkupExtensions;

public class OnPlatformExtension
{
    private static readonly  object s_unset = new object();

    public OnPlatformExtension()
    {
        
    }
    
    public OnPlatformExtension(object defaultValue)
    {
        Default = defaultValue;
    }
    
    [Content]
    public object? Default { get; set; } = s_unset;
    public object? Windows { get; set; } = s_unset;
    public object? macOS { get; set; } = s_unset;
    public object? Linux { get; set; } = s_unset;
    public object? Android { get; set; } = s_unset;
    public object? iOS { get; set; } = s_unset;
    public object? Browser { get; set; } = s_unset;

    public IValueConverter? Converter { get; set; }

    public object? ConverterParameter { get; set; }

    public object? ProvideValue(IServiceProvider serviceProvider)
    {
        if (Default == s_unset
            && Windows == s_unset
            && macOS == s_unset
            && Linux == s_unset
            && Android == s_unset
            && iOS == s_unset
            && Browser == s_unset)
        {
            throw new InvalidOperationException("OnPlatformExtension requires a value to be specified for at least one platform or Default.");
        }

        var provideTarget = serviceProvider.GetService<IProvideValueTarget>();

        var targetType = provideTarget.TargetProperty switch
        {
            AvaloniaProperty ap => ap.PropertyType,
            PropertyInfo pi => pi.PropertyType,
            _ => null,
        };

        if (provideTarget.TargetObject is Setter setter)
        {
            targetType = setter.Property?.PropertyType ?? targetType;
        }
        
        if (!TryGetValueForPlatform(out var value))
        {
            return AvaloniaProperty.UnsetValue;
        }

        if (targetType is null)
        {
            return value;
        }
        
        var converter = Converter ?? DefaultValueConverter.Instance;
        return converter.Convert(value, targetType, ConverterParameter, CultureInfo.CurrentUICulture);
    }

    private bool TryGetValueForPlatform(out object? value)
    {
        var runtimeInfo = AvaloniaLocator.Current.GetRequiredService<IRuntimePlatform>().GetRuntimeInfo();

        value = runtimeInfo.OperatingSystem switch
        {
            OperatingSystemType.WinNT when Windows != s_unset => Windows,
            OperatingSystemType.Linux when Linux != s_unset => Linux,
            OperatingSystemType.OSX when macOS != s_unset => macOS,
            OperatingSystemType.Android when Android != s_unset => Android,
            OperatingSystemType.iOS when iOS != s_unset => iOS,
            OperatingSystemType.Browser when Browser != s_unset => Browser,
            _ => Default
        };

        return value != s_unset;
    }
}
