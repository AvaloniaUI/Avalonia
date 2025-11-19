using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Input;

/// <summary>
/// Represents a format usable with the clipboard and drag-and-drop, with a data type.
/// </summary>
/// <typeparam name="T">The data type.</typeparam>
/// <remarks>
/// This class cannot be instantiated directly.
/// Use universal formats such as <see cref="DataFormat.Text"/> and <see cref="DataFormat.File"/>,
/// or create custom formats using <see cref="DataFormat.CreateBytesApplicationFormat"/>,
/// <see cref="DataFormat.CreateStringApplicationFormat"/>, <see cref="DataFormat.CreateBytesPlatformFormat"/>
/// or <see cref="DataFormat.CreateStringPlatformFormat"/>.
/// </remarks>
[SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Used to resolve typed overloads.")]
public sealed class DataFormat<T> : DataFormat
    where T : class
{
    internal DataFormat(DataFormatKind kind, string identifier)
        : base(kind, identifier)
    {
    }
}
