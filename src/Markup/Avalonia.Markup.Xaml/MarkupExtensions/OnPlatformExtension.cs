#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Markup.Xaml.MarkupExtensions;

public class On
{
    public string Platform { get; set; } = "Unknown";

    [Content]
    public object? Content { get; set; }
}

public class OnPlatformExtension<TReturn> : IAddChild<On>
{
    private readonly Dictionary<string, TReturn?> _values = new();

    public OnPlatformExtension(TReturn defaultValue)
    {
        Default = defaultValue;
    }

    public TReturn? Default { get => _values.TryGetValue(nameof(Default), out var value) ? value : default; set { _values[nameof(Default)] = value; } }
    public TReturn? Windows { get => _values.TryGetValue(nameof(Windows), out var value) ? value : default; set { _values[nameof(Windows)] = value; } }
    public TReturn? macOS { get => _values.TryGetValue(nameof(macOS), out var value) ? value : default; set { _values[nameof(macOS)] = value; } }
    public TReturn? Linux { get => _values.TryGetValue(nameof(Linux), out var value) ? value : default; set { _values[nameof(Linux)] = value; } }
    public TReturn? Android { get => _values.TryGetValue(nameof(Android), out var value) ? value : default; set { _values[nameof(Android)] = value; } }
    public TReturn? iOS { get => _values.TryGetValue(nameof(iOS), out var value) ? value : default; set { _values[nameof(iOS)] = value; } }
    public TReturn? Browser { get => _values.TryGetValue(nameof(Browser), out var value) ? value : default; set { _values[nameof(Browser)] = value; } }
    
    public TReturn? Desktop { get => _values.TryGetValue(nameof(Desktop), out var value) ? value : default; set { _values[nameof(Desktop)] = value; } }
    public TReturn? Mobile { get => _values.TryGetValue(nameof(Mobile), out var value) ? value : default; set { _values[nameof(Mobile)] = value; } }
    

    public object? ProvideValue()
    {
        if (!_values.Any())
        {
            throw new InvalidOperationException("OnPlatformExtension requires a value to be specified for at least one platform or Default.");
        }

        var (value, hasValue) = TryGetValueForPlatform();
        return !hasValue ? AvaloniaProperty.UnsetValue : value;
    }

    private (TReturn? value, bool hasValue) TryGetFormFactorValues(RuntimePlatformInfo runtimeInfo)
    {
        if (runtimeInfo.IsDesktop)
        {
            if (_values.TryGetValue(nameof(Desktop), out var val1))
            {
                return (val1, true);
            }
        }

        if (runtimeInfo.IsMobile)
        {
            if (_values.TryGetValue(nameof(Mobile), out var val1))
            {
                return (val1, true);
            }
        }
        
        return default;
    }

    private (TReturn? value, bool hasValue) TryGetValueForPlatform()
    {
        var runtimeInfo = AvaloniaLocator.Current.GetRequiredService<IRuntimePlatform>().GetRuntimeInfo();

        return runtimeInfo.OperatingSystem switch
        {
            OperatingSystemType.WinNT => _values.TryGetValue(nameof(Windows), out var val) ?
                (val, true) :
                TryGetFormFactorValues(runtimeInfo),
            OperatingSystemType.OSX => _values.TryGetValue(nameof(macOS), out var val) ?
                (val, true) :
                TryGetFormFactorValues(runtimeInfo),
            OperatingSystemType.Linux => _values.TryGetValue(nameof(Linux), out var val) ?
                (val, true) :
                TryGetFormFactorValues(runtimeInfo),
            OperatingSystemType.Android => _values.TryGetValue(nameof(Android), out var val) ?
                (val, true) :
                TryGetFormFactorValues(runtimeInfo),
            OperatingSystemType.iOS => _values.TryGetValue(nameof(iOS), out var val) ?
                (val, true) :
                TryGetFormFactorValues(runtimeInfo),
            OperatingSystemType.Browser => _values.TryGetValue(nameof(Browser), out var val) ?
                (val, true) :
                TryGetFormFactorValues(runtimeInfo),
            _ => _values.TryGetValue(nameof(Default), out var val) ? (val, true) : TryGetFormFactorValues(runtimeInfo)
        };
    }

    public void AddChild(On child)
    {
        foreach (var platform in child.Platform.Split(new [] { "," }, StringSplitOptions.RemoveEmptyEntries))
        {
            _values[platform.Trim()] = (TReturn?)child.Content;
        }
    }
}
