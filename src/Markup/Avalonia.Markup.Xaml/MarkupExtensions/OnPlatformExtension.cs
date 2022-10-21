#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Markup.Xaml.MarkupExtensions;

public static class OnPlatformExtensionHelper
{
    // TEMPORARY, to replace with XAML compiler namespace helpers
    public static bool IsOSPlatform(string platform)
    {
        var runtimeInfo = AvaloniaLocator.Current.GetRequiredService<IRuntimePlatform>().GetRuntimeInfo();
        return platform switch
        {
            "WINDOWS" => runtimeInfo.OperatingSystem == OperatingSystemType.WinNT,
            "MACOS" => runtimeInfo.OperatingSystem == OperatingSystemType.OSX,
            _ => runtimeInfo.OperatingSystem.ToString().Equals(platform, StringComparison.OrdinalIgnoreCase)
        };
        //return RuntimeInformation.IsOSPlatform(OSPlatform.Create(platform));
    }
}

public class On
{
    public string Platform { get; set; } = "Unknown";

    [Content]
    public object? Content { get; set; }
}

public class OnPlatformExtension : OnPlatformExtension<object>
{
    public OnPlatformExtension()
    {

    }

    public OnPlatformExtension(object defaultValue) : base(defaultValue)
    {
    }
}

public class OnPlatformExtension<TReturn> : IAddChild<On>
{
    private readonly Dictionary<string, TReturn?> _values = new();

    public OnPlatformExtension(TReturn defaultValue)
    {
        Default = defaultValue;
    }

    public OnPlatformExtension()
    {

    }

    public TReturn? Default { get => _values.TryGetValue(nameof(Default), out var value) ? value : default; set { _values[nameof(Default)] = value; } }
    public TReturn? Windows { get => _values.TryGetValue(nameof(Windows), out var value) ? value : default; set { _values[nameof(Windows)] = value; } }
    public TReturn? macOS { get => _values.TryGetValue(nameof(macOS), out var value) ? value : default; set { _values[nameof(macOS)] = value; } }
    public TReturn? Linux { get => _values.TryGetValue(nameof(Linux), out var value) ? value : default; set { _values[nameof(Linux)] = value; } }
    public TReturn? Android { get => _values.TryGetValue(nameof(Android), out var value) ? value : default; set { _values[nameof(Android)] = value; } }
    public TReturn? iOS { get => _values.TryGetValue(nameof(iOS), out var value) ? value : default; set { _values[nameof(iOS)] = value; } }
    public TReturn? Browser { get => _values.TryGetValue(nameof(Browser), out var value) ? value : default; set { _values[nameof(Browser)] = value; } }
    public object? ProvideValue()
    {
        throw new NotSupportedException();
        if (!_values.Any())
        {
            throw new InvalidOperationException("OnPlatformExtension requires a value to be specified for at least one platform or Default.");
        }

        var (value, hasValue) = TryGetValueForPlatform();
        return !hasValue ? AvaloniaProperty.UnsetValue : value;
    }

    private (TReturn? value, bool hasValue) TryGetValueForPlatform()
    {
        var runtimeInfo = AvaloniaLocator.Current.GetRequiredService<IRuntimePlatform>().GetRuntimeInfo();

        TReturn val;

        switch (runtimeInfo.OperatingSystem)
        {
            case OperatingSystemType.WinNT:
                if (_values.TryGetValue(nameof(Windows), out val))
                {
                    return (val, true);
                }
                break;

            case OperatingSystemType.OSX:
                if (_values.TryGetValue(nameof(macOS), out val))
                {
                    return (val, true);
                }
                break;

            case OperatingSystemType.Linux:
                if (_values.TryGetValue(nameof(Linux), out val))
                {
                    return (val, true);
                }
                break;

            case OperatingSystemType.Android:
                if (_values.TryGetValue(nameof(Android), out val))
                {
                    return (val, true);
                }
                break;

            case OperatingSystemType.iOS:
                if (_values.TryGetValue(nameof(iOS), out val))
                {
                    return (val, true);
                }
                break;

            case OperatingSystemType.Browser:
                if (_values.TryGetValue(nameof(Browser), out val))
                {
                    return (val, true);
                }
                break;
        }

        if (_values.TryGetValue(nameof(Default), out val))
        {
            return (val, true);
        };

        return default;
    }

    public void AddChild(On child)
    {
        foreach (var platform in child.Platform.Split(new [] { "," }, StringSplitOptions.RemoveEmptyEntries))
        {
            _values[platform.Trim()] = (TReturn?)child.Content;
        }
    }
}
