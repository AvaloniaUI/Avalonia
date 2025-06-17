using System;
using Avalonia.Utilities;

namespace Avalonia.Input.Platform;

public sealed record DataFormat
{
    private const string PrefixApplication = "avalonia-app-format:";
    private const string PrefixCrossPlatform = "avalonia-xplat-format:";

    internal const string NameText = "Text";
    internal const string NameFile = "File";
    internal const string NameFiles = "Files";
    internal const string NameFileNames = "FileNames";

    internal DataFormat(DataFormatKind kind, string identifier, bool isLegacy)
    {
        Kind = kind;
        Identifier = identifier;
        IsLegacy = isLegacy;
        SystemName = ComputeSystemName();
    }

    public DataFormatKind Kind { get; }

    public string Identifier { get; }

    internal string SystemName { get; }

    // TODO12: remove
    internal bool IsLegacy { get; }

    public static DataFormat Text { get; } = CreateWellKnownFormat(NameText);

    internal static DataFormat File { get; } = CreateWellKnownFormat(NameFile);

    // TODO12: remove
    internal static DataFormat Files3 { get; } = CreateWellKnownFormat(NameFiles);

    // TODO12: remove
    internal static DataFormat FileNames3 { get; } = CreateWellKnownFormat(NameFileNames);

    private static DataFormat CreateWellKnownFormat(string identifier)
        => new(DataFormatKind.CrossPlatform, identifier, isLegacy: true);

    public static DataFormat CreateApplicationFormat(string identifier)
    {
        ThrowHelper.ThrowIfNullOrEmpty(identifier);
        return new DataFormat(DataFormatKind.Application, identifier, isLegacy: false);
    }

    public static DataFormat CreateOperatingSystemFormat(string identifier)
    {
        ThrowHelper.ThrowIfNullOrEmpty(identifier);
        return new DataFormat(DataFormatKind.OperatingSystem, identifier, isLegacy: false);
    }

    internal static DataFormat Parse(string systemName)
    {
        ThrowHelper.ThrowIfNullOrEmpty(systemName);

        return TryParseWellKnownFormat(systemName)
            ?? TryParseWithPrefix(systemName, PrefixApplication, DataFormatKind.Application)
            ?? TryParseWithPrefix(systemName, PrefixCrossPlatform, DataFormatKind.CrossPlatform)
            ?? new DataFormat(DataFormatKind.OperatingSystem, systemName, isLegacy: false);

        static DataFormat? TryParseWellKnownFormat(string identifier)
        {
            if (NameText.Equals(identifier, StringComparison.OrdinalIgnoreCase))
                return Text;

            if (NameFile.Equals(identifier, StringComparison.OrdinalIgnoreCase))
                return File;

            if (NameFiles.Equals(identifier, StringComparison.OrdinalIgnoreCase))
                return Files3;

            if (NameFileNames.Equals(identifier, StringComparison.OrdinalIgnoreCase))
                return FileNames3;

            return null;
        }

        static DataFormat? TryParseWithPrefix(string format, string prefix, DataFormatKind kind)
        {
            if (!format.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return null;

            var identifier = format.Substring(prefix.Length);
            return TryParseWellKnownFormat(identifier) ?? new DataFormat(kind, identifier, isLegacy: false);
        }
    }

    private string ComputeSystemName()
    {
        // TODO12: remove
        if (IsLegacy)
            return Identifier;

        return Kind switch
        {
            DataFormatKind.Application => PrefixApplication + Identifier,
            DataFormatKind.CrossPlatform => PrefixCrossPlatform + Identifier,
            _ => Identifier
        };
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
