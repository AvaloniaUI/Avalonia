namespace Avalonia.Input;

/// <summary>
/// Represents the kind of a <see cref="DataFormat"/>.
/// </summary>
public enum DataFormatKind
{
    /// <summary>
    /// <para>
    /// The data format is specific to the application.
    /// The exact format name used internally by Avalonia will vary depending on the platform.
    /// </para>
    /// <para>
    /// Such a format is created using <see cref="DataFormat.CreateBytesApplicationFormat"/>
    /// or <see cref="DataFormat.CreateStringApplicationFormat"/>.
    /// </para>
    /// </summary>
    /// <seealso cref="DataFormat.CreateBytesApplicationFormat"/>
    /// <seealso cref="DataFormat.CreateStringApplicationFormat"/>
    Application,

    /// <summary>
    /// <para>
    /// The data format is specific to the current platform.
    /// Any other application using the same identifier will be able to access it.
    /// </para>
    /// <para>
    /// Such a format is created using <see cref="DataFormat.CreateBytesPlatformFormat"/>
    /// or <see cref="DataFormat.CreateStringPlatformFormat"/>.
    /// </para>
    /// </summary>
    /// <seealso cref="DataFormat.CreateBytesPlatformFormat"/>
    /// <seealso cref="DataFormat.CreateStringPlatformFormat"/>
    Platform,

    /// <summary>
    /// <para>
    /// The data format is cross-platform and supported directly by Avalonia.
    /// Such formats include <see cref="DataFormat.Text"/> and <see cref="DataFormat.File"/>.
    /// </para>
    /// <para>
    /// It is not possible to create such a format directly.
    /// </para>
    /// </summary>
    Universal
}
