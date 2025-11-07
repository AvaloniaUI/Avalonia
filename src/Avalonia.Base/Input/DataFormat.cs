using System;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using Avalonia.Platform.Storage;
using Avalonia.Utilities;

namespace Avalonia.Input;

/// <summary>
/// Represents a format usable with the clipboard and drag-and-drop.
/// </summary>
public abstract class DataFormat : IEquatable<DataFormat>
{
    private protected DataFormat(DataFormatKind kind, string identifier)
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
    public static DataFormat<string> Text { get; } = CreateUniversalFormat<string>("Text");

    /// <summary>
    /// Gets a data format representing a bitmap.
    /// Its data type is <see cref="Media.Imaging.Bitmap"/>.
    /// </summary>
    public static DataFormat<Bitmap> Bitmap { get; } = CreateUniversalFormat<Bitmap>("Bitmap");

    /// <summary>
    /// Gets a data format representing a single file.
    /// Its data type is <see cref="IStorageItem"/>.
    /// </summary>
    public static DataFormat<IStorageItem> File { get; } = CreateUniversalFormat<IStorageItem>("File");

    /// <summary>
    /// Creates a name for this format, usable by the underlying platform.
    /// </summary>
    /// <param name="applicationPrefix">The system prefix used to recognize the name as an application format.</param>
    /// <returns>A system name for the format.</returns>
    /// <remarks>
    /// This method can only be called if <see cref="Kind"/> is
    /// <see cref="DataFormatKind.Application"/> or <see cref="DataFormatKind.Platform"/>.
    /// </remarks>
    public string ToSystemName(string applicationPrefix)
    {
        ThrowHelper.ThrowIfNull(applicationPrefix);

        return Kind switch
        {
            DataFormatKind.Application => applicationPrefix + Identifier,
            DataFormatKind.Platform => Identifier,
            _ => throw new InvalidOperationException($"Cannot get system name for universal format {Identifier}")
        };
    }

    /// <inheritdoc />
    public bool Equals(DataFormat? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Kind == other.Kind && Identifier == other.Identifier;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => Equals(obj as DataFormat);

    /// <inheritdoc />
    public override int GetHashCode()
        => ((int)Kind * 397) ^ Identifier.GetHashCode();

    /// <summary>
    /// Compares two instances of <see cref="DataFormat"/> for equality.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns>true if the two instances are equal; otherwise false.</returns>
    public static bool operator ==(DataFormat? left, DataFormat? right)
        => Equals(left, right);

    /// <summary>
    /// Compares two instances of <see cref="DataFormat"/> for inequality.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns>true if the two instances are not equal; otherwise false.</returns>
    public static bool operator !=(DataFormat? left, DataFormat? right)
        => !Equals(left, right);

    private static DataFormat<T> CreateUniversalFormat<T>(string identifier) where T : class
        => new(DataFormatKind.Universal, identifier);

    /// <summary>
    /// Creates a new format specific to the application that returns an array of <see cref="byte"/>.
    /// </summary>
    /// <param name="identifier">
    /// <para>
    /// The format identifier. To avoid conflicts with system identifiers, this value isn't passed to the underlying
    /// platform directly. However, two different applications using the same identifier
    /// with <see cref="CreateBytesApplicationFormat"/> or <see cref="CreateStringApplicationFormat"/>
    /// are able to share data using this format.
    /// </para>
    /// <para>Only ASCII letters (A-Z, a-z), digits (0-9), the dot (.) and the hyphen (-) are accepted.</para>
    /// </param>
    /// <returns>A new <see cref="DataFormat"/>.</returns>
    public static DataFormat<byte[]> CreateBytesApplicationFormat(string identifier)
        => CreateApplicationFormat<byte[]>(identifier);

    /// <summary>
    /// Creates a new format specific to the application that returns a <see cref="string"/>.
    /// </summary>
    /// <param name="identifier">
    /// <para>
    /// The format identifier. To avoid conflicts with system identifiers, this value isn't passed to the underlying
    /// platform directly. However, two different applications using the same identifier
    /// with <see cref="CreateBytesApplicationFormat"/> or <see cref="CreateStringApplicationFormat"/>
    /// are able to share data using this format.
    /// </para>
    /// <para>Only ASCII letters (A-Z, a-z), digits (0-9), the dot (.) and the hyphen (-) are accepted.</para>
    /// </param>
    /// <returns>A new <see cref="DataFormat"/>.</returns>
    public static DataFormat<string> CreateStringApplicationFormat(string identifier)
        => CreateApplicationFormat<string>(identifier);

    private static DataFormat<T> CreateApplicationFormat<T>(string identifier)
        where T : class
    {
        if (!IsValidApplicationFormatIdentifier(identifier))
            throw new ArgumentException("Invalid application identifier", nameof(identifier));

        return new(DataFormatKind.Application, identifier);
    }

    /// <summary>
    /// Creates a new format for the current platform that returns an array of <see cref="byte"/>.
    /// </summary>
    /// <param name="identifier">
    /// The format identifier. This value is not validated and is passed AS IS to the underlying platform.
    /// Most systems use mime types, but macOS requires Uniform Type Identifiers (UTI).
    /// </param>
    /// <returns>A new <see cref="DataFormat"/>.</returns>
    public static DataFormat<byte[]> CreateBytesPlatformFormat(string identifier)
        => CreatePlatformFormat<byte[]>(identifier);

    /// <summary>
    /// Creates a new format for the current platform that returns a <see cref="string"/>.
    /// </summary>
    /// <param name="identifier">
    /// The format identifier. This value is not validated and is passed AS IS to the underlying platform.
    /// Most systems use mime types, but macOS requires Uniform Type Identifiers (UTI).
    /// </param>
    /// <returns>A new <see cref="DataFormat"/>.</returns>
    public static DataFormat<string> CreateStringPlatformFormat(string identifier)
        => CreatePlatformFormat<string>(identifier);

    private static DataFormat<T> CreatePlatformFormat<T>(string identifier)
        where T : class
    {
        ThrowHelper.ThrowIfNullOrEmpty(identifier);

        return new(DataFormatKind.Platform, identifier);
    }

    /// <summary>
    /// Creates a <see cref="DataFormat"/> from a name coming from the underlying platform.
    /// </summary>
    /// <param name="systemName">The name.</param>
    /// <param name="applicationPrefix">The system prefix used to recognize the name as an application format.</param>
    /// <returns>A <see cref="DataFormat"/> corresponding to <paramref name="systemName"/>.</returns>
    [PrivateApi]
    public static DataFormat<T> FromSystemName<T>(string systemName, string applicationPrefix)
        where T : class
    {
        ThrowHelper.ThrowIfNull(systemName);
        ThrowHelper.ThrowIfNull(applicationPrefix);

        if (systemName.StartsWith(applicationPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var identifier = systemName.Substring(applicationPrefix.Length);
            if (IsValidApplicationFormatIdentifier(identifier))
                return new(DataFormatKind.Application, identifier);
        }

        return new(DataFormatKind.Platform, systemName);
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
