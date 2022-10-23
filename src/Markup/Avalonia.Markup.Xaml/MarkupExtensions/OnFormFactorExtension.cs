#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Platform;

namespace Avalonia.Markup.Xaml.MarkupExtensions;

public class OnFormFactorExtension : OnFormFactorExtension<object>
{
    public OnFormFactorExtension()
    {

    }

    public OnFormFactorExtension(object defaultValue) : base(defaultValue)
    {
    }
}

public class OnFormFactorExtension<TReturn>
{
    private readonly Dictionary<string, TReturn?> _values = new();

    public OnFormFactorExtension()
    {

    }

    public OnFormFactorExtension(TReturn defaultValue)
    {
        Default = defaultValue;
    }

    public TReturn? Default
    {
        get => _values.TryGetValue(nameof(Default), out var value) ? value : default;
        set { _values[nameof(Default)] = value; }
    }

    public TReturn? Desktop
    {
        get => _values.TryGetValue(nameof(Desktop), out var value) ? value : default;
        set { _values[nameof(Desktop)] = value; }
    }

    public TReturn? Mobile
    {
        get => _values.TryGetValue(nameof(Mobile), out var value) ? value : default;
        set { _values[nameof(Mobile)] = value; }
    }


    public object? ProvideValue()
    {
        if (!_values.Any())
        {
            throw new InvalidOperationException(
                "OnPlatformExtension requires a value to be specified for at least one platform or Default.");
        }

        var (value, hasValue) = TryGetValueForPlatform();
        return !hasValue ? AvaloniaProperty.UnsetValue : value;
    }

    private (object? value, bool hasValue) TryGetValueForPlatform()
    {
        var runtimeInfo = AvaloniaLocator.Current.GetRequiredService<IRuntimePlatform>().GetRuntimeInfo();

        TReturn val;

        if (runtimeInfo.IsDesktop)
        {
            if (_values.TryGetValue(nameof(Desktop), out val))
            {
                return (val, true);
            }
        }

        if (runtimeInfo.IsMobile)
        {
            if (_values.TryGetValue(nameof(Mobile), out val))
            {
                return (val, true);
            }
        }

        if (_values.TryGetValue(nameof(Default), out val))
        {
            return (val, true);
        }

        return default;
    }
}
