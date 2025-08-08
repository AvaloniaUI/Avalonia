using System;
using Avalonia.Platform.Storage;
using Avalonia.Utilities;

namespace Avalonia.Input;

/// <summary>
/// Represents a format usable with the clipboard and drag-and-drop.
/// </summary>
public sealed record DataFormat
{
    private DataFormat(DataFormatKind kind, string identifier)
    {
        Kind = kind;
        Identifier = identifier;
    }

    /// <summary>
    /// Gets the kind of the data format.
    /// </summary>
    public DataFormatKind Kind { get; }

    /// <summary>
    /// Gets the identifier of the data format.
    /// </summary>
    public string Identifier { get; }

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

    /// <summary>
    /// Creates a name for this format, usable by the operating system.
    /// </summary>
    /// <param name="applicationPrefix">The system prefix used to recognize the name as an application format.</param>
    /// <returns>A system name for the format.</returns>
    /// <remarks>
    /// This method can only be called if <see cref="Kind"/> is
    /// <see cref="DataFormatKind.Application"/> or <see cref="DataFormatKind.OperatingSystem"/>.
    /// </remarks>
    public string ToSystemName(string applicationPrefix)
    {
        ThrowHelper.ThrowIfNull(applicationPrefix);

        return Kind switch
        {
            DataFormatKind.Application => applicationPrefix + Identifier,
            DataFormatKind.OperatingSystem => Identifier,
            _ => throw new InvalidOperationException($"Cannot get system name for cross-platform format {Identifier}")
        };
    }

    private static DataFormat CreateCrossPlatformFormat(string identifier)
        => new(DataFormatKind.CrossPlatform, identifier);

    /// <summary>
    /// Creates a new format specific to the application.
    /// </summary>
    /// <param name="identifier">
    /// <para>
    /// The format identifier. To avoid conflicts with system identifiers, this value isn't passed to the underlying
    /// operating system directly. However, two different applications using the same identifier
    /// with <see cref="CreateApplicationFormat"/> are able to share data using this format.
    /// </para>
    /// <para>Only ASCII letters (A-Z, a-z), digits (0-9), the dot (.) and the hyphen (-) are accepted.</para>
    /// </param>
    /// <returns>A new <see cref="DataFormat"/>.</returns>
    public static DataFormat CreateApplicationFormat(string identifier)
    {
        if (!IsValidApplicationFormatIdentifier(identifier))
            throw new ArgumentException("Invalid application identifier", nameof(identifier));

        return new DataFormat(DataFormatKind.Application, identifier);
    }

    /// <summary>
    /// Creates a new format for the current operating system.
    /// </summary>
    /// <param name="identifier">
    /// The format identifier. This value is not validated and is passed AS IS to the underlying operating system.
    /// Most systems use mime types, but macOS requires Uniform Type Identifiers (UTI).
    /// </param>
    /// <returns>A new <see cref="DataFormat"/>.</returns>
    public static DataFormat CreateOperatingSystemFormat(string identifier)
    {
        ThrowHelper.ThrowIfNullOrEmpty(identifier);

        return new DataFormat(DataFormatKind.OperatingSystem, identifier);
    }

    /// <summary>
    /// Creates a <see cref="DataFormat"/> from a name coming from the underlying operating system.
    /// </summary>
    /// <param name="systemName">The name.</param>
    /// <param name="applicationPrefix">The system prefix used to recognize the name as an application format.</param>
    /// <returns>A <see cref="DataFormat"/> corresponding to <paramref name="systemName"/>.</returns>
    public static DataFormat FromSystemName(string systemName, string applicationPrefix)
    {
        ThrowHelper.ThrowIfNull(systemName);
        ThrowHelper.ThrowIfNull(applicationPrefix);

        if (systemName.StartsWith(applicationPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var identifier = systemName.Substring(applicationPrefix.Length);
            if (IsValidApplicationFormatIdentifier(identifier))
                return new DataFormat(DataFormatKind.Application, identifier);
        }

        return new DataFormat(DataFormatKind.OperatingSystem, systemName);
    }

    private static bool IsValidApplicationFormatIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return false;

        foreach (var c in identifier)
        {
            if (!IsValidChar(c))
                return false;
        }

        return true;

        static bool IsValidChar(char c)
            => IsAsciiLetterOrDigit(c) || c == '.' || c == '-';

        static bool IsAsciiLetterOrDigit(char c)
        {
#if NET8_0_OR_GREATER
            return char.IsAsciiLetterOrDigit(c);
#else
            return c is
                (>= '0' and <= '9') or
                (>= 'A' and <= 'Z') or
                (>= 'a' and <= 'z');
#endif
        }
    }

    /// <inheritdoc />
    public override string ToString()
        => $"{Kind}: {Identifier}";
}
