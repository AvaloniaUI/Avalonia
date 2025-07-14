using System;
using Avalonia.Utilities;

namespace Avalonia.Input.Platform;

public sealed record DataFormat
{
    private const string PrefixApplication = "avalonia-app-format:";
    private const string PrefixCrossPlatform = "avalonia-xplat-format:";

    internal DataFormat(DataFormatKind kind, string identifier)
    {
        Kind = kind;
        Identifier = identifier;

        SystemName = Kind switch
        {
            DataFormatKind.Application => PrefixApplication + Identifier,
            DataFormatKind.OperatingSystem => Identifier,
            DataFormatKind.CrossPlatform => PrefixCrossPlatform + Identifier,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    public DataFormatKind Kind { get; }

    public string Identifier { get; }

    public string SystemName { get; }

    public static DataFormat Text { get; } = CreateCrossPlatformFormat("Text");

    public static DataFormat File { get; } = CreateCrossPlatformFormat("File");

    private static DataFormat CreateCrossPlatformFormat(string identifier)
        => new(DataFormatKind.CrossPlatform, identifier);

    public static DataFormat CreateApplicationFormat(string identifier)
    {
        ThrowHelper.ThrowIfNullOrEmpty(identifier);
        return new DataFormat(DataFormatKind.Application, identifier);
    }

    public static DataFormat CreateOperatingSystemFormat(string identifier)
    {
        ThrowHelper.ThrowIfNullOrEmpty(identifier);
        return new DataFormat(DataFormatKind.OperatingSystem, identifier);
    }

    public static DataFormat FromSystemName(string systemName)
    {
        ThrowHelper.ThrowIfNullOrEmpty(systemName);

        return TryParseWithPrefix(systemName, PrefixApplication, DataFormatKind.Application)
            ?? TryParseWithPrefix(systemName, PrefixCrossPlatform, DataFormatKind.CrossPlatform)
            ?? new DataFormat(DataFormatKind.OperatingSystem, systemName);

        static DataFormat? TryParseWithPrefix(string format, string prefix, DataFormatKind kind)
            => format.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ?
                new DataFormat(kind, format.Substring(prefix.Length)) :
                null;
    }

    /// <inheritdoc />
    public override string ToString()
        => SystemName;
}

public enum DataFormatKind
{
    Application,
    OperatingSystem,
    CrossPlatform
}
