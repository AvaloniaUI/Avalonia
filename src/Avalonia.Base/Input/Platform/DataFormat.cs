using System;
using Avalonia.Platform.Storage;
using Avalonia.Utilities;

namespace Avalonia.Input.Platform;

/// <summary>
/// Represents a format usable with the clipboard and drag-and-drop.
/// </summary>
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

    /// <summary>
    /// Gets the kind of the data format.
    /// </summary>
    public DataFormatKind Kind { get; }

    /// <summary>
    /// Gets the identifier of the data format.
    /// </summary>
    public string Identifier { get; }

    public string SystemName { get; }

    /// <summary>
    /// Gets a data format representing plain text.
    /// Its data type is <see cref="string"/>.
    /// </summary>
    public static DataFormat Text { get; } = CreateCrossPlatformFormat("Text");

    /// <summary>
    /// Gets a data format representing a single file.
    /// Its data type is <see cref="IStorageItem"/>.
    /// </summary>
    public static DataFormat File { get; } = CreateCrossPlatformFormat("File");

    private static DataFormat CreateCrossPlatformFormat(string identifier)
        => new(DataFormatKind.CrossPlatform, identifier);

    /// <summary>
    /// Creates a new format specific to the application.
    /// </summary>
    /// <param name="identifier">
    /// The format identifier. To avoid conflicts with system identifiers, this value isn't passed to the underlying
    /// operating system directly but might be mangled. However, two different applications using the same identifier
    /// with <see cref="CreateApplicationFormat"/> are able to share data using this format.
    /// </param>
    /// <returns>A new <see cref="DataFormat"/>.</returns>
    public static DataFormat CreateApplicationFormat(string identifier)
    {
        ThrowHelper.ThrowIfNullOrEmpty(identifier);
        return new DataFormat(DataFormatKind.Application, identifier);
    }

    /// <summary>
    /// Creates a new format for the current operating system.
    /// </summary>
    /// <param name="identifier">
    /// The format identifier. This value is passed AS IS to the underlying operating system.
    /// Consequently, the identifier should be a valid value.
    /// Most systems use mime types, but macOS requires Uniform Type Identifiers (UTI).
    /// </param>
    /// <returns>A new <see cref="DataFormat"/>.</returns>
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
        => $"{Kind}: {Identifier}";
}
