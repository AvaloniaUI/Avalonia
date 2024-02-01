using System;
using Avalonia.Logging;
using MicroCom.Runtime;

namespace Avalonia.Win32.WinRT;

/// <summary>
/// Any WinRT API might not be available even if Windows version is supposed to support them (Win PE, Xbox...).
/// Using ApiInformation is a typical solution in UWP/WinUI apps, so we should do as well.
/// </summary>
internal static unsafe class WinRTApiInformation
{
    private static readonly Lazy<IApiInformationStatics?> s_statics = new(() =>
    {
        if (Win32Platform.WindowsVersion.Major < 10)
        {
            return null;
        }

        try
        {
            using var apiStatics = NativeWinRTMethods.CreateActivationFactory<IApiInformationStatics>(
                "Windows.Foundation.Metadata.ApiInformation");
            return apiStatics.CloneReference();
        }
        catch (Exception ex)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Win32Platform)?
                .Log(null, "Unable to create ApiInformation instance: {0}", ex);
            return null;
        }
    });

    public static bool IsTypePresent(string typeName)
    {
        if (s_statics.Value == null)
        {
            return false;
        }

        using var typeNamePtr = new HStringInterop(typeName);
        var result = 0;
        if (s_statics.Value.IsTypePresent(typeNamePtr.Handle, &result) == 0)
        {
            return result == 1;
        }

        return false;
    }

    public static bool IsMethodPresent(string typeName, string methodName)
    {
        if (s_statics.Value == null)
        {
            return false;
        }

        using var typeNamePtr = new HStringInterop(typeName);
        using var methodNamePtr = new HStringInterop(methodName);
        var result = 0;
        if (s_statics.Value.IsMethodPresent(typeNamePtr.Handle, methodNamePtr.Handle, &result) == 0)
        {
            return result == 1;
        }

        return false;
    }

    public static bool IsMethodPresentWithArity(string typeName, string methodName, uint inputParameterCount)
    {
        if (s_statics.Value == null)
        {
            return false;
        }

        using var typeNamePtr = new HStringInterop(typeName);
        using var methodNamePtr = new HStringInterop(methodName);
        var result = 0;
        if (s_statics.Value.IsMethodPresentWithArity(typeNamePtr.Handle, methodNamePtr.Handle, inputParameterCount, &result) == 0)
        {
            return result == 1;
        }

        return false;
    }

    public static bool IsEventPresent(string typeName, string eventName)
    {
        if (s_statics.Value == null)
        {
            return false;
        }

        using var typeNamePtr = new HStringInterop(typeName);
        using var eventNamePtr = new HStringInterop(eventName);
        var result = 0;
        if (s_statics.Value.IsEventPresent(typeNamePtr.Handle, eventNamePtr.Handle, &result) == 0)
        {
            return result == 1;
        }

        return false;
    }

    public static bool IsPropertyPresent(string typeName, string propertyName)
    {
        if (s_statics.Value == null)
        {
            return false;
        }

        using var typeNamePtr = new HStringInterop(typeName);
        using var propertyNamePtr = new HStringInterop(propertyName);
        var result = 0;
        if (s_statics.Value.IsPropertyPresent(typeNamePtr.Handle, propertyNamePtr.Handle, &result) == 0)
        {
            return result == 1;
        }

        return false;
    }

    public static bool IsReadOnlyPropertyPresent(string typeName, string propertyName)
    {
        if (s_statics.Value == null)
        {
            return false;
        }

        using var typeNamePtr = new HStringInterop(typeName);
        using var propertyNamePtr = new HStringInterop(propertyName);
        var result = 0;
        if (s_statics.Value.IsReadOnlyPropertyPresent(typeNamePtr.Handle, propertyNamePtr.Handle, &result) == 0)
        {
            return result == 1;
        }

        return false;
    }

    public static bool IsWriteablePropertyPresent(string typeName, string propertyName)
    {
        if (s_statics.Value == null)
        {
            return false;
        }

        using var typeNamePtr = new HStringInterop(typeName);
        using var propertyNamePtr = new HStringInterop(propertyName);
        var result = 0;
        if (s_statics.Value.IsWriteablePropertyPresent(typeNamePtr.Handle, propertyNamePtr.Handle, &result) == 0)
        {
            return result == 1;
        }

        return false;
    }

    public static bool IsEnumNamedValuePresent(string enumTypeName, string valueName)
    {
        if (s_statics.Value == null)
        {
            return false;
        }

        using var enumTypeNamePtr = new HStringInterop(enumTypeName);
        using var valueNamePtr = new HStringInterop(valueName);
        var result = 0;
        if (s_statics.Value.IsEnumNamedValuePresent(enumTypeNamePtr.Handle, valueNamePtr.Handle, &result) == 0)
        {
            return result == 1;
        }

        return false;
    }

    public static bool IsApiContractPresentByMajor(string contractName, ushort majorVersion)
    {
        if (s_statics.Value == null)
        {
            return false;
        }

        using var contractNamePtr = new HStringInterop(contractName);
        var result = 0;
        if (s_statics.Value.IsApiContractPresentByMajor(contractNamePtr.Handle, majorVersion, &result) == 0)
        {
            return result == 1;
        }

        return false;
    }

    public static bool IsApiContractPresentByMajorAndMinor(string contractName, ushort majorVersion, ushort minorVersion)
    {
        if (s_statics.Value == null)
        {
            return false;
        }

        using var contractNamePtr = new HStringInterop(contractName);
        var result = 0;
        if (s_statics.Value.IsApiContractPresentByMajorAndMinor(contractNamePtr.Handle, majorVersion, minorVersion, &result) == 0)
        {
            return result == 1;
        }

        return false;
    }
}
